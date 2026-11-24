using EventsService.Application.Contracts;
using EventsService.Application.Services;
using EventsService.Domain.Models;

namespace EventsService.Tests;

public sealed class EventServiceCacheTests
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);
    private const string TopCacheKey = "event:top10";

    private readonly FakeEventRepository _repository = new();
    private readonly FakeCacheService _cache = new();
    private readonly EventService _sut;

    public EventServiceCacheTests()
    {
        _sut = new EventService(_repository, new FakeEventPublisher(), _cache);
    }

    #region GetEventByIdAsync

    [Fact]
    public async Task GetEventByIdAsync_CacheHit_DoesNotCallRepository()
    {
        var @event = CreateEvent();
        _repository.Seed(@event);
        await _cache.SetAsync(CacheKey(@event.Id), ToInfo(@event, title: "Из кэша"), Ttl);

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

        var cached = await _cache.GetAsync<EventInfo>(CacheKey(@event.Id));
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
        await _cache.SetAsync(TopCacheKey, new List<EventInfo> { ToInfo(CreateEvent()) }, Ttl);

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

        var cached = await _cache.GetAsync<List<EventInfo>>(TopCacheKey);
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
        await _sut.GetEventByIdAsync(@event.Id); // прогрев кэша
        Assert.NotNull(await _cache.GetAsync<EventInfo>(CacheKey(@event.Id)));

        var request = new EventUpsert(
            "Новое название",
            null,
            DateTime.UtcNow.AddDays(2),
            DateTime.UtcNow.AddDays(2).AddHours(3),
            100);
        await _sut.UpdateEventAsync(@event.Id, request);

        Assert.Null(await _cache.GetAsync<EventInfo>(CacheKey(@event.Id)));

        // Следующее чтение идёт в репозиторий и возвращает свежие данные, а не устаревшие из кэша.
        var fresh = await _sut.GetEventByIdAsync(@event.Id);
        Assert.Equal("Новое название", fresh.Title);
    }

    [Fact]
    public async Task DeleteEventAsync_CachedEvent_RemovesCacheEntry()
    {
        var @event = CreateEvent();
        _repository.Seed(@event);
        await _sut.GetEventByIdAsync(@event.Id); // прогрев кэша

        var deleted = await _sut.DeleteEventAsync(@event.Id);

        Assert.True(deleted);
        Assert.Null(await _cache.GetAsync<EventInfo>(CacheKey(@event.Id)));
    }

    #endregion

    private static string CacheKey(Guid id) => $"event:{id}";

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
