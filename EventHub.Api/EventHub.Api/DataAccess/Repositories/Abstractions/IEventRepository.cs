using EventHub.Api.Contracts;
using EventHub.Api.Models;

namespace EventHub.Api.DataAccess.Repositories.Abstractions;

public interface IEventRepository
{
    Task AddAsync(Event data, CancellationToken ct = default);
    Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(IQueryable<Event>, int totalCount)> GetAllEventsAsync(EventFilter filter,CancellationToken ct = default);
    Task<Event?> UpdateByIdAsync(Guid id, EventUpsert data, CancellationToken ct = default);
    Task<bool> DeleteByIdAsync(Guid id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
