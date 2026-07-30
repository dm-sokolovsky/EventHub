using EventHub.Api.Models;
using EventHub.Api.Extensions;

namespace EventHub.Api.Services;

public class EventService : IEventService
{
    
    // Коллекция для манипуляции над событиями
    private static List<Event> Events { get; } = [];

    public (List<Event> Items, int TotalCount) GetEvents(EventFilter eventFilter, int page, int pageSize)
    {
        var filtered = Events.AsQueryable()
            .TitleFilter(eventFilter.Title)
            .FromDateFilter(eventFilter.From)
            .ToDateFilter(eventFilter.To);

        var totalCount = filtered.Count();
        var items = filtered.Page(page, pageSize).ToList();

        return (items, totalCount);
    }



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