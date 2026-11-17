using BookingsService.Domain.Models;

namespace BookingsService.Application.Contracts;

/// <summary>
/// DTO для отображения брони
/// </summary>
public record BookingInfo
{
    public required Guid Id { get; init; }
    public required Guid EventId { get; init; }
    public required BookingStatus Status { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
}
