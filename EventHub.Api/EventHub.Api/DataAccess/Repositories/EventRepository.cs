using EventHub.Api.Common.Exceptions;
using EventHub.Api.Contracts;
using EventHub.Api.DataAccess.Repositories.Abstractions;
using EventHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Api.DataAccess.Repositories;

public class EventRepository(AppDbContext appDbContext) : IEventRepository
{
    private readonly AppDbContext _appDbContext = appDbContext;
    
    public async Task AddAsync(Event data, CancellationToken ct = default)
    {
        await _appDbContext.Events.AddAsync(data, ct);
        await _appDbContext.SaveChangesAsync(ct);
    }

    public async Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _appDbContext.Events.FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task<(IQueryable<Event>, int totalCount)> GetAllEventsAsync(EventFilter filter, CancellationToken ct = default)
    {
        var query = _appDbContext.Events.AsQueryable();
        
        if (filter.From.HasValue)
            query = query.Where(e => e.StartAt >= filter.From.Value);

        if (filter.To.HasValue)
            query = query.Where(e => e.StartAt <= filter.To.Value);

        if (!string.IsNullOrWhiteSpace(filter.Title))
            query = query.Where(e => e.Title.ToLower().Contains(filter.Title.ToLower()));
        
        var  totalCount = await query.CountAsync(ct);
        
        return (query, totalCount);
    }

    public async Task<Event?> UpdateByIdAsync(Guid id, EventUpsert data, CancellationToken ct = default)
    {
        var eventToUpdate = await _appDbContext.Events.FirstOrDefaultAsync(e => e.Id == id, ct);
        
        if (eventToUpdate is null)
            return null;
        
        eventToUpdate.Update(data.Title, data.StartAt, data.EndAt, data.Description, data.TotalSeats);
        
        await _appDbContext.SaveChangesAsync(ct);
        
        return eventToUpdate;
    }

    public async Task<bool> DeleteByIdAsync(Guid id, CancellationToken ct = default)
    {
        var eventToDelete = await _appDbContext.Events.FirstOrDefaultAsync(e => e.Id == id, ct);
        
        if (eventToDelete is null)
            return false;
        
        _appDbContext.Events.Remove(eventToDelete);
        
        await  _appDbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _appDbContext.SaveChangesAsync(ct);
    }
}