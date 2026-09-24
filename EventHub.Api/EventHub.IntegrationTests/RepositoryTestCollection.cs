namespace EventHub.IntegrationTests;

[CollectionDefinition("RepositoryCollection")]
public class RepositoryTestCollection : ICollectionFixture<PostgresFixture>;