using EventHub.Api.Models;
using EventHub.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController(IEventService  eventService): ControllerBase
{
    [HttpGet]
    public IActionResult GetAllEvents()
    {
        var result = eventService.GetEvents()
            .Select(MapEventDto);
        return Ok(result);
    }
    
    [HttpGet("{id}")]
    public IActionResult GetEventById(Guid id)
    {
        var result = eventService
            .GetEventById(id);
        
        return result is null ? NotFound(): Ok(MapEventDto(result));
    }

    [HttpPost]
    public IActionResult CreateEvent(EventDto eventDto)
    {
        eventService.CreateEvent(MapEvent(eventDto));
        return Ok(eventDto);
    }

    [HttpPut("{id}")]
    public IActionResult UpdateEvent(Guid id, EventDto eventDto)
    {
        var result = eventService.UpdateEvent(id, MapEvent(eventDto));
        return result is null ? NotFound(): Ok(MapEventDto(result));
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteEvent(Guid id)
    {
        var result = eventService.DeleteEvent(id);
        
        return result ? Ok() : NotFound();
    }


    private Event MapEvent(EventDto eventDto) 
        => new Event(eventDto.Title, eventDto.Description, eventDto.StartAt, eventDto.EndAt);

    private EventDto MapEventDto(Event @event) 
        => new EventDto(@event.Id, @event.Title, @event.Description, @event.StartAt, @event.EndAt);
}