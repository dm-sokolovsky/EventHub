namespace EventHub.Api;

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
/// DTO для создания события
/// </summary>
/// <param name="Title">Название события</param>
/// <param name="Description">Описание события (опционально)</param>
/// <param name="StartAt">Дата и время начала</param>
/// <param name="EndAt">Дата и время окончания</param>
public record EventCreatedDto
(
    string Title,
    string? Description,
    DateTime StartAt,
    DateTime EndAt
);