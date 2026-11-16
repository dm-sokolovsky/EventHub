namespace EventHub.Contracts;

/// <summary>
/// Имена топиков межсервисного обмена
/// </summary>
public static class Topics
{
    /// <summary>
    /// Бронь подтверждена — публикует BookingsService, подписывается EventsService
    /// </summary>
    public const string BookingConfirmed = "booking-confirmed";
}
