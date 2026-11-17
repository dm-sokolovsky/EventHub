using Testcontainers.PostgreSql;

namespace EventsService.IntegrationTests;

public class PostgresFixture : IAsyncLifetime
{
    public PostgreSqlContainer Postgres { get; } = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("eventsservice_tests")
        .Build();

    public async Task InitializeAsync() => await Postgres.StartAsync();

    public async Task DisposeAsync() => await Postgres.DisposeAsync();
}
