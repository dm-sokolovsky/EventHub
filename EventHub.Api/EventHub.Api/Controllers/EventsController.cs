using System.Net;
using EventHub.Api.Common;
using EventHub.Api.Models;
using EventHub.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Controllers;

/// <summary>
/// Контроллер для манипуляции над событиями
/// </summary>
/// <param name="eventService"></param>
[ApiController]
[Route("api/events")]
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
    public IActionResult GetAllEvents([FromQuery] EventFilterDto eventFilterDto)
    {
        var result = eventService.GetEvents(MapEventFilter(eventFilterDto))
            .Select(MapEventDto)
            .ToList();
        
        var response = new ApiResult<List<EventDto>>
        {
            Data = result,
            Success = true,
            StatusCode = HttpStatusCode.OK,
            Message = "Получаем все события"
        };

        return response
            .ToActionResult();
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
    public IActionResult GetEventById(Guid id)
    {
        ApiBaseResult response;
        
        try
        {
            var result = eventService
                .GetEventById(id);

            if (result is null)
            {
                response = new ApiBaseResult
                {
                    Success = false,
                    StatusCode = HttpStatusCode.NotFound,
                    Message = $"Не удалось найти событие по {id}"
                };

                return response
                    .ToActionResult();
            }
            
            response = new ApiResult<EventDto>
            {
                Data = MapEventDto(result),
                Success = true,
                StatusCode = HttpStatusCode.OK,
                Message = "Получаем событие по id из коллекции"
            };

            return response
                .ToActionResult();
        }
        catch (Exception ex)
        {
            response = new ApiBaseResult
            {
                Success = false,
                StatusCode = HttpStatusCode.InternalServerError,
                Message = $"Необработанное исключение: {ex.Message}"
            };
            
            return response
                .ToActionResult();
        }
    }

    /// <summary>
    /// Метод добавляет новое событие в коллекцию
    /// </summary>
    /// <param name="eventDto">Параметр eventDto, для добавления нового события в коллекцию</param>
    /// <response code="201">Возвращается JSON-структура ApiResult с деталями ответа</response>
    /// <returns></returns>
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status201Created)]
    [Produces("application/json")]
    [HttpPost]
    public IActionResult CreateEvent([FromBody] EventCreatedDto eventDto)
    {
        eventService.CreateEvent(MapEvent(eventDto));
        
        var response = new ApiBaseResult
        {
            Success = true,
            StatusCode = HttpStatusCode.Created,
            Message = "Добавляем событие в коллекцию и возвращаем HTTP 201 Created"
        };

        return response
            .ToActionResult();
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
    public IActionResult UpdateEvent(Guid id, [FromBody] EventCreatedDto eventDto)
    {
        var result = eventService.UpdateEvent(id, MapEvent(eventDto));
        
        ApiBaseResult response;
        
        
        if (result is null)
        {
            response = new ApiBaseResult
            {
                Success = false,
                StatusCode = HttpStatusCode.NotFound,
                Message = $"Не удалось найти событие по {id}"
            };

            return response
                .ToActionResult();
        }
        
        response = new ApiResult<EventDto>
        {
            Data = MapEventDto(result),
            Success = true,
            StatusCode = HttpStatusCode.OK,
            Message = "Меняем событие в коллекции по id"
        };

        return response
            .ToActionResult();
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
    public IActionResult DeleteEvent(Guid id)
    {
        var success = eventService.DeleteEvent(id);
        
        ApiBaseResult response;

        if (!success)
        {
            response = new ApiBaseResult
            {
                Success = false,
                StatusCode = HttpStatusCode.NotFound,
                Message = $"Не удалось найти событие по {id}"
            };
            
            return response
                .ToActionResult();
        }

        response = new ApiBaseResult()
        {
            Success = true,
            StatusCode = HttpStatusCode.NoContent,
            Message = "Удаляем событие из коллекции и возвращаем"
        };
        
        return response
            .ToActionResult();
    }


    private Event MapEvent(EventCreatedDto @eventDto) 
        => new Event(@eventDto.Title, @eventDto.Description, @eventDto.StartAt, @eventDto.EndAt);

    private EventDto MapEventDto(Event @event) 
        => new EventDto(@event.Id, @event.Title, @event.Description, @event.StartAt, @event.EndAt);

    private EventFilter MapEventFilter(EventFilterDto @eventFilterDto) =>
        new EventFilter(@eventFilterDto.Title, @eventFilterDto.From, @eventFilterDto.To);
}