using EventHub.Api.Models;
using EventHub.Api.Services;

namespace EventHub.Tests;

[Collection("EventService collection")]
public class EventServiceTests
{
    private readonly IEnumerable<Event> _events;
    private readonly EventService _eventService;

    public EventServiceTests(EventServiceFixture fixture)
    {
        _events = fixture.Events;
        _eventService = fixture.EventService;
    }

    [Fact]
    public void EventService_AddEvent()
    {
        var startAt = DateTime.UtcNow;
        var endAt = DateTime.UtcNow + TimeSpan.FromDays(1);
        var @event = new Event("test_new", "test_new", startAt, endAt);

        _eventService.CreateEvent(@event);
        var created = _eventService.GetEventById(@event.Id);

        Assert.NotNull(created);
        Assert.Equal(@event.Id, created.Id);
        Assert.Equal("test_new", created.Title);
        Assert.Equal("test_new", created.Description);
        Assert.Equal(startAt, created.StartAt);
        Assert.Equal(endAt, created.EndAt);
    }

    [Fact]
    public void EventService_GetAllEvents()
    {
        var uniqueTitle = $"title_{Guid.NewGuid()}";
        var startAt = DateTime.UtcNow;
        var endAt = startAt + TimeSpan.FromDays(1);

        var event1 = new Event(uniqueTitle, "desc1", startAt, endAt);
        var event2 = new Event(uniqueTitle, "desc2", startAt, endAt);

        _eventService.CreateEvent(event1);
        _eventService.CreateEvent(event2);

        var filter = new EventFilter(uniqueTitle, null, null);
        var (items, totalCount) = _eventService.GetEvents(filter, page: 1, pageSize: 10);

        Assert.Equal(2, totalCount);
        Assert.Equal(2, items.Count);
        Assert.Contains(items, e => e.Id == event1.Id);
        Assert.Contains(items, e => e.Id == event2.Id);
    }

    [Fact]
    public void EventService_GetEventById_ReturnsEvent()
    {
        var @event = new Event("get_by_id", "get_by_id", DateTime.UtcNow, DateTime.UtcNow.AddDays(1));
        _eventService.CreateEvent(@event);

        var result = _eventService.GetEventById(@event.Id);

        Assert.NotNull(result);
        Assert.Equal(@event.Id, result.Id);
        Assert.Equal("get_by_id", result.Title);
    }

    [Fact]
    public void EventService_GetEventById_NotFound_ReturnsNull()
    {
        var result = _eventService.GetEventById(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public void EventService_UpdateEvent()
    {
        var startAt = DateTime.UtcNow;
        var endAt = startAt + TimeSpan.FromDays(1);
        var original = new Event("update_original", "update_original", startAt, endAt);
        _eventService.CreateEvent(original);

        var newStartAt = startAt.AddDays(2);
        var newEndAt = endAt.AddDays(2);
        var updatedEvent = new Event("update_new", "update_new", newStartAt, newEndAt);

        var result = _eventService.UpdateEvent(original.Id, updatedEvent);

        Assert.NotNull(result);
        Assert.Equal(original.Id, result.Id);
        Assert.Equal("update_new", result.Title);
        Assert.Equal("update_new", result.Description);
        Assert.Equal(newStartAt, result.StartAt);
        Assert.Equal(newEndAt, result.EndAt);

        var fetched = _eventService.GetEventById(original.Id);
        Assert.NotNull(fetched);
        Assert.Equal("update_new", fetched.Title);
    }

    [Fact]
    public void EventService_UpdateEvent_NotFound_ReturnsNull()
    {
        var updatedEvent = new Event("no_such_event", "no_such_event", DateTime.UtcNow, DateTime.UtcNow.AddDays(1));

        var result = _eventService.UpdateEvent(Guid.NewGuid(), updatedEvent);

        Assert.Null(result);
    }

    [Fact]
    public void EventService_DeleteEvent_RemovesExistingEvent()
    {
        var @event = new Event("to_delete", "to_delete", DateTime.UtcNow, DateTime.UtcNow.AddDays(1));
        _eventService.CreateEvent(@event);

        var deleted = _eventService.DeleteEvent(@event.Id);

        Assert.True(deleted);
        Assert.Null(_eventService.GetEventById(@event.Id));
    }
}
