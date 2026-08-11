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
    Task CreateBookingAsync(Guid eventId);
    
    /// <summary>
    /// Получение брони по идентификатору 
    /// </summary>
    /// <param name="bookingId">Id брони</param>
    /// <returns></returns>
    Task<Booking?> GetBookingByIdAsync(Guid bookingId);
}