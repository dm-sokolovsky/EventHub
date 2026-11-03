using EventHab.Application.Repositories;
using EventHub.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Repositories;

public class BookingRepository(AppDbContext appDbContext) : IBookingRepository
{
    private readonly AppDbContext _appDbContext = appDbContext;
    
    public async Task AddAsync(Booking data, CancellationToken ct = default)
    {
        await _appDbContext.Bookings.AddAsync(data, ct);
        await _appDbContext.SaveChangesAsync(ct);
    }

    public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _appDbContext.Bookings.FirstOrDefaultAsync(b => b.Id == id, ct);
    }

    public async Task<IReadOnlyList<Guid>> GetPendingIds(CancellationToken ct = default)
    {
        return await _appDbContext.Bookings
            .Where(b => b.Status == BookingStatus.Pending)
            .Select(b => b.Id)
            .ToListAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _appDbContext.SaveChangesAsync(ct);
    }
    
    
    public async Task<int> CountActiveBookingsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _appDbContext.Set<Booking>()
            .CountAsync(
                b => b.UserId == userId &&
                     (b.Status == BookingStatus.Pending ||
                      b.Status == BookingStatus.Confirmed),
                cancellationToken);
    }
}