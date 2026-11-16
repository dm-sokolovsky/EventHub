namespace EventsService.IntegrationTests;

[CollectionDefinition("RepositoryCollection")]
public class RepositoryTestCollection : ICollectionFixture<PostgresFixture>;
