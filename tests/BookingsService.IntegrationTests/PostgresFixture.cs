using Testcontainers.PostgreSql;

namespace BookingsService.IntegrationTests;

public class PostgresFixture : IAsyncLifetime
{
    public PostgreSqlContainer Postgres { get; } = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("bookingsservice_tests")
        .Build();

    public async Task InitializeAsync() => await Postgres.StartAsync();

    public async Task DisposeAsync() => await Postgres.DisposeAsync();
}
