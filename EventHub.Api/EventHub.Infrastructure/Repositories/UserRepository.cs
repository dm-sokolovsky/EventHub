using EventHab.Application.Repositories;
using EventHub.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Repositories;

public sealed class UserRepository(AppDbContext appDbContext) : IUserRepository
{
    private readonly AppDbContext _appDbContext = appDbContext;
    
    public async Task<bool> ExistsByLoginAsync(string login, CancellationToken ct) 
        => await appDbContext.Users.AnyAsync(u => u.Login == login, ct);

    public async Task<User?> GetByLoginAsync(string login, CancellationToken ct) 
        => await appDbContext.Users.SingleOrDefaultAsync(u => u.Login == login, ct);

    public async Task AddAsync(User user, CancellationToken ct) 
        => await appDbContext.Users.AddAsync(user, ct);

    public async Task SaveChangesAsync(CancellationToken ct) 
        =>  await appDbContext.SaveChangesAsync(ct);
}