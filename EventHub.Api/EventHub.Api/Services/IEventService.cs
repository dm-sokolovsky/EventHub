using EventHub.Api.Models;

namespace EventHub.Api.Services;

public interface IEventService
{
    (List<Event> Items, int TotalCount) GetEvents(EventFilter eventFilter, int page, int pageSize);
    Event? GetEventById(Guid id);
    Event CreateEvent(Event newEvent);
    Event? UpdateEvent(Guid id, Event updatedEvent);
    bool DeleteEvent(Guid id);
}