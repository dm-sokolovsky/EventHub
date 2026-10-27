using EventHub.Domain.Models;

namespace EventHab.Application.Repositories;

public interface IBookingRepository
{
    Task AddAsync(Booking data, CancellationToken ct = default);
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetPendingIds(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}