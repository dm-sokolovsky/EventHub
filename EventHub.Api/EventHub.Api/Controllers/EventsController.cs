using System.Net;
using EventHub.Api.Common;
using EventHub.Api.Contracts;
using EventHub.Api.Extensions;
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
    /// <response code="400">Возвращается JSON-структура ApiBaseResult, если page или pageSize меньше 1</response>
    /// <returns></returns>
    [ProducesResponseType(typeof(ApiBaseResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResult<List<EventDto>>), StatusCodes.Status200OK)]
    [Produces("application/json")]
    [HttpGet]
    public IActionResult GetAllEvents([FromQuery] EventFilterDto eventFilterDto, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        ApiBaseResult response;
        
        if (page < 1 || pageSize < 1)
        {
            response = new ApiBaseResult
            {
                Success = false,
                StatusCode = HttpStatusCode.BadRequest,
                Message = $"page и pageSize должны быть не меньше 1 (page={page}, pageSize={pageSize})"
            };

            return response.ToActionResult();
        }

        var eventFilter = eventFilterDto.ToEventFilter();

        var (events, totalCount) = eventService.GetEvents(eventFilter, page, pageSize);
        var eventDtos = events.Select(e => e.ToDto()).ToList();

        var result = new PaginatedResult(
            totalCount,
            eventDtos,
            page,
            pageSize);
        
        response = new ApiResult<PaginatedResult>
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
                Data = result.ToDto(),
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
    [ProducesResponseType(typeof(ApiResult<EventDto>), StatusCodes.Status201Created)]
    [Produces("application/json")]
    [HttpPost]
    public IActionResult CreateEvent([FromBody] EventUpsertDto eventDto)
    {
        var created = eventService.CreateEvent(eventDto.ToEvent());

        var response = new ApiResult<EventDto>
        {
            Data = created.ToDto(),
            Success = true,
            StatusCode = HttpStatusCode.Created,
            Message = "Добавляем событие в коллекцию и возвращаем HTTP 201 Created"
        };

        return CreatedAtAction(nameof(GetEventById), new { id = created.Id }, response);
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
    public IActionResult UpdateEvent(Guid id, [FromBody] EventUpsertDto eventDto)
    {
        var result = eventService.UpdateEvent(id, eventDto.ToEvent());
        
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
            Data = result.ToDto(),
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
}