using EventHub.Api.Models;
using EventHub.Api.Services;

namespace EventHub.Tests;

[Collection("EventService collection")]
public class EventServicePaginatedTests
{
    private readonly IEnumerable<Event> _events;
    private readonly EventService _eventService;

    public EventServicePaginatedTests(EventServiceFixture fixture)
    {
        _events = fixture.Events;
        _eventService = fixture.EventService;
    }

    [Fact]
    public void EventService_Pagination_ReturnsCorrectPagesAndTotalCount()
    {
        var uniqueTitle = $"page_{Guid.NewGuid()}";
        for (var i = 0; i < 5; i++)
        {
            _eventService.CreateEvent(new Event(uniqueTitle, $"desc{i}", DateTime.UtcNow, DateTime.UtcNow.AddDays(1)));
        }

        var filter = new EventFilter(uniqueTitle, null, null);

        var (firstPage, totalCount) = _eventService.GetEvents(filter, page: 1, pageSize: 2);
        var (secondPage, _) = _eventService.GetEvents(filter, page: 2, pageSize: 2);
        var (thirdPage, _) = _eventService.GetEvents(filter, page: 3, pageSize: 2);

        Assert.Equal(5, totalCount);
        Assert.Equal(2, firstPage.Count);
        Assert.Equal(2, secondPage.Count);
        Assert.Single(thirdPage);

        var allIds = firstPage.Concat(secondPage).Concat(thirdPage).Select(e => e.Id).ToHashSet();
        Assert.Equal(5, allIds.Count);
    }
}
