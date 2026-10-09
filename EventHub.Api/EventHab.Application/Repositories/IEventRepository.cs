using EventHab.Application.Contracts;
using EventHub.Domain.Models;

namespace EventHab.Application.Repositories;

public interface IEventRepository
{
    Task AddAsync(Event data, CancellationToken ct = default);
    Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(IReadOnlyList<Event>, int totalCount)> GetAllEventsAsync(EventFilter filter, int page = 1, int pageSize = 10, CancellationToken ct = default);
    Task<bool> DeleteByIdAsync(Guid id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
