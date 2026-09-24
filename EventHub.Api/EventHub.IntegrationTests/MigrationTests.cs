using EventHub.Api.DataAccess;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace EventHub.IntegrationTests;

public class MigrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("eventhub_migrations_test")
        .Build();
    
    public async Task InitializeAsync() => await _postgres.StartAsync();

    public async Task DisposeAsync() => await _postgres.DisposeAsync();
    
    [Fact]
    public async Task Migrate_AppliesInitialCreateMigration()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        await using var context = new AppDbContext(options);

        // Act
        await context.Database.MigrateAsync();
        var applied = await context.Database.GetAppliedMigrationsAsync();

        // Assert
        Assert.Contains("20260923101253_InitialCreate", applied);
    }
}