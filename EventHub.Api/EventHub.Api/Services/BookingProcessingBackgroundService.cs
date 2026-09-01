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

    private readonly IBookingService _bookingService;
    private readonly ILogger<BookingProcessingBackgroundService> _logger;

    public BookingProcessingBackgroundService(
        IBookingService bookingService,
        ILogger<BookingProcessingBackgroundService> logger)
    {
        _bookingService = bookingService;
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

        foreach (var booking in pendingBookings)
        {
            stoppingToken.ThrowIfCancellationRequested();

            // Имитация обращения к внешней системе
            await Task.Delay(ProcessingDelay, stoppingToken);
            
            booking.Confirm();

            await _bookingService.UpdateBookingAsync(booking);

            _logger.LogInformation("Бронь {BookingId} переведена в статус {Status}", booking.Id, booking.Status);
        }
    }
}
