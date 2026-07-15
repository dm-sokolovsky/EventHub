using EventHub.Api.Models;

namespace EventHub.Api.Services;

public interface IEventService
{
    List<Event> GetEvents();
    Event? GetEventById(Guid id);
    void CreateEvent(Event newEvent);
    Event? UpdateEvent(Event updatedEvent);
    bool DeleteEvent(Guid id);
}