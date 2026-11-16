using BookingsService.Application.Contracts;

namespace BookingsService.Application.Services.Abstractions;

/// <summary>
/// Интерфейс сервиса бронирования
/// </summary>
public interface IBookingService
{
    /// <summary>
    /// Создание брони для указанного события
    /// </summary>
    /// <param name="eventId">Id события</param>
    /// <param name="userId">Id пользователя</param>
    Task<BookingInfo> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получение брони по идентификатору
    /// </summary>
    Task<BookingInfo> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Отмена брони по идентификатору
    /// </summary>
    Task<BookingInfo> CancelledBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);
}
