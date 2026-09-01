using System.ComponentModel.DataAnnotations;

namespace EventHub.Api.Models;

public class Event
{
    public Guid Id { get; set; }
    
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }
    
    public DateTime StartAt { get; set; }
    
    public DateTime EndAt { get; set; }
    
    /// <summary>
    /// Общее количество мест на событии
    /// </summary>
    public int TotalSeats { get; private set; }
    
    /// <summary>
    /// Текущее количество свободных мест; при сосоздании равно TotalSeats
    /// </summary>
    public int AvailableSeats { get; private set; }

    public Event(string title, string? description, DateTime startAt, DateTime endAt, int totalSeats)
    {
        
        if (totalSeats <= 0) 
            throw new ArgumentOutOfRangeException(nameof(totalSeats));
        
        Id = Guid.NewGuid();
        Title = title;
        Description = description;
        StartAt = startAt;
        EndAt = endAt;
        TotalSeats = totalSeats;
        AvailableSeats = totalSeats;
    }

    /// <summary>
    /// Бронирует свободные места внутри события
    /// </summary>
    /// <param name="count">Кол-во мест</param>
    /// <returns></returns>
    public bool TryReserveSeats(int count = 1)
    {
        if (count > AvailableSeats)
            return  false;

        AvailableSeats -= count;
        return true;
    }
    
    public bool ReleaseSeat(int count = 1)
    {
        return false;
    }
}