using EventHub.Api.Models;

namespace EventHub.Api.Extensions;

public static class EventQueryableExtensions
{
    public static IQueryable<Event> TitleFilter(this IQueryable<Event> queryable, string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return queryable;
        
        var titleSmall = title.ToLower();

        return queryable.Where(e => e.Title.ToLower().Contains(titleSmall));
    }

    public static IQueryable<Event> FromDateFilter(this IQueryable<Event> queryable, DateTime? date)
    {
        if (date is null)
            return queryable;

        return queryable.Where(e => e.StartAt >= date);
    }
    
    public static IQueryable<Event> ToDateFilter(this IQueryable<Event> queryable, DateTime? date)
    {
        if (date is null)
            return queryable;

        return queryable.Where(e => e.EndAt <= date);
    }
}