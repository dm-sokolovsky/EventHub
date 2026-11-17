namespace BookingsService.Infrastructure.Models;

/// <summary>
/// Локальная копия события (read model), поддерживаемая по сообщениям
/// топика events-changed из EventsService
/// </summary>
public sealed class LocalEvent
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
}
