using EventHub.Api.Common.Exceptions;
using EventHub.Api.Contracts;
using EventHub.Api.DataAccess;
using EventHub.Api.DataAccess.Repositories;
using EventHub.Api.DataAccess.Repositories.Abstractions;
using EventHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Api.Services;

public sealed class EventService(IEventRepository eventRepository) : IEventService
{
    
    private readonly IEventRepository _eventRepository = eventRepository;
    
    public async Task<EventInfo> CreateEventAsync(CreateEvent request, CancellationToken cancellationToken = default)
    {
        var @event = Event.Create(request.Title, request.StartAt, request.EndAt, request.TotalSeats, request.Description);
        
        await _eventRepository.AddAsync(@event, cancellationToken);
        
        return ToInfo(@event);
    }
    
    public async Task<EventInfo> GetEventByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var @event = await _eventRepository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Event not found");

        return ToInfo(@event);
    }
    
    public async Task<PaginatedResult<EventInfo>> GetAllEventsAsync(
        EventFilter eventFilter, 
        int page = 1,
        int pageSize = 10, 
        CancellationToken cancellationToken = default)
    {
        var (query, totalCount) = await _eventRepository.GetAllEventsAsync(eventFilter, cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

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
        var @event = await _eventRepository.GetByIdAsync(id, cancellationToken)
                     ?? throw new NotFoundException("Event not found");

        @event.Update(request.Title, request.StartAt, request.EndAt, request.Description, request.TotalSeats);
        
        await _eventRepository.SaveChangesAsync(cancellationToken);
        
        return ToInfo(@event);
    }

    public async Task<bool> DeleteEventAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _eventRepository.DeleteByIdAsync(id, cancellationToken);;
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