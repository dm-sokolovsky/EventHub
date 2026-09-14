using EventHub.Api.Models;

namespace EventHub.Api.Contracts;

/// <summary>
/// DTO для отображения брони 
/// </summary>
/// <param name="Id">Id брони</param>
/// <param name="EventId">Id события</param>
/// <param name="Status">Статус брони</param>
/// <param name="CreatedAt">Дата и время создания брони</param>
/// <param name="ProcessedAt">Дата и время обработки брони</param>
public record BookingInfo
{
    public required Guid Id { get; init; }
    public required Guid EventId { get; init; }
    public required BookingStatus Status { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
}