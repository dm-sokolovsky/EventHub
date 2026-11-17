namespace BookingsService.IntegrationTests;

[CollectionDefinition("RepositoryCollection")]
public class RepositoryTestCollection : ICollectionFixture<PostgresFixture>;
