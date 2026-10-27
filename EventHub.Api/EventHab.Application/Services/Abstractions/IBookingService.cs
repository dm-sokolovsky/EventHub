using EventHab.Application.Contracts;
using EventHub.Domain.Exceptions;

namespace EventHab.Application.Services.Abstractions;

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
    /// <exception cref="NotFoundException">Событие не найдено или было удалено</exception>
    Task<BookingInfo> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Получение брони по идентификатору
    /// </summary>
    /// <param name="bookingId">Id брони</param>
    /// <returns></returns>
    Task<BookingInfo?> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);
    
}