using EventHub.Api.Models;
using EventHub.Api.Models.Event;
using EventHub.Api.Services;

namespace EventHub.Tests;

[Collection("EventService collection")]
public class EventServiceFilterTests
{
    private readonly IEnumerable<Event> _events;
    private readonly EventService _eventService;

    public EventServiceFilterTests(EventServiceFixture fixture)
    {
        _events = fixture.Events;
        _eventService = fixture.EventService;
    }

    [Fact]
    public void EventService_FilterByTitle_ReturnsOnlyMatchingEvents()
    {
        var uniqueTitle = $"title_{Guid.NewGuid()}";
        var matching = new Event(uniqueTitle, "desc", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 10);
        var nonMatching = new Event($"other_{Guid.NewGuid()}", "desc", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 10);

        _eventService.CreateEvent(matching);
        _eventService.CreateEvent(nonMatching);

        var filter = new EventFilter(uniqueTitle, null, null);
        var (items, totalCount) = _eventService.GetEvents(filter, page: 1, pageSize: 10);

        Assert.Equal(1, totalCount);
        Assert.Equal(matching.Id, Assert.Single(items).Id);
    }

    [Fact]
    public void EventService_FilterByDateRange_ReturnsOnlyEventsWithinRange()
    {
        var uniqueTitle = $"date_range_{Guid.NewGuid()}";

        var inRange = new Event(uniqueTitle, "desc", new DateTime(2026, 1, 10), new DateTime(2026, 1, 11), 10);
        var outOfRange = new Event(uniqueTitle, "desc", new DateTime(2026, 3, 10), new DateTime(2026, 3, 11), 10);

        _eventService.CreateEvent(inRange);
        _eventService.CreateEvent(outOfRange);

        var filter = new EventFilter(uniqueTitle, new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));
        var (items, totalCount) = _eventService.GetEvents(filter, page: 1, pageSize: 10);

        Assert.Equal(1, totalCount);
        Assert.Equal(inRange.Id, Assert.Single(items).Id);
    }

    [Fact]
    public void EventService_CombinedFilter_TitleAndDateRange_ReturnsOnlyEventsMatchingBoth()
    {
        var titlePrefix = $"combo_{Guid.NewGuid()}";
        var from = new DateTime(2026, 5, 1);
        var to = new DateTime(2026, 5, 31);

        var matchesBoth = new Event($"{titlePrefix}_match", "desc", new DateTime(2026, 5, 10), new DateTime(2026, 5, 11), 10);
        var matchesTitleOnly = new Event($"{titlePrefix}_match", "desc", new DateTime(2026, 7, 10), new DateTime(2026, 7, 11), 10);
        var matchesDateOnly = new Event($"other_{Guid.NewGuid()}", "desc", new DateTime(2026, 5, 10), new DateTime(2026, 5, 11), 10);

        _eventService.CreateEvent(matchesBoth);
        _eventService.CreateEvent(matchesTitleOnly);
        _eventService.CreateEvent(matchesDateOnly);

        var filter = new EventFilter(titlePrefix, from, to);
        var (items, totalCount) = _eventService.GetEvents(filter, page: 1, pageSize: 10);

        Assert.Equal(1, totalCount);
        Assert.Equal(matchesBoth.Id, Assert.Single(items).Id);
    }
}
