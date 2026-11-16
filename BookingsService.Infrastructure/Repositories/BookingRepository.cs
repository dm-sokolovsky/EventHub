using BookingsService.Application.Repositories;
using BookingsService.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BookingsService.Infrastructure.Repositories;

public class BookingRepository(AppDbContext appDbContext) : IBookingRepository
{
    public async Task AddAsync(Booking data, CancellationToken ct = default)
    {
        await appDbContext.Bookings.AddAsync(data, ct);
        await appDbContext.SaveChangesAsync(ct);
    }

    public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await appDbContext.Bookings.FirstOrDefaultAsync(b => b.Id == id, ct);
    }

    public async Task<IReadOnlyList<Guid>> GetPendingIds(CancellationToken ct = default)
    {
        return await appDbContext.Bookings
            .Where(b => b.Status == BookingStatus.Pending)
            .Select(b => b.Id)
            .ToListAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await appDbContext.SaveChangesAsync(ct);
    }

    public async Task<int> CountActiveBookingsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await appDbContext.Bookings
            .CountAsync(
                b => b.UserId == userId &&
                     (b.Status == BookingStatus.Pending ||
                      b.Status == BookingStatus.Confirmed),
                cancellationToken);
    }
}
