using UsersService.Domain.Models;
using UsersService.Infrastructure;
using UsersService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace UsersService.IntegrationTests;

[Collection("RepositoryCollection")]
public class UserRepositoryTests(PostgresFixture fixture)
{
    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.Postgres.GetConnectionString())
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private async Task ResetDatabaseAsync()
    {
        NpgsqlConnection.ClearAllPools();
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    [Fact]
    public async Task AddAsync_SavesUserToDatabase()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var user = User.Create("testuser", "passwordHash");

        // Act
        var repository = new UserRepository(context);
        await repository.AddAsync(user, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        // Assert
        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        Assert.NotNull(saved);
        Assert.Equal("testuser", saved.Login);
        Assert.Equal(Role.User, saved.Role);
    }

    [Fact]
    public async Task GetByLoginAsync_ReturnsCorrectUser()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var user = User.Create("login-check", "passwordHash");
        await context.AddAsync(user);
        await context.SaveChangesAsync();

        // Act
        var repository = new UserRepository(CreateContext());
        var result = await repository.GetByLoginAsync("login-check", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
    }

    [Fact]
    public async Task ExistsByLoginAsync_ReturnsTrueForExistingLogin()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var user = User.Create("exists-check", "passwordHash");
        await context.AddAsync(user);
        await context.SaveChangesAsync();

        // Act
        var repository = new UserRepository(CreateContext());
        var exists = await repository.ExistsByLoginAsync("exists-check", CancellationToken.None);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsByLoginAsync_ReturnsFalseForUnknownLogin()
    {
        await ResetDatabaseAsync();

        // Arrange
        var repository = new UserRepository(CreateContext());

        // Act
        var exists = await repository.ExistsByLoginAsync("no-such-user", CancellationToken.None);

        // Assert
        Assert.False(exists);
    }
}
