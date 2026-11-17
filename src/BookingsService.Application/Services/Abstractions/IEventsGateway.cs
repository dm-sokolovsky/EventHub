using EventHub.Contracts;

namespace BookingsService.Application.Services.Abstractions;

/// <summary>
/// Шлюз к данным событий (EventsService).
/// Пока реализован заглушкой в памяти; позже заменяется на интеграцию через Kafka.
/// </summary>
public interface IEventsGateway
{
    /// <summary>
    /// Получить срез события; null, если событие не найдено
    /// </summary>
    Task<EventSnapshot?> GetAsync(Guid eventId, CancellationToken ct = default);

    /// <summary>
    /// Зарезервировать одно место; false, если мест не осталось
    /// </summary>
    Task<bool> TryReserveSeatsAsync(Guid eventId, CancellationToken ct = default);

    /// <summary>
    /// Освободить одно место
    /// </summary>
    Task ReleaseSeatsAsync(Guid eventId, CancellationToken ct = default);
}
