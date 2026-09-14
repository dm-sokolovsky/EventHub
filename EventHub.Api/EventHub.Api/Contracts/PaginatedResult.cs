namespace EventHub.Api.Contracts;

/// <summary>
/// DTO для вывода результатов пагинации событий
/// </summary>
/// <param name="TotalCount">Общее кол-во событий, подходящих под фильтр (без учёта пагинации)</param>
/// <param name="Events">Массив событий текущей страницы</param>
/// <param name="PageNumber">Номер текущей страницы</param>
/// <param name="PageSize">Кол-во элементов на текущей странице</param>
public sealed record PaginatedResult<T>
{
    public required T[] Items { get; init; }
    public required int TotalCount { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
}