namespace EventHub.Api.Models.Booking;

/// <summary>
/// Бронирование 
/// </summary>
public class Booking
{
    /// <summary>
    /// Уникальный идентификатор брони
    /// </summary>
    public Guid Id { get; private set; }
    
    /// <summary>
    /// Идентификатор события, к которому относится бронь
    /// </summary>
    public Guid EventId { get; private set; }
    
    /// <summary>
    /// Текущий статус брони
    /// </summary>
    public BookingStatus Status { get; private set; }
    
    /// <summary>
    /// Дата и время создания брони
    /// </summary>
    public DateTime CreatedAt { get; private set; }
    
    /// <summary>
    /// Дата и время обработки брони
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Создание бронирование 
    /// </summary>
    /// <param name="eventId"></param>
    public Booking(Guid eventId)
    {
        Id = Guid.NewGuid();
        EventId = eventId;
        Status = BookingStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }
}
