using EventHub.Contracts;
using EventsService.Application.Contracts;
using EventsService.Application.Services;
using EventsService.Application.Services.Abstractions;
using EventsService.Domain.Models;
using Microsoft.Extensions.Options;

namespace EventsService.Tests;

public sealed class BookingConfirmedHandlerTests
{
    private readonly FakeEventRepository _repository = new();
    private readonly FakeCacheService _cache = new();
    private readonly EventService _eventService;
    private readonly BookingConfirmedHandler _sut;

    public BookingConfirmedHandlerTests()
    {
        _eventService = new EventService(_repository, new FakeEventPublisher(), _cache, Options.Create(new CacheOptions()));
        _sut = new BookingConfirmedHandler(_repository, _cache);
    }

    [Fact]
    public async Task HandleAsync_ValidBooking_ReservesSeatsAndInvalidatesCache()
    {
        var @event = CreateEvent(totalSeats: 100);
        _repository.Seed(@event);
        await _eventService.GetEventByIdAsync(@event.Id); // прогрев кэша события
        await _eventService.GetTopEventsCachedAsync(); // прогрев кэша топ-10

        var result = await _sut.HandleAsync(Message(@event.Id, seats: 3));

        Assert.Equal(BookingConfirmedResult.Reserved, result);
        Assert.Equal(97, @event.AvailableSeats);
        Assert.Null(await _cache.GetAsync<EventInfo>(CacheKeys.Event(@event.Id)));
        Assert.Null(await _cache.GetAsync<List<EventInfo>>(CacheKeys.Top10));

        // Следующее чтение возвращает свежие данные из репозитория, а не устаревшие из кэша.
        var fresh = await _eventService.GetEventByIdAsync(@event.Id);
        Assert.Equal(97, fresh.AvailableSeats);
    }

    [Fact]
    public async Task HandleAsync_EventNotFound_KeepsCache()
    {
        var @event = CreateEvent();
        _repository.Seed(@event);
        await _eventService.GetEventByIdAsync(@event.Id); // прогрев кэша

        var result = await _sut.HandleAsync(Message(Guid.NewGuid(), seats: 1));

        Assert.Equal(BookingConfirmedResult.EventNotFound, result);
        Assert.Equal(0, _repository.SaveChangesCallCount);
        Assert.NotNull(await _cache.GetAsync<EventInfo>(CacheKeys.Event(@event.Id)));
    }

    [Fact]
    public async Task HandleAsync_NotEnoughSeats_KeepsCacheAndDoesNotSave()
    {
        var @event = CreateEvent(totalSeats: 2);
        _repository.Seed(@event);
        await _eventService.GetEventByIdAsync(@event.Id); // прогрев кэша

        var result = await _sut.HandleAsync(Message(@event.Id, seats: 5));

        Assert.Equal(BookingConfirmedResult.SeatsUnavailable, result);
        Assert.Equal(2, @event.AvailableSeats);
        Assert.Equal(0, _repository.SaveChangesCallCount);
        Assert.NotNull(await _cache.GetAsync<EventInfo>(CacheKeys.Event(@event.Id)));
    }

    private static BookingConfirmed Message(Guid eventId, int seats) =>
        new(Guid.NewGuid(), eventId, Guid.NewGuid(), seats, DateTime.UtcNow);

    private static Event CreateEvent(int totalSeats = 100) =>
        Event.Create("Концерт", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), totalSeats, "Описание");
}
