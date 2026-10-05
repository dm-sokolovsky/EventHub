using EventHub.Api.Contracts;
using EventHub.Api.DataAccess;
using EventHub.Api.DataAccess.Repositories;
using EventHub.Api.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace EventHub.IntegrationTests;

[Collection("RepositoryCollection")]
public class EventRepositoryTests(PostgresFixture fixture) 
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
    public async Task CreateEvent_SaveEventToDatabase()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var now = DateTime.UtcNow.AddHours(1);
        var eventData = Event.Create("Event", now, now.AddHours(2), 10);

        // Act
        var repository = new EventRepository(context);
        await repository.AddAsync(eventData);
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Events.FirstOrDefaultAsync(b => b.Id == eventData.Id);
        Assert.NotNull(saved);
    } 
    
    [Fact]
    public async Task GetById_ReturnsCorrectEvent()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var now = DateTime.UtcNow.AddHours(1);
        var eventData = Event.Create("Event", now, now.AddHours(2), 10);
        await context.AddAsync(eventData);
        await context.SaveChangesAsync();
        
        // Act
        var repository = new EventRepository(CreateContext());
        var result = await repository.GetByIdAsync(eventData.Id);

        // Assert
        Assert.NotNull(result);
    }
    
    [Fact]
    public async Task DeleteEvent_RemovesFromDatabase()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var now = DateTime.UtcNow.AddHours(1);
        var eventData = Event.Create("Event", now, now.AddHours(2), 10, "тест");
        await context.AddAsync(eventData);
        await context.SaveChangesAsync();

        // Act
        var repository = new EventRepository(CreateContext());
        await repository.DeleteByIdAsync(eventData.Id);

        // Assert
        await using var verifyContext = CreateContext();
        var deleted = await verifyContext.Events.FirstOrDefaultAsync(b => b.Id == eventData.Id);
        Assert.Null(deleted);
    }
    
    
    [Fact]
    public async Task GetAllEvents_ReturnsCollectionOfEvents()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var now = DateTime.UtcNow.AddHours(1);
        var eventData1 = Event.Create("Event1", now, now.AddHours(2), 10, "тест");
        var eventData2 = Event.Create("Event2", now, now.AddHours(2), 10, "тест");
        var eventData3 = Event.Create("Event3", now.AddMinutes(5), now.AddHours(3), 10, "тест");
        await context.AddAsync(eventData1);
        await context.AddAsync(eventData2);
        await context.AddAsync(eventData3);
        await context.SaveChangesAsync();

        // Act
        var repository = new EventRepository(CreateContext());
        var filterToTitle = new EventFilter("Event", null, null);
        var (resultToTitle, countResultToTitle) = await repository.GetAllEventsAsync(filterToTitle);
        
        var filterToDate = new EventFilter(null, now.AddDays(-1), now.AddDays(-1));
        var (resultToDate, countResultToDate) = await repository.GetAllEventsAsync(filterToDate);

        // Assert
        Assert.Equal(3, countResultToTitle);
        Assert.Equal(0, countResultToDate);
    }
}