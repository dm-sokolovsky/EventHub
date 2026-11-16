using EventsService.Application.Contracts;
using EventsService.Application.Repositories;
using EventsService.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace EventsService.Infrastructure.Repositories;

public class EventRepository(AppDbContext appDbContext) : IEventRepository
{
    public async Task AddAsync(Event data, CancellationToken ct = default)
    {
        await appDbContext.Events.AddAsync(data, ct);
        await appDbContext.SaveChangesAsync(ct);
    }

    public async Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await appDbContext.Events.FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task<(IReadOnlyList<Event>, int totalCount)> GetAllEventsAsync(EventFilter filter, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        var query = appDbContext.Events.AsQueryable();

        if (filter.From.HasValue)
            query = query.Where(e => e.StartAt >= filter.From.Value);

        if (filter.To.HasValue)
            query = query.Where(e => e.StartAt <= filter.To.Value);

        if (!string.IsNullOrWhiteSpace(filter.Title))
            query = query.Where(e => e.Title.ToLower().Contains(filter.Title.ToLower()));

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<bool> DeleteByIdAsync(Guid id, CancellationToken ct = default)
    {
        var eventToDelete = await appDbContext.Events.FirstOrDefaultAsync(e => e.Id == id, ct);

        if (eventToDelete is null)
            return false;

        appDbContext.Events.Remove(eventToDelete);

        await appDbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await appDbContext.SaveChangesAsync(ct);
    }
}
