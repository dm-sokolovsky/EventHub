using EventHub.Api.Common.Exceptions;

namespace EventHub.Api.Models;

/// <summary>
/// Бронирование 
/// </summary>
public sealed class Booking
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

    public Event? Event { get; private set; }

    private Booking() {}
    
    /// <summary>
    /// Создание бронирование
    /// </summary>
    /// <param name="id"></param>
    /// <param name="eventId"></param>
    /// <param name="status"></param>
    /// <param name="createdAt"></param>
    private Booking(
        Guid id, 
        Guid eventId, 
        BookingStatus status, 
        DateTime createdAt
        )
    {
        Id = id;
        EventId = eventId;
        Status = status;
        CreatedAt = createdAt;
    }

    public static Booking CreatePending(Guid eventId)
    {
        if (eventId == Guid.Empty)
            throw new ValidationException(nameof(EventId), "EventId cannot be empty");

        return new Booking(Guid.NewGuid(), eventId, BookingStatus.Pending, DateTime.UtcNow);
    }

    public void Confirm()
    {
        Status = BookingStatus.Confirmed;
        ProcessedAt = DateTime.UtcNow;
    }

    public void Reject()
    {
        Status = BookingStatus.Rejected;
        ProcessedAt = DateTime.UtcNow;
    }
}
