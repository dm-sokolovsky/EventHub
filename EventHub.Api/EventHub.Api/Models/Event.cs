using System.ComponentModel.DataAnnotations;
using EventHub.Api.Common.Exceptions;
using ValidationException = EventHub.Api.Common.Exceptions.ValidationException;

namespace EventHub.Api.Models;

public sealed class Event
{
    /// <summary>
    /// Id события
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Заголовок события 
    /// </summary>
    public string Title { get; private set; } = null!;

    /// <summary>
    /// Описание события 
    /// </summary>
    public string? Description { get; private set; }
    
    /// <summary>
    /// Начало события
    /// </summary>
    public DateTime StartAt { get; private set; }
    
    /// <summary>
    /// Окончание события 
    /// </summary>
    public DateTime EndAt { get; private set; }
    
    /// <summary>
    /// Общее количество мест на событии
    /// </summary>
    public int TotalSeats { get; private set; }
    
    /// <summary>
    /// Текущее количество свободных мест; при сосоздании равно TotalSeats
    /// </summary>
    public int AvailableSeats { get; private set; }

    public ICollection<Booking> Bookings { get; private set; } = [];
    
    private Event() {}
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="title"></param>
    /// <param name="description"></param>
    /// <param name="startAt"></param>
    /// <param name="endAt"></param>
    /// <param name="totalSeats"></param>
    private Event(
        Guid id,
        string title, 
        string? description, 
        DateTime startAt, 
        DateTime endAt, 
        int totalSeats)
    {
        Id = id;
        Title = title;
        Description = description;
        StartAt = startAt;
        EndAt = endAt;
        TotalSeats = totalSeats;
        AvailableSeats = totalSeats;
    }

    public static Event Create(
        string? title,
        DateTime? startAt,
        DateTime? endAt,
        int? totalSeats = null,
        string? description = null
        )
    {
        ThrowIfNotValid(title, startAt, endAt, totalSeats);
        
        return new Event(Guid.NewGuid(), title!.Trim(), description, startAt!.Value, endAt!.Value, totalSeats!.Value);
    }
    
    public void Update(
        string? title,
        DateTime? startAt,
        DateTime? endAt,
        string? description = null,
        int? totalSeats = null)
    {
        ThrowIfNotValid(title, startAt, endAt, totalSeats);

        Title = title!;
        StartAt = startAt!.Value;
        EndAt = endAt!.Value;
        Description = description;
        TotalSeats = totalSeats!.Value;
    }
    
    public bool TryReserveSeats(int count = 1)
    {
        if (AvailableSeats < count)
            return false;

        AvailableSeats -= count;
        return true;
    }

    public void ReleaseSeats(int count = 1)
    {
        AvailableSeats = Math.Min(TotalSeats, AvailableSeats + count);
    }
    
    private static void ThrowIfNotValid(
        string? title,
        DateTime? startAt,
        DateTime? endAt,
        int? totalSeats)
    {
        var errors = new Dictionary<string, ICollection<string>>();

        if (string.IsNullOrWhiteSpace(title))
            AddError(errors, nameof(Title), "Title cannot be empty");

        if (!startAt.HasValue)
            AddError(errors, nameof(StartAt), "Start time cannot be null");

        if (!endAt.HasValue)
            AddError(errors, nameof(EndAt), "End time cannot be null");

        if (startAt < DateTime.UtcNow)
            AddError(errors, nameof(StartAt), "Event cannot start in the past");

        if (endAt <= startAt)
            AddError(errors, nameof(EndAt), "End time must be after start time");

        if (!totalSeats.HasValue || totalSeats.Value <= 0)
            AddError(errors, nameof(TotalSeats), "TotalSeats must be greater than zero");

        if (errors.Any())
            throw new ValidationException(errors);
    }

    private static void AddError(Dictionary<string, ICollection<string>> errors, string field, string message)
    {
        if (!errors.ContainsKey(field))
            errors[field] = new List<string>();

        errors[field].Add(message);
    }
}