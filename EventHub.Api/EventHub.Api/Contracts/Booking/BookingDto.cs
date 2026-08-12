using EventHub.Api.Models.Booking;

namespace EventHub.Api.Contracts.Booking;

/// <summary>
/// DTO для отображения брони 
/// </summary>
/// <param name="Id">Id брони</param>
/// <param name="EventId">Id события</param>
/// <param name="Status">Статус брони</param>
/// <param name="CreatedAt">Дата и время создания брони</param>
/// <param name="ProcessedAt">Дата и время обработки брони</param>
public record BookingDto(
    Guid Id,
    Guid EventId,
    BookingStatus Status,
    DateTime CreatedAt,
    DateTime? ProcessedAt
);