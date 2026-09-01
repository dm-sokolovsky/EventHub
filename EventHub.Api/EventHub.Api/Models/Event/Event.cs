using System.ComponentModel.DataAnnotations;

namespace EventHub.Api.Models.Event;

public class Event
{
    /// <summary>
    /// Id события
    /// </summary>
    public Guid Id { get; private set; }
    
    /// <summary>
    /// Заголовок события 
    /// </summary>
    public string Title { get; private set; } 

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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="title"></param>
    /// <param name="description"></param>
    /// <param name="startAt"></param>
    /// <param name="endAt"></param>
    /// /// <param name="totalSeats"></param>
    public Event(string title, string? description, DateTime startAt, DateTime endAt, int totalSeats)
    {
        ValidatePeriod(startAt, endAt);
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

    /// <summary>
    /// Метод для обновления события 
    /// </summary>
    /// <param name="title"></param>
    /// <param name="description"></param>
    /// <param name="startAt"></param>
    /// <param name="endAt"></param>
    /// <exception cref="ArgumentException"></exception>
    public void UpdateDetails(string title, string? description, DateTime startAt, DateTime endAt)
    {
        ValidatePeriod(startAt, endAt);
        Title = title;
        Description = description;
        StartAt = startAt;
        EndAt = endAt;
    }
    
    private static void ValidatePeriod(DateTime startAt, DateTime endAt)
    {
        if (endAt <= startAt)
            throw new ArgumentException("EndAt должен быть позже StartAt");
    }
}