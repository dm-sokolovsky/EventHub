using System.ComponentModel.DataAnnotations;

namespace EventHub.Api.Contracts;

/// <summary>
/// DTO для отображения события
/// </summary>
/// <param name="Id">Id события</param>
/// <param name="Title">Название события</param>
/// <param name="Description">Описание события (опционально)</param>
/// <param name="StartAt">Дата и время начала</param>
/// <param name="EndAt">Дата и время окончания</param>
public record EventDto
(
    Guid Id,
    string Title,
    string? Description,
    DateTime StartAt,
    DateTime EndAt
);

/// <summary>
/// DTO для создания и обновления события
/// </summary>
/// <param name="Title">Название события</param>
/// <param name="Description">Описание события (опционально)</param>
/// <param name="StartAt">Дата и время начала</param>
/// <param name="EndAt">Дата и время окончания</param>
public record EventUpsertDto
(
    [Required(ErrorMessage = "Title is required")] string Title,
    string? Description,
    [Required(ErrorMessage = "Start At is required")] DateTime StartAt,
    [Required(ErrorMessage = "End At is required")] DateTime EndAt
) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndAt <= StartAt)
            yield return new ValidationResult(
                "EndAt должен быть позже StartAt",
                [nameof(EndAt), nameof(StartAt)]);
    }
}

/// <summary>
/// DTO для фильтрации всех событий
/// </summary>
/// <param name="Title">Поиск по названию (регистронезависимый, частичное совпадение) </param>
/// <param name="From">События, которые начинаются не раньше указанной даты</param>
/// <param name="To">События, которые заканчиваются не позже указанной даты</param>
public record EventFilterDto
(
    string? Title,
    DateTime? From,
    DateTime? To
);
