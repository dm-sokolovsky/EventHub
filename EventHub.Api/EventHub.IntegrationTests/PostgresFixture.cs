using Testcontainers.PostgreSql;

namespace EventHub.IntegrationTests;

public class PostgresFixture : IAsyncLifetime
{
    public PostgreSqlContainer Postgres { get; } = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("eventhub_tests")
        .Build();

    public async Task InitializeAsync() => await Postgres.StartAsync();

    public async Task DisposeAsync() => await Postgres.DisposeAsync();
}