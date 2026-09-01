using System.Net;
using EventHub.Api.Common;
using EventHub.Api.Common.Exceptions;
using EventHub.Api.Common.Extensions.Booking;
using EventHub.Api.Contracts;
using EventHub.Api.Contracts.Booking;
using EventHub.Api.Extensions.Event;
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
public class EventsController(IEventService  eventService, IBookingService bookingService): ControllerBase
{
    public const string Name = "Events";
    
    /// <summary>
    /// Метод возвращает все события из коллекции
    /// </summary>
    /// <response code="200">Возвращается JSON-структура ApiResult с деталями ответа</response>
    /// <response code="400">Возвращается JSON-структура ApiBaseResult, если page или pageSize меньше 1</response>
    /// <returns></returns>
    [ProducesResponseType(typeof(ApiBaseResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResult<PaginatedResult<EventDto>>), StatusCodes.Status200OK)]
    [Produces("application/json")]
    [HttpGet]
    public IActionResult GetAllEvents([FromQuery] EventFilterDto eventFilterDto, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page < 1 || pageSize < 1)
        {
            throw new BadRequestException($"page и pageSize должны быть не меньше 1 (page={page}, pageSize={pageSize})");
        }

        var eventFilter = eventFilterDto.ToEventFilter();

        var (events, totalCount) = eventService.GetEvents(eventFilter, page, pageSize);
        var eventDtos = events.Select(e => e.ToDto()).ToList();

        var result = new PaginatedResult<EventDto>(
            totalCount,
            eventDtos,
            page,
            pageSize);

        var response = new ApiResult<PaginatedResult<EventDto>>
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
    /// <returns></returns>
    [ProducesResponseType(typeof(ApiBaseResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResult<EventInfoDto>), StatusCodes.Status200OK)]
    [Produces("application/json")]
    [HttpGet("{id}")]
    public IActionResult GetEventById(Guid id)
    {
        var result = eventService.GetEventById(id)
            ?? throw new NotFoundException($"Не удалось найти событие по {id}");

        var response = new ApiResult<EventInfoDto>
        {
            Data = result.ToDto(),
            Success = true,
            StatusCode = HttpStatusCode.OK,
            Message = "Получаем событие по id из коллекции"
        };

        return response
            .ToActionResult();
    }

    /// <summary>
    /// Метод добавляет новое событие в коллекцию
    /// </summary>
    /// <param name="eventDto">Параметр eventDto, для добавления нового события в коллекцию</param>
    /// <response code="201">Возвращается JSON-структура ApiResult с деталями ответа</response>
    /// <response code="400">Возвращается стандартный ValidationProblemDetails</response>
    /// <returns></returns>
    [ProducesResponseType(typeof(ApiResult<EventInfoDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    [HttpPost]
    public IActionResult CreateEvent([FromBody] EventUpsertDto eventDto)
    {
        var created = eventService.CreateEvent(eventDto.ToEvent());

        var response = new ApiResult<EventInfoDto>
        {
            Data = created.ToDto(),
            Success = true,
            StatusCode = HttpStatusCode.Created,
            Message = "Добавляем событие в коллекцию и возвращаем HTTP 201 Created"
        };

        return response.ToActionResultWithLocation(nameof(GetEventById), null,new { id = created.Id });
    }

    /// <summary>
    /// Метод обновляет событие из коллекции по id
    /// </summary>
    /// <param name="id">id событие</param>
    /// <param name="eventDto">измененные данные события</param>
    /// <response code="200">Возвращается JSON-структура ApiResult с деталями ответа</response>
    /// <response code="400">Возвращается стандартный ValidationProblemDetails</response>
    /// <response code="404">Возвращается JSON-структура ApiBaseResult с деталями ответа</response>
    /// <returns></returns>
    [ProducesResponseType(typeof(ApiBaseResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResult<EventInfoDto>), StatusCodes.Status200OK)]
    [Produces("application/json")]
    [HttpPut("{id}")]
    public IActionResult UpdateEvent(Guid id, [FromBody] EventUpsertDto eventDto)
    {
        var result = eventService.UpdateEvent(id, eventDto.ToEvent())
            ?? throw new NotFoundException($"Не удалось найти событие по {id}");

        var response = new ApiResult<EventInfoDto>
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
    /// <response code="204">Событие успешно удалено, возвращается JSON-структура ApiBaseResult с деталями ответа</response>
    /// <response code="404">Возвращается JSON-структура ApiBaseResult с деталями ответа</response>
    /// <returns></returns>
    [ProducesResponseType(typeof(ApiBaseResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiBaseResult), StatusCodes.Status204NoContent)]
    [Produces("application/json")]
    [HttpDelete("{id}")]
    public IActionResult DeleteEvent(Guid id)
    {
        var success = eventService.DeleteEvent(id);

        if (!success)
        {
            throw new NotFoundException($"Не удалось найти событие по {id}");
        }

        var response = new ApiBaseResult
        {
            Success = true,
            StatusCode = HttpStatusCode.NoContent,
            Message = "Удаляем событие из коллекции и возвращаем"
        };

        return response
            .ToActionResult();
    }

    /// <summary>
    /// Метод создает бронь по Id события 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="NotFoundException"></exception>
    [ProducesResponseType(typeof(ApiBaseResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiBaseResult), StatusCodes.Status202Accepted)]
    [Produces("application/json")]
    [HttpPost("{id}/book")]
    public async Task<IActionResult> CreateBooking(Guid id)
    {
        var booking = await bookingService.CreateBookingAsync(id);


        var response = new ApiResult<BookingDto>
        {
            Data = booking.ToDto(),
            Success = true,
            StatusCode = HttpStatusCode.Accepted,
            Message = "Добавляем бронь в коллекцию и возвращаем HTTP 202 Accepted"
        };
    
        return response.ToActionResultWithLocation(
            nameof(BookingsController.GetBookingById),
            BookingsController.Name,
            new { id = booking.Id });
        
    }
    
}