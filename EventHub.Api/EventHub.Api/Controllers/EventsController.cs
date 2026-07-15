using System.Net;
using EventHub.Api.Models;
using EventHub.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController(IEventService  eventService): ControllerBase
{
    [HttpGet]
    public ApiResult<List<EventDto>> GetAllEvents()
    {
        var result = eventService.GetEvents()
            .Select(MapEventDto)
            .ToList();
        
        return new ApiResult<List<EventDto>>
        {
            Data = result,
            Success = true,
            StatusCode = HttpStatusCode.OK,
            Message = "Получаем все события"
        };
    }
    
    [HttpGet("{id}")]
    public ApiBaseResult GetEventById(Guid id)
    {
        try
        {
            var result = eventService
                .GetEventById(id);

            if (result is null)
            {
                return new ApiBaseResult
                {
                    Success = true,
                    StatusCode = HttpStatusCode.NotFound,
                    Message = $"Не удалось найти событие по {id}"
                };
            }

            return new ApiResult<EventDto>
            {
                Data = MapEventDto(result),
                Success = true,
                StatusCode = HttpStatusCode.OK,
                Message = "Получаем событие по id из коллекции"
            };
        }
        catch (ArgumentOutOfRangeException ex)
        {
            // В случае ошибки возвращаем неуспешный результат со статусом Not Found
            return new ApiResult
            {
                Success = false,
                StatusCode = HttpStatusCode.NotFound,
                Message = $"Не удалось найти событие по {id}"
            };
        }
        catch (Exception ex)
        {
            return new ApiResult
            {
                Success = false,
                StatusCode = HttpStatusCode.InternalServerError,
                Message = $"Необработанное исключение: {ex.Message}"
            };
        }
    }

    [HttpPost]
    public ApiResult CreateEvent(EventCreatedDto eventDto)
    {
        eventService.CreateEvent(MapEvent(eventDto));
        
        return new ApiResult
        {
            Success = true,
            StatusCode = HttpStatusCode.Created,
            Message = "Добавляем событие в коллекцию и возвращаем HTTP 201 Created"
        };
    }

    [HttpPut("{id}")]
    public ApiBaseResult UpdateEvent(Guid id, EventCreatedDto eventDto)
    {
        var result = eventService.UpdateEvent(id, MapEvent(eventDto));
        
        if (result is null)
        {
            return new ApiBaseResult
            {
                Success = true,
                StatusCode = HttpStatusCode.NotFound,
                Message = $"Не удалось найти событие по {id}"
            };
        }
        
        return new ApiResult<EventDto>
        {
            Data = MapEventDto(result),
            Success = true,
            StatusCode = HttpStatusCode.OK,
            Message = "Меняем событие в коллекции по id"
        };
    }

    [HttpDelete("{id}")]
    public ApiBaseResult DeleteEvent(Guid id)
    {
        var success = eventService.DeleteEvent(id);

        if (!success)
        {
            return new ApiBaseResult
            {
                Success = true,
                StatusCode = HttpStatusCode.NotFound,
                Message = $"Не удалось найти событие по {id}"
            };
        }
        
        return new ApiResult
        {
            Success = true,
            StatusCode = HttpStatusCode.OK,
            Message = "Удаляем событие из коллекции и возвращаем HTTP 200 OK"
        };
    }


    private Event MapEvent(EventCreatedDto eventDto) 
        => new Event(eventDto.Title, eventDto.Description, eventDto.StartAt, eventDto.EndAt);

    private EventDto MapEventDto(Event @event) 
        => new EventDto(@event.Id, @event.Title, @event.Description, @event.StartAt, @event.EndAt);
}