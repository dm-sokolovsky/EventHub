using EventHub.Api.Models;

namespace EventHub.Api.Services;

public interface IEventService
{
    List<Event> GetEvents(EventFilter eventFilter);
    Event? GetEventById(Guid id);
    void CreateEvent(Event newEvent);
    Event? UpdateEvent(Guid id, Event updatedEvent);
    bool DeleteEvent(Guid id);
}