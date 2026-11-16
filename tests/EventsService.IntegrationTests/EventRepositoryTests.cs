using EventsService.Application.Contracts;
using EventsService.Domain.Models;
using EventsService.Infrastructure;
using EventsService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EventsService.IntegrationTests;

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
    public async Task AddAsync_SavesEventToDatabase()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var now = DateTime.UtcNow.AddHours(1);
        var eventData = Event.Create("Event", now, now.AddHours(2), 10);

        // Act
        var repository = new EventRepository(context);
        await repository.AddAsync(eventData);

        // Assert
        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Events.FirstOrDefaultAsync(e => e.Id == eventData.Id);
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsCorrectEvent()
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
        Assert.Equal(eventData.Id, result.Id);
    }

    [Fact]
    public async Task DeleteByIdAsync_RemovesEventFromDatabase()
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
        var deleted = await repository.DeleteByIdAsync(eventData.Id);

        // Assert
        Assert.True(deleted);
        await using var verifyContext = CreateContext();
        var result = await verifyContext.Events.FirstOrDefaultAsync(e => e.Id == eventData.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllEventsAsync_FiltersByTitleAndDate()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var now = DateTime.UtcNow.AddHours(1);
        var eventData1 = Event.Create("Event1", now, now.AddHours(2), 10, "тест");
        var eventData2 = Event.Create("Event2", now, now.AddHours(2), 10, "тест");
        var eventData3 = Event.Create("Concert", now.AddMinutes(5), now.AddHours(3), 10, "тест");
        await context.AddAsync(eventData1);
        await context.AddAsync(eventData2);
        await context.AddAsync(eventData3);
        await context.SaveChangesAsync();

        // Act
        var repository = new EventRepository(CreateContext());
        var filterToTitle = new EventFilter("event", null, null);
        var (resultToTitle, countResultToTitle) = await repository.GetAllEventsAsync(filterToTitle);

        var filterToDate = new EventFilter(null, now.AddDays(-1), now.AddDays(-1));
        var (_, countResultToDate) = await repository.GetAllEventsAsync(filterToDate);

        // Assert
        Assert.Equal(2, countResultToTitle);
        Assert.Equal(2, resultToTitle.Count);
        Assert.Equal(0, countResultToDate);
    }
}
