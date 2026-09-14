using EventHub.Api.Contracts;
using EventHub.Api.Models;

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
    /// <exception cref="EventHub.Api.Common.Exceptions.NotFoundException">Событие не найдено или было удалено</exception>
    Task<BookingInfo> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Получение брони по идентификатору
    /// </summary>
    /// <param name="bookingId">Id брони</param>
    /// <returns></returns>
    Task<BookingInfo?> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);
    
}