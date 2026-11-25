using EventHub.Contracts;
using EventsService.Application.Contracts;
using EventsService.Application.Repositories;
using EventsService.Application.Services.Abstractions;
using EventsService.Domain.Exceptions;
using EventsService.Domain.Models;

namespace EventsService.Application.Services;

public sealed class EventService(
    IEventRepository eventRepository,
    IEventPublisher eventPublisher,
    ICacheService cacheService
        ) : IEventService
{
    public async Task<EventInfo> CreateEventAsync(CreateEvent request, CancellationToken cancellationToken = default)
    {
        var @event = Event.Create(request.Title, request.StartAt, request.EndAt, request.TotalSeats, request.Description);

        await eventRepository.AddAsync(@event, cancellationToken);

        await eventPublisher.PublishAsync(ToChanged(@event, EventChangeType.Created), cancellationToken);

        return ToInfo(@event);
    }

    public async Task<EventInfo> GetEventByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {

        var cacheKey = CacheKeys.Event(id);
        
        var cached = await cacheService.GetAsync<EventInfo>(cacheKey);
        
        if (cached is not null)
            return cached;
        
        var @event = await eventRepository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Event not found");
        
        var info = ToInfo(@event);

        await cacheService.SetAsync(cacheKey, info, TimeSpan.FromMinutes(5));

        return info;
    }

    public async Task<PaginatedResult<EventInfo>> GetAllEventsAsync(
        EventFilter eventFilter,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await eventRepository.GetAllEventsAsync(eventFilter, page, pageSize, cancellationToken);

        return new PaginatedResult<EventInfo>
        {
            Items = items.Select(ToInfo).ToArray(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<EventInfo> UpdateEventAsync(Guid id, EventUpsert request, CancellationToken cancellationToken = default)
    {
        var @event = await eventRepository.GetByIdAsync(id, cancellationToken)
                     ?? throw new NotFoundException("Event not found");

        @event.Update(request.Title, request.StartAt, request.EndAt, request.Description, request.TotalSeats);

        await eventRepository.SaveChangesAsync(cancellationToken);

        await cacheService.RemoveAsync(CacheKeys.Event(id));
        await cacheService.RemoveAsync(CacheKeys.Top10);

        await eventPublisher.PublishAsync(ToChanged(@event, EventChangeType.Updated), cancellationToken);

        return ToInfo(@event);
    }

    public async Task<bool> DeleteEventAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var @event = await eventRepository.GetByIdAsync(id, cancellationToken);
        if (@event is null)
            return false;

        var deleted = await eventRepository.DeleteByIdAsync(id, cancellationToken);

        if (deleted)
        {
            await cacheService.RemoveAsync(CacheKeys.Event(id));
            await cacheService.RemoveAsync(CacheKeys.Top10);
            await eventPublisher.PublishAsync(ToChanged(@event, EventChangeType.Deleted), cancellationToken);
        }

        return deleted;
    }

    public async Task<List<EventInfo>?> GetTopEventsCachedAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.Top10;

        var cached = await cacheService.GetAsync<List<EventInfo>>(cacheKey);

        if (cached is not null)
            return cached;

        var top = (await eventRepository.GetTopEventsAsync(10, cancellationToken))
            .Select(ToInfo)
            .ToList();

        await cacheService.SetAsync(cacheKey, top, TimeSpan.FromMinutes(5));

        return top;
    }

    private static EventChanged ToChanged(Event @event, EventChangeType changeType) => new(
        @event.Id,
        @event.Title,
        @event.StartAt,
        @event.EndAt,
        @event.TotalSeats,
        @event.AvailableSeats,
        changeType);

    private static EventInfo ToInfo(Event @event) => new()
    {
        Id = @event.Id,
        Title = @event.Title,
        StartAt = @event.StartAt,
        EndAt = @event.EndAt,
        TotalSeats = @event.TotalSeats,
        AvailableSeats = @event.AvailableSeats,
        Description = @event.Description
    };
}
