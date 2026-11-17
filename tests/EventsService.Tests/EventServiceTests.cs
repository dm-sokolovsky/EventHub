using EventsService.Application;
using EventsService.Application.Contracts;
using EventsService.Application.Repositories;
using EventsService.Application.Services;
using EventsService.Application.Services.Abstractions;
using EventsService.Domain.Exceptions;
using EventsService.Infrastructure;
using EventsService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventsService.Tests;

public sealed class EventServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;
    private readonly IEventService _eventService;
    private readonly FakeEventPublisher _eventPublisher;

    public EventServiceTests()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddSingleton<IEventPublisher, FakeEventPublisher>();
        services.AddScoped<IEventService, EventService>();

        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();
        _eventService = _scope.ServiceProvider.GetRequiredService<IEventService>();
        _eventPublisher = (FakeEventPublisher)_serviceProvider.GetRequiredService<IEventPublisher>();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _serviceProvider.Dispose();
    }

    private static CreateEvent ValidCreateRequest(string title = "Test Event") => new()
    {
        Title = title,
        StartAt = DateTime.UtcNow.AddDays(1),
        EndAt = DateTime.UtcNow.AddDays(1).AddHours(2),
        TotalSeats = 10,
        Description = "Description"
    };

    #region CreateEventAsync

    [Fact]
    public async Task CreateEventAsync_WithValidData_ReturnsEventInfoWithAllSeatsAvailable()
    {
        var result = await _eventService.CreateEventAsync(ValidCreateRequest());

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Test Event", result.Title);
        Assert.Equal(10, result.TotalSeats);
        Assert.Equal(10, result.AvailableSeats);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateEventAsync_WithEmptyTitle_ThrowsValidationException(string? title)
    {
        var request = ValidCreateRequest() with { Title = title };

        await Assert.ThrowsAsync<ValidationException>(() =>
            _eventService.CreateEventAsync(request));
    }

    [Fact]
    public async Task CreateEventAsync_WithNullStartAt_ThrowsValidationException()
    {
        var request = ValidCreateRequest() with { StartAt = null };

        await Assert.ThrowsAsync<ValidationException>(() =>
            _eventService.CreateEventAsync(request));
    }

    [Fact]
    public async Task CreateEventAsync_WithNullEndAt_ThrowsValidationException()
    {
        var request = ValidCreateRequest() with { EndAt = null };

        await Assert.ThrowsAsync<ValidationException>(() =>
            _eventService.CreateEventAsync(request));
    }

    [Fact]
    public async Task CreateEventAsync_WithPastStartAt_ThrowsValidationException()
    {
        var request = ValidCreateRequest() with { StartAt = DateTime.UtcNow.AddDays(-1) };

        await Assert.ThrowsAsync<ValidationException>(() =>
            _eventService.CreateEventAsync(request));
    }

    [Fact]
    public async Task CreateEventAsync_WithEndAtBeforeStartAt_ThrowsValidationException()
    {
        var request = ValidCreateRequest() with
        {
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddHours(1)
        };

        await Assert.ThrowsAsync<ValidationException>(() =>
            _eventService.CreateEventAsync(request));
    }

    [Fact]
    public async Task CreateEventAsync_WithZeroSeats_ThrowsValidationException()
    {
        var request = ValidCreateRequest() with { TotalSeats = 0 };

        await Assert.ThrowsAsync<ValidationException>(() =>
            _eventService.CreateEventAsync(request));
    }

    [Fact]
    public async Task CreateEventAsync_WithWhitespaceTitle_TrimsTitle()
    {
        var result = await _eventService.CreateEventAsync(ValidCreateRequest("  Trimmed  "));

        Assert.Equal("Trimmed", result.Title);
    }

    #endregion

    #region GetEventByIdAsync

    [Fact]
    public async Task GetEventByIdAsync_WithValidId_ReturnsEventInfo()
    {
        var created = await _eventService.CreateEventAsync(ValidCreateRequest());

        var result = await _eventService.GetEventByIdAsync(created.Id);

        Assert.Equal(created.Id, result.Id);
        Assert.Equal(created.Title, result.Title);
    }

    [Fact]
    public async Task GetEventByIdAsync_WithInvalidId_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _eventService.GetEventByIdAsync(Guid.NewGuid()));
    }

    #endregion

    #region GetAllEventsAsync

    [Fact]
    public async Task GetAllEventsAsync_WithNoEvents_ReturnsEmptyResult()
    {
        var result = await _eventService.GetAllEventsAsync(new EventFilter(null, null, null));

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetAllEventsAsync_WithTitleFilter_IsCaseInsensitive()
    {
        await _eventService.CreateEventAsync(ValidCreateRequest("Rock Festival"));
        await _eventService.CreateEventAsync(ValidCreateRequest("Jazz Evening"));

        var result = await _eventService.GetAllEventsAsync(new EventFilter("rock", null, null));

        Assert.Single(result.Items);
        Assert.Equal("Rock Festival", result.Items[0].Title);
    }

    [Fact]
    public async Task GetAllEventsAsync_WithFromFilter_ReturnsOnlyFutureEvents()
    {
        var soon = DateTime.UtcNow.AddDays(1);
        var later = DateTime.UtcNow.AddDays(10);

        await _eventService.CreateEventAsync(ValidCreateRequest("Soon") with { StartAt = soon, EndAt = soon.AddHours(2) });
        await _eventService.CreateEventAsync(ValidCreateRequest("Later") with { StartAt = later, EndAt = later.AddHours(2) });

        var result = await _eventService.GetAllEventsAsync(
            new EventFilter(null, DateTime.UtcNow.AddDays(5), null));

        Assert.Single(result.Items);
        Assert.Equal("Later", result.Items[0].Title);
    }

    [Fact]
    public async Task GetAllEventsAsync_WithPagination_ReturnsRequestedPage()
    {
        for (var i = 1; i <= 15; i++)
            await _eventService.CreateEventAsync(ValidCreateRequest($"Event {i}"));

        var page1 = await _eventService.GetAllEventsAsync(
            new EventFilter(null, null, null), page: 1, pageSize: 10);
        var page2 = await _eventService.GetAllEventsAsync(
            new EventFilter(null, null, null), page: 2, pageSize: 10);

        Assert.Equal(15, page1.TotalCount);
        Assert.Equal(10, page1.Items.Length);
        Assert.Equal(2, page1.TotalPages);
        Assert.Equal(5, page2.Items.Length);
    }

    #endregion

    #region UpdateEventAsync

    [Fact]
    public async Task UpdateEventAsync_WithValidData_UpdatesEvent()
    {
        var created = await _eventService.CreateEventAsync(ValidCreateRequest("Old title"));

        var update = new EventUpsert("New title", "New description",
            DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(2).AddHours(3), 20);

        var result = await _eventService.UpdateEventAsync(created.Id, update);

        Assert.Equal("New title", result.Title);
        Assert.Equal(20, result.TotalSeats);
        Assert.Equal(20, result.AvailableSeats);
    }

    [Fact]
    public async Task UpdateEventAsync_WithInvalidId_ThrowsNotFoundException()
    {
        var update = new EventUpsert("Title", null,
            DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(2).AddHours(3), 20);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _eventService.UpdateEventAsync(Guid.NewGuid(), update));
    }

    [Fact]
    public async Task UpdateEventAsync_ReducingSeatsBelowBooked_ThrowsValidationException()
    {
        var created = await _eventService.CreateEventAsync(ValidCreateRequest() with { TotalSeats = 10 });

        // Имитируем бронь: резервируем 8 мест через доменную модель
        var repository = _scope.ServiceProvider.GetRequiredService<IEventRepository>();
        var entity = await repository.GetByIdAsync(created.Id, CancellationToken.None);
        entity!.TryReserveSeats(8);
        await repository.SaveChangesAsync(CancellationToken.None);

        var update = new EventUpsert("Title", null,
            DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(2).AddHours(3), 5);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _eventService.UpdateEventAsync(created.Id, update));
    }

    #endregion

    #region DeleteEventAsync

    [Fact]
    public async Task DeleteEventAsync_WithValidId_ReturnsTrueAndDeletesEvent()
    {
        var created = await _eventService.CreateEventAsync(ValidCreateRequest());

        var deleted = await _eventService.DeleteEventAsync(created.Id);

        Assert.True(deleted);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _eventService.GetEventByIdAsync(created.Id));
    }

    [Fact]
    public async Task DeleteEventAsync_WithInvalidId_ReturnsFalse()
    {
        var deleted = await _eventService.DeleteEventAsync(Guid.NewGuid());

        Assert.False(deleted);
    }

    #endregion
}
