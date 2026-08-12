using EventHub.Api.Models;

namespace EventHub.Api.Extensions.Event;

public static class EventQueryableExtensions
{
    public static IQueryable<Models.Event.Event> TitleFilter(this IQueryable<Models.Event.Event> queryable, string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return queryable;

        var titleSmall = title.ToLower();

        return queryable.Where(e => e.Title.ToLower().Contains(titleSmall));
    }

    public static IQueryable<Models.Event.Event> FromDateFilter(this IQueryable<Models.Event.Event> queryable, DateTime? date)
    {
        if (date is null)
            return queryable;

        return queryable.Where(e => e.StartAt >= date);
    }

    public static IQueryable<Models.Event.Event> ToDateFilter(this IQueryable<Models.Event.Event> queryable, DateTime? date)
    {
        if (date is null)
            return queryable;

        return queryable.Where(e => e.EndAt <= date);
    }

    public static IQueryable<Models.Event.Event> Page(this IQueryable<Models.Event.Event> queryable, int page, int pageSize) => queryable
            .Skip((page - 1) * pageSize)
            .Take(pageSize);
}