using System.Collections.Concurrent;
using EventHub.Contracts;
using BookingsService.Application.Services.Abstractions;

namespace BookingsService.Infrastructure.Gateways;

/// <summary>
/// Временная in-memory заглушка шлюза к EventsService.
/// Хранит события и места в памяти процесса; данные теряются при перезапуске.
/// Заменяется на интеграцию с EventsService (Kafka), когда та появится.
/// </summary>
public sealed class InMemoryEventsGateway : IEventsGateway
{
    private sealed class EventState
    {
        public required EventSnapshot Snapshot { get; init; }
        public int ReservedSeats { get; set; }
        public int TotalSeats { get; set; }
    }

    private readonly ConcurrentDictionary<Guid, EventState> _events = new();

    /// <summary>
    /// Добавить событие в заглушку (используется при старте и в тестах)
    /// </summary>
    public void AddEvent(Guid eventId, DateTime startAt, int totalSeats)
    {
        _events[eventId] = new EventState
        {
            Snapshot = new EventSnapshot(eventId, startAt),
            TotalSeats = totalSeats
        };
    }

    public Task<EventSnapshot?> GetAsync(Guid eventId, CancellationToken ct = default)
    {
        _events.TryGetValue(eventId, out var state);
        return Task.FromResult(state?.Snapshot);
    }

    public Task<bool> TryReserveSeatsAsync(Guid eventId, CancellationToken ct = default)
    {
        if (!_events.TryGetValue(eventId, out var state))
            return Task.FromResult(false);

        lock (state)
        {
            if (state.ReservedSeats >= state.TotalSeats)
                return Task.FromResult(false);

            state.ReservedSeats++;
            return Task.FromResult(true);
        }
    }

    public Task ReleaseSeatsAsync(Guid eventId, CancellationToken ct = default)
    {
        if (_events.TryGetValue(eventId, out var state))
        {
            lock (state)
            {
                if (state.ReservedSeats > 0)
                    state.ReservedSeats--;
            }
        }

        return Task.CompletedTask;
    }
}
