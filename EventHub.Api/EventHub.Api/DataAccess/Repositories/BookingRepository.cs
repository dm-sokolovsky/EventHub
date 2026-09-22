using EventHub.Api.DataAccess.Repositories.Abstractions;
using EventHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Api.DataAccess.Repositories;

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
}