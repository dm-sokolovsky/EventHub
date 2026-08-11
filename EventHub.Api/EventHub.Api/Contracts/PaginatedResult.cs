namespace EventHub.Api.Contracts;

/// <summary>
/// DTO для вывода результатов пагинации событий
/// </summary>
/// <param name="TotalCount">Общее кол-во событий, подходящих под фильтр (без учёта пагинации)</param>
/// <param name="Events">Массив событий текущей страницы</param>
/// <param name="PageNumber">Номер текущей страницы</param>
/// <param name="PageSize">Кол-во элементов на текущей странице</param>
public record PaginatedResult(
    int TotalCount,
    List<EventDto> Events,
    int PageNumber,
    int PageSize
    );
