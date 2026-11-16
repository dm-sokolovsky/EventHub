namespace UsersService.IntegrationTests;

[CollectionDefinition("RepositoryCollection")]
public class RepositoryTestCollection : ICollectionFixture<PostgresFixture>;
