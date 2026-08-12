using System.Net;
using EventHub.Api.Common;
using EventHub.Api.Common.Exceptions;
using EventHub.Api.Common.Extensions.Booking;
using EventHub.Api.Contracts.Booking;
using EventHub.Api.Models;
using EventHub.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Controllers;

/// <summary>
/// Контроллер для манипуляции над бронью
/// </summary>
/// <param name="bookingService"></param>
[ApiController]
[Route("api/bookings")]
public class BookingsController(IBookingService bookingService): ControllerBase
{
    public const string Name = "Bookings";
    
    /// <summary>
    /// Метод получает бронь по id брони
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="NotFoundException"></exception>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetBookingById(Guid id)
    {
        var booking = await bookingService.GetBookingByIdAsync(id)
                      ?? throw new NotFoundException($"Не удалось найти бронь по {id}");
        
        var response = new ApiResult<BookingDto>
        {
            Data = booking.ToDto(),
            Success = true,
            StatusCode = HttpStatusCode.OK,
            Message = "Получаем бронь по id из коллекции"
        };

        return response.ToActionResult();
    }
}