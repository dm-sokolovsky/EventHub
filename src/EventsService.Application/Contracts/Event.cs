using System.ComponentModel.DataAnnotations;

namespace EventsService.Application.Contracts;

/// <summary>
/// DTO для отображения события
/// </summary>
public sealed record EventInfo
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required DateTime StartAt { get; init; }
    public required DateTime EndAt { get; init; }
    public required int TotalSeats { get; init; }
    public required int AvailableSeats { get; init; }
}

/// <summary>
/// DTO для обновления события
/// </summary>
public sealed record EventUpsert
(
    [Required(ErrorMessage = "Title is required")] string Title,
    string? Description,
    [Required(ErrorMessage = "Start At is required")] DateTime? StartAt,
    [Required(ErrorMessage = "End At is required")] DateTime? EndAt,
    [Required(ErrorMessage = "TotalSeats is required")] int TotalSeats
) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndAt <= StartAt)
            yield return new ValidationResult(
                "EndAt должен быть позже StartAt",
                [nameof(EndAt), nameof(StartAt)]);

        if (TotalSeats <= 0)
            yield return new ValidationResult(
                "TotalSeats должна быть больше 0",
                [nameof(TotalSeats)]
            );
    }
}

/// <summary>
/// DTO для создания события
/// </summary>
public sealed record CreateEvent
{
    public string? Title { get; init; }
    public DateTime? StartAt { get; init; }
    public DateTime? EndAt { get; init; }
    public int? TotalSeats { get; init; }
    public string? Description { get; init; }
}

/// <summary>
/// DTO для фильтрации всех событий
/// </summary>
public sealed record EventFilter
(
    string? Title,
    DateTime? From,
    DateTime? To
);
