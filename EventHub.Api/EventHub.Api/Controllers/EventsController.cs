using System.Net;
using EventHub.Api.Models;
using EventHub.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Controllers;

/// <summary>
/// Контроллер для манипуляции над событиями
/// </summary>
/// <param name="eventService"></param>
[ApiController]
[Route("api/[controller]")]
public class EventsController(IEventService  eventService): ControllerBase
{
    /// <summary>
    /// Метод возвращает все события из коллекции
    /// </summary>
    /// <response code="200">Возвращается JSON-структура ApiResult с деталями ответа</response>
    /// <returns></returns>
    [ProducesResponseType(typeof(ApiResult<List<EventDto>>), StatusCodes.Status200OK)]
    [Produces("application/json")]
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
    
    /// <summary>
    /// Метод возвращает событие по id из коллекции
    /// </summary>
    /// <param name="id">Параметр id, для получения события</param>
    /// <response code="200">Возвращается JSON-структура ApiResult с деталями ответа</response>
    /// <response code="404">Возвращается JSON-структура ApiBaseResult с деталями ответа</response>
    /// <response code="500">Возвращается JSON-структура ApiBaseResult с деталями ответа необработанного исключения</response>
    /// <returns></returns>
    [ProducesResponseType(typeof(ApiBaseResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiBaseResult), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ApiResult<EventDto>), StatusCodes.Status200OK)]
    [Produces("application/json")]
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
            return new ApiBaseResult
            {
                Success = false,
                StatusCode = HttpStatusCode.NotFound,
                Message = $"Не удалось найти событие по {id}"
            };
        }
        catch (Exception ex)
        {
            return new ApiBaseResult
            {
                Success = false,
                StatusCode = HttpStatusCode.InternalServerError,
                Message = $"Необработанное исключение: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Метод добавляет новое событие в коллекцию
    /// </summary>
    /// <param name="eventDto">Параметр eventDto, для добавления нового события в коллекцию</param>
    /// <response code="201">Возвращается JSON-структура ApiResult с деталями ответа</response>
    /// <returns></returns>
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status200OK)]
    [Produces("application/json")]
    [HttpPost]
    public ApiBaseResult CreateEvent(EventCreatedDto eventDto)
    {
        eventService.CreateEvent(MapEvent(eventDto));
        
        return new ApiBaseResult
        {
            Success = true,
            StatusCode = HttpStatusCode.Created,
            Message = "Добавляем событие в коллекцию и возвращаем HTTP 201 Created"
        };
    }

    /// <summary>
    /// Метод обновляет событие из коллекции по id
    /// </summary>
    /// <param name="id">id событие</param>
    /// <param name="eventDto">измененные данные события</param>
    /// <response code="200">Возвращается JSON-структура ApiResult с деталями ответа</response>
    /// <response code="404">Возвращается JSON-структура ApiBaseResult с деталями ответа</response>
    /// <returns></returns>
    [ProducesResponseType(typeof(ApiBaseResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResult<EventDto>), StatusCodes.Status200OK)]
    [Produces("application/json")]
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

    /// <summary>
    /// Метод удаляет событие из коллекции по id
    /// </summary>
    /// <param name="id">id события</param>
    /// <response code="200">Возвращается JSON-структура ApiResult с деталями ответа</response>
    /// <response code="404">Возвращается JSON-структура ApiBaseResult с деталями ответа</response>
    /// <returns></returns>
    [ProducesResponseType(typeof(ApiBaseResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiBaseResult), StatusCodes.Status200OK)]
    [Produces("application/json")]
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
        
        return new ApiBaseResult()
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