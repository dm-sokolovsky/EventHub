using EventHub.Api.Models;

namespace EventHub.Api.Services;

public class EventService : IEventService
{
    
    // Коллекция для манипуляции над событиями
    private static List<Event> Events { get; set; } = [];
    
    public List<Event> GetEvents() => Events;

    public Event? GetEventById(Guid id) => Events.FirstOrDefault(x => x.Id == id);

    public void CreateEvent(Event newEvent) => Events.Add(newEvent);

    public Event? UpdateEvent(Guid id, Event updatedEvent)
    {
        var index = Events.FindIndex(x => x.Id == id);
        
        if (index == -1)
            return null;

        updatedEvent.Id = id;
        
        Events[index] = updatedEvent;
        return updatedEvent;
    }

    public bool DeleteEvent(Guid id)
    {
        var index = Events.FindIndex(x => x.Id == id);
        if (index == -1) return false;

        Events.RemoveAt(index);
        return true;
    }
}