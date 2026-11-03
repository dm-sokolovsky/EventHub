using EventHub.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Infrastructure.Seed;

public static class AdminSeeder
{
    public static async Task SeedAsync(
        IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var configuration = scope.ServiceProvider
            .GetRequiredService<IConfiguration>();

        var login = configuration["AdminSeed:Login"];
        var password = configuration["AdminSeed:Password"];

        if (string.IsNullOrWhiteSpace(login) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "AdminSeed configuration is missing.");
        }

        login = login.Trim().ToLowerInvariant();

        var existingUser = await context.Users
            .AnyAsync(u => u.Login == login);

        if (existingUser)
            return;


        var hasher = new IdentityPasswordHasher();
        var hashedPassword = hasher.Hash(password);
        
        var admin = User.Create(login, hashedPassword, Role.Admin);

        context.Users.Add(admin);

        await context.SaveChangesAsync();
    }
}