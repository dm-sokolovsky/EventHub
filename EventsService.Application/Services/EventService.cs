using EventsService.Application.Contracts;
using EventsService.Application.Repositories;
using EventsService.Application.Services.Abstractions;
using EventsService.Domain.Exceptions;
using EventsService.Domain.Models;

namespace EventsService.Application.Services;

public sealed class EventService(IEventRepository eventRepository) : IEventService
{
    public async Task<EventInfo> CreateEventAsync(CreateEvent request, CancellationToken cancellationToken = default)
    {
        var @event = Event.Create(request.Title, request.StartAt, request.EndAt, request.TotalSeats, request.Description);

        await eventRepository.AddAsync(@event, cancellationToken);

        return ToInfo(@event);
    }

    public async Task<EventInfo> GetEventByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var @event = await eventRepository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Event not found");

        return ToInfo(@event);
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

        return ToInfo(@event);
    }

    public async Task<bool> DeleteEventAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await eventRepository.DeleteByIdAsync(id, cancellationToken);
    }

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
