using EventHub.Api.Models.Booking;

namespace EventHub.Api.Services;

/// <summary>
/// Интерфейс сервиса бронирования 
/// </summary>
public interface IBookingService
{
    /// <summary>
    /// Создание брони для указанного события 
    /// </summary>
    /// <param name="eventId">Id события</param>
    /// <returns></returns>
    Task<Booking> CreateBookingAsync(Guid eventId);
    
    /// <summary>
    /// Получение брони по идентификатору
    /// </summary>
    /// <param name="bookingId">Id брони</param>
    /// <returns></returns>
    Task<Booking?> GetBookingByIdAsync(Guid bookingId);

    /// <summary>
    /// Получение всех броней в статусе Pending
    /// </summary>
    Task<IReadOnlyList<Booking>> GetPendingBookingsAsync();

    /// <summary>
    /// Сохранение обновлённой брони в хранилище
    /// </summary>
    /// <param name="booking">Обновлённая бронь</param>
    Task UpdateBookingAsync(Booking booking);
}