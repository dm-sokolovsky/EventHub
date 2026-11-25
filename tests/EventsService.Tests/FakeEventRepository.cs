using EventsService.Application.Contracts;
using EventsService.Application.Repositories;
using EventsService.Domain.Models;

namespace EventsService.Tests;

public sealed class FakeEventRepository : IEventRepository
{
    private readonly Dictionary<Guid, Event> _events = [];

    public int GetByIdCallCount { get; private set; }
    public int GetTopCallCount { get; private set; }
    public int SaveChangesCallCount { get; private set; }

    public void Seed(Event @event) => _events[@event.Id] = @event;

    public Task AddAsync(Event data, CancellationToken ct = default)
    {
        _events[data.Id] = data;
        return Task.CompletedTask;
    }

    public Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        GetByIdCallCount++;
        return Task.FromResult(_events.GetValueOrDefault(id));
    }

    public Task<(IReadOnlyList<Event>, int totalCount)> GetAllEventsAsync(
        EventFilter filter, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        IReadOnlyList<Event> items = _events.Values
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        return Task.FromResult((items, _events.Count));
    }

    public Task<bool> DeleteByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_events.Remove(id));

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        SaveChangesCallCount++;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Event>> GetTopEventsAsync(int count, CancellationToken ct = default)
    {
        GetTopCallCount++;
        IReadOnlyList<Event> top = _events.Values
            .OrderByDescending(e => (double)(e.TotalSeats - e.AvailableSeats) / e.TotalSeats)
            .Take(count)
            .ToList();
        return Task.FromResult(top);
    }
}
