using EventsService.Application.Contracts;
using EventsService.Application.Services;
using EventsService.Application.Services.Abstractions;
using EventsService.Domain.Models;
using Microsoft.Extensions.Options;

namespace EventsService.Tests;

public sealed class EventServiceCacheTests
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    private static readonly CacheOptions TestCacheOptions = new()
    {
        EventTtl = TimeSpan.FromMinutes(30),
        TopEventsTtl = TimeSpan.FromMinutes(2)
    };

    private readonly FakeEventRepository _repository = new();
    private readonly FakeCacheService _cache = new();
    private readonly EventService _sut;

    public EventServiceCacheTests()
    {
        _sut = new EventService(_repository, new FakeEventPublisher(), _cache, Options.Create(TestCacheOptions));
    }

    #region GetEventByIdAsync

    [Fact]
    public async Task GetEventByIdAsync_CacheHit_DoesNotCallRepository()
    {
        var @event = CreateEvent();
        _repository.Seed(@event);
        await _cache.SetAsync(CacheKeys.Event(@event.Id), ToInfo(@event, title: "Из кэша"), Ttl);

        var result = await _sut.GetEventByIdAsync(@event.Id);

        Assert.Equal(0, _repository.GetByIdCallCount);
        Assert.Equal("Из кэша", result.Title);
    }

    [Fact]
    public async Task GetEventByIdAsync_CacheMiss_LoadsFromRepositoryAndStoresInCache()
    {
        var @event = CreateEvent();
        _repository.Seed(@event);

        var result = await _sut.GetEventByIdAsync(@event.Id);

        Assert.Equal(1, _repository.GetByIdCallCount);
        Assert.Equal(@event.Title, result.Title);

        var cached = await _cache.GetAsync<EventInfo>(CacheKeys.Event(@event.Id));
        Assert.NotNull(cached);
        Assert.Equal(@event.Title, cached.Title);

        // Повторное чтение обслуживается кэшем, репозиторий не дёргается.
        await _sut.GetEventByIdAsync(@event.Id);
        Assert.Equal(1, _repository.GetByIdCallCount);
    }

    #endregion

    #region GetTopEventsCachedAsync

    [Fact]
    public async Task GetTopEventsCachedAsync_CacheHit_DoesNotCallRepository()
    {
        await _cache.SetAsync(CacheKeys.Top10, new List<EventInfo> { ToInfo(CreateEvent()) }, Ttl);

        var result = await _sut.GetTopEventsCachedAsync();

        Assert.Equal(0, _repository.GetTopCallCount);
        Assert.Single(result!);
    }

    [Fact]
    public async Task GetTopEventsCachedAsync_CacheMiss_LoadsFromRepositoryAndStoresInCache()
    {
        _repository.Seed(CreateEvent());

        var result = await _sut.GetTopEventsCachedAsync();

        Assert.Equal(1, _repository.GetTopCallCount);
        Assert.Single(result!);

        var cached = await _cache.GetAsync<List<EventInfo>>(CacheKeys.Top10);
        Assert.NotNull(cached);

        // Повторное чтение обслуживается кэшем, репозиторий не дёргается.
        var second = await _sut.GetTopEventsCachedAsync();
        Assert.Equal(1, _repository.GetTopCallCount);
        Assert.Single(second!);
    }

    #endregion

    #region Мутирующие операции — стратегия инвалидации

    [Fact]
    public async Task UpdateEventAsync_CachedEvent_InvalidatesCache()
    {
        var @event = CreateEvent();
        _repository.Seed(@event);
        await _sut.GetEventByIdAsync(@event.Id); // прогрев кэша события
        await _sut.GetTopEventsCachedAsync(); // прогрев кэша топ-10
        Assert.NotNull(await _cache.GetAsync<EventInfo>(CacheKeys.Event(@event.Id)));
        Assert.NotNull(await _cache.GetAsync<List<EventInfo>>(CacheKeys.Top10));

        var request = new EventUpsert(
            "Новое название",
            null,
            DateTime.UtcNow.AddDays(2),
            DateTime.UtcNow.AddDays(2).AddHours(3),
            100);
        await _sut.UpdateEventAsync(@event.Id, request);

        Assert.Null(await _cache.GetAsync<EventInfo>(CacheKeys.Event(@event.Id)));
        Assert.Null(await _cache.GetAsync<List<EventInfo>>(CacheKeys.Top10));

        // Следующее чтение идёт в репозиторий и возвращает свежие данные, а не устаревшие из кэша.
        var fresh = await _sut.GetEventByIdAsync(@event.Id);
        Assert.Equal("Новое название", fresh.Title);
    }

    [Fact]
    public async Task DeleteEventAsync_CachedEvent_RemovesCacheEntry()
    {
        var @event = CreateEvent();
        _repository.Seed(@event);
        await _sut.GetEventByIdAsync(@event.Id); // прогрев кэша события
        await _sut.GetTopEventsCachedAsync(); // прогрев кэша топ-10

        var deleted = await _sut.DeleteEventAsync(@event.Id);

        Assert.True(deleted);
        Assert.Null(await _cache.GetAsync<EventInfo>(CacheKeys.Event(@event.Id)));
        Assert.Null(await _cache.GetAsync<List<EventInfo>>(CacheKeys.Top10));
    }

    #endregion

    #region TTL и создание события

    [Fact]
    public async Task GetEventByIdAsync_CacheMiss_CachesWithConfiguredEventTtl()
    {
        var @event = CreateEvent();
        _repository.Seed(@event);

        await _sut.GetEventByIdAsync(@event.Id);

        Assert.Equal(TestCacheOptions.EventTtl, _cache.Expirations[CacheKeys.Event(@event.Id)]);
    }

    [Fact]
    public async Task GetTopEventsCachedAsync_CacheMiss_CachesWithConfiguredTopEventsTtl()
    {
        _repository.Seed(CreateEvent());

        await _sut.GetTopEventsCachedAsync();

        Assert.Equal(TestCacheOptions.TopEventsTtl, _cache.Expirations[CacheKeys.Top10]);
    }

    [Fact]
    public async Task CreateEventAsync_CachedTop10_InvalidatesTop10()
    {
        _repository.Seed(CreateEvent());
        await _sut.GetTopEventsCachedAsync(); // прогрев кэша топ-10
        Assert.NotNull(await _cache.GetAsync<List<EventInfo>>(CacheKeys.Top10));

        var request = new CreateEvent
        {
            Title = "Новое событие",
            StartAt = DateTime.UtcNow.AddDays(3),
            EndAt = DateTime.UtcNow.AddDays(3).AddHours(2),
            TotalSeats = 50
        };
        await _sut.CreateEventAsync(request);

        Assert.Null(await _cache.GetAsync<List<EventInfo>>(CacheKeys.Top10));
    }

    #endregion

    private static Event CreateEvent(string title = "Концерт") =>
        Event.Create(title, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "Описание");

    private static EventInfo ToInfo(Event @event, string? title = null) => new()
    {
        Id = @event.Id,
        Title = title ?? @event.Title,
        Description = @event.Description,
        StartAt = @event.StartAt,
        EndAt = @event.EndAt,
        TotalSeats = @event.TotalSeats,
        AvailableSeats = @event.AvailableSeats
    };
}
