using EventHub.Domain.Models;

namespace EventHab.Application.Repositories;

public interface IUserRepository
{
    Task<bool> ExistsByLoginAsync(string login, CancellationToken ct);
    Task<User?> GetByLoginAsync(string login, CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}