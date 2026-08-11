using EventHub.Api.Contracts.Booking;
using EventHub.Api.Models;

namespace EventHub.Api.Common.Extensions.Booking;

public static class BookingMappingExtensions
{
    public static BookingCreateDto ToCreateDto(this Models.Booking.Booking @booking) =>
        new(@booking.Id, @booking.EventId, @booking.Status);

    public static BookingDto ToDto(this Models.Booking.Booking @booking) =>
        new(@booking.Id, @booking.EventId, @booking.Status, booking.CreatedAt, booking.ProcessedAt);

}