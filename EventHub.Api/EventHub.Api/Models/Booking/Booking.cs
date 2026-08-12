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
    public DateTime? ProcessedAt { get;  private set; }

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

    /// <summary>
    /// Метод, который подтверждает бронь
    /// </summary>
    public void Confirm()
    {
        Status = BookingStatus.Confirmed;
        ProcessedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Метод, который отклоняет бронь
    /// </summary>
    public void Reject()
    {
        Status = BookingStatus.Rejected;
        ProcessedAt = DateTime.UtcNow;
    }
}
