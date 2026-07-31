using EventHub.Api.Models;
using EventHub.Api.Services;

namespace EventHub.Tests;

public class EventServiceFixture
{
    public EventService EventService { get; set; }
    public IEnumerable<Event> Events { get; set; }

    public EventServiceFixture()
    {
        EventService = new EventService();
        Events = new List<Event>()
        {
            new Event("test1", "test1",  DateTime.UtcNow,  DateTime.UtcNow + TimeSpan.FromDays(1)),
            new Event("test2", "test2",  DateTime.UtcNow,  DateTime.UtcNow + TimeSpan.FromDays(2)),
            new Event("test3", "test3",  DateTime.UtcNow,  DateTime.UtcNow + TimeSpan.FromDays(3)),
            new Event("test4", "test4",  DateTime.UtcNow,  DateTime.UtcNow + TimeSpan.FromDays(4))
        };
    }
}