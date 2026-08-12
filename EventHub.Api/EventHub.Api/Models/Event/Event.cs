using System.ComponentModel.DataAnnotations;

namespace EventHub.Api.Models;

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
    /// 
    /// </summary>
    /// <param name="title"></param>
    /// <param name="description"></param>
    /// <param name="startAt"></param>
    /// <param name="endAt"></param>
    public Event(string title, string? description, DateTime startAt, DateTime endAt)
    {
        ValidatePeriod(startAt, endAt);
        Id = Guid.NewGuid();
        Title = title;
        Description = description;
        StartAt = startAt;
        EndAt = endAt;
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