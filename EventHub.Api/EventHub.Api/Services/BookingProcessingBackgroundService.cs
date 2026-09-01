using EventHub.Api.Models.Booking;

namespace EventHub.Api.Services;

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
    
    private readonly SemaphoreSlim _processingSemaphore = new(1, 1); 

    private readonly IBookingService _bookingService;
    private readonly IEventService _eventService;
    private readonly ILogger<BookingProcessingBackgroundService> _logger;

    public BookingProcessingBackgroundService(
        IBookingService bookingService,
        IEventService eventService,
        ILogger<BookingProcessingBackgroundService> logger)
    {
        _bookingService = bookingService;
        _eventService = eventService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingBookingsAsync(stoppingToken);
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

    private async Task ProcessPendingBookingsAsync(CancellationToken stoppingToken)
    {
        var pendingBookings = await _bookingService.GetPendingBookingsAsync();

        var tasks = pendingBookings.Select(booking => ProcessBookingAsync(booking, stoppingToken));
        await Task.WhenAll(tasks); 
    }

    private async Task ProcessBookingAsync(Booking booking, CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        // Имитация обращения к внешней системе
        await Task.Delay(ProcessingDelay, stoppingToken);
        
        await _processingSemaphore.WaitAsync(stoppingToken);

        try
        {

            if (_eventService.GetEventById(booking.EventId) is null)
            {
                booking.Reject();
                _logger.LogWarning($"Не удалось найти событие с id = {booking.EventId}");
            }
            else
            {
                booking.Confirm();
            }

            await _bookingService.UpdateBookingAsync(booking);
        }
        catch (Exception ex)
        {
            if (ex is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
                _logger.LogError(ex, "Непредвиденная ошибка при обработке брони {BookingId}", booking.Id);

            booking.Reject();
            await _bookingService.UpdateBookingAsync(booking);

            var @event = _eventService.GetEventById(booking.EventId);

            @event?.ReleaseSeat();
        }
        finally
        {
            _processingSemaphore.Release();
        }

        _logger.LogInformation("Бронь {BookingId} переведена в статус {Status}", booking.Id, booking.Status);
    }
}
