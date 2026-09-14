using EventHub.Api.Common.Exceptions;
using EventHub.Api.Contracts;
using EventHub.Api.DataAccess;
using EventHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Api.Services;

public sealed class EventService : IEventService
{

    private readonly AppDbContext _context;

    public EventService(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task<EventInfo> CreateEventAsync(CreateEvent request, CancellationToken cancellationToken = default)
    {
        var @event = Event.Create(request.Title, request.StartAt, request.EndAt, request.TotalSeats, request.Description);
        await _context.Events.AddAsync(@event, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return ToInfo(@event);
    }
    
    public async Task<EventInfo> GetEventByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var @event = await _context.Events.FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
                     ?? throw new NotFoundException("Event not found");

        return ToInfo(@event);
    }
    
    public async Task<PaginatedResult<EventInfo>> GetAllEventsAsync(
        EventFilter eventFilter, 
        int page = 1,
        int pageSize = 10, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.Events.AsQueryable();

        if (eventFilter.From.HasValue)
            query = query.Where(e => e.StartAt >= eventFilter.From.Value);

        if (eventFilter.To.HasValue)
            query = query.Where(e => e.StartAt <= eventFilter.To.Value);

        if (!string.IsNullOrWhiteSpace(eventFilter.Title))
            query = query.Where(e => e.Title.ToLower().Contains(eventFilter.Title.ToLower()));

        var totalCount = await query.CountAsync(cancellationToken);

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
        var @event = await _context.Events.FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
                     ?? throw new NotFoundException("Event not found");

        @event.Update(request.Title, request.StartAt, request.EndAt, request.Description);
        await _context.SaveChangesAsync(cancellationToken);

        return ToInfo(@event);
    }

    public async Task<bool> DeleteEventAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var @event = await _context.Events.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (@event == null)
            return false;

        _context.Events.Remove(@event);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
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