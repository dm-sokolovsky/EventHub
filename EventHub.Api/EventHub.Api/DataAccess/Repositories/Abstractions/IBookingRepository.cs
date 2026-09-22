using EventHub.Api.Models;

namespace EventHub.Api.DataAccess.Repositories.Abstractions;

public interface IBookingRepository
{
    Task AddAsync(Booking data, CancellationToken ct = default);
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default);
}