using EventHub.Api.Contracts;
using EventHub.Api.Models;

namespace EventHub.Api.Services;

public interface IEventService
{
    Task<PaginatedResult<EventInfo>> GetAllEventsAsync(EventFilter eventFilter, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<EventInfo> GetEventByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EventInfo> CreateEventAsync(CreateEvent newEvent, CancellationToken cancellationToken = default);
    Task<EventInfo> UpdateEventAsync(Guid id, EventUpsert updatedEvent, CancellationToken cancellationToken = default);
    Task<bool> DeleteEventAsync(Guid id,  CancellationToken cancellationToken = default);
}