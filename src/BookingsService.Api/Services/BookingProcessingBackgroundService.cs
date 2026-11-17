using BookingsService.Application;
using BookingsService.Application.Exceptions;
using BookingsService.Application.Repositories;
using BookingsService.Application.Services.Abstractions;
using BookingsService.Domain.Models;
using EventHub.Contracts;

namespace BookingsService.Api.Services;

/// <summary>
/// Фоновый сервис, который периодически опрашивает хранилище броней
/// и обрабатывает брони в статусе Pending
/// </summary>
public class BookingProcessingBackgroundService : BackgroundService
{
    // Интервал между опросами хранилища
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);

    // Имитация обращения к внешней системе при обработке одной брони
    private static readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingProcessingBackgroundService> _logger;
    private readonly IEventPublisher _eventPublisher;

    public BookingProcessingBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<BookingProcessingBackgroundService> logger,
        IEventPublisher eventPublisher
        )
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {

                IReadOnlyList<Guid> pendingBookingIds;

               await using (var scope = _scopeFactory.CreateAsyncScope())
                {
                    var repo = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
                    pendingBookingIds = await repo.GetPendingIds(stoppingToken);
                }

                var tasks = pendingBookingIds.Select(id =>
                    ProcessBookingAsync(id, stoppingToken));

                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке бронирований в фоновом сервисе");
            }

            try
            {
                await Task.Delay(PollingInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task ProcessBookingAsync(Guid bookingId, CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(ProcessingDelay, stoppingToken);

            using var scope = _scopeFactory.CreateScope();
            var repoBooking = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
            var eventsGateway = scope.ServiceProvider.GetRequiredService<IEventsGateway>();

            var booking = await repoBooking.GetByIdAsync(bookingId, stoppingToken);
            if (booking == null || booking.Status != BookingStatus.Pending)
                return;

            var eventSnapshot = await eventsGateway.GetAsync(booking.EventId, stoppingToken);
            if (eventSnapshot == null)
            {
                booking.Reject();
                await repoBooking.SaveChangesAsync(stoppingToken);

                _logger.LogWarning(
                    "Booking {BookingId} rejected: event {EventId} not found",
                    booking.Id, booking.EventId);

                return;
            }

            booking.Confirm();
            await repoBooking.SaveChangesAsync(stoppingToken);
            await _eventPublisher.PublishAsync(new BookingConfirmed(booking.Id, booking.EventId, booking.UserId,
                Seats: 1, DateTime.UtcNow), stoppingToken);

            _logger.LogInformation(
                "Booking {BookingId} for event {EventId} processed → {Status}",
                booking.Id, booking.EventId, booking.Status);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (EventPublishingException ex)
        {
            _logger.LogWarning(ex,                                                
                "Kafka unavailable, booking {BookingId} stays pending for retry", 
                bookingId);   
        }
        catch (Exception ex)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repoBooking = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
                var eventsGateway = scope.ServiceProvider.GetRequiredService<IEventsGateway>();

                var booking = await repoBooking.GetByIdAsync(bookingId, stoppingToken);
                if (booking != null)
                {
                    booking.Reject();
                    await repoBooking.SaveChangesAsync(stoppingToken);

                    await eventsGateway.ReleaseSeatsAsync(booking.EventId, stoppingToken);
                }

                _logger.LogError(ex,
                    "Booking {BookingId} rejected due to processing error",
                    bookingId);
            }
            catch (Exception releaseEx)
            {
                _logger.LogError(releaseEx,
                    "Failed to reject booking {BookingId} after error",
                    bookingId);
            }
        }
    }
}
