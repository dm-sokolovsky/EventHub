using System.ComponentModel.DataAnnotations;
using EventHub.Api.Contracts;

namespace EventHub.Tests;

public class EventUpsertDtoValidationTests
{
    [Fact]
    public void EventUpsertDto_EmptyTitle_FailsValidation()
    {
        var dto = new EventUpsertDto(string.Empty, "desc", DateTime.UtcNow, DateTime.UtcNow.AddHours(1));

        var isValid = TryValidate(dto, out var results);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(EventUpsertDto.Title)));
    }

    [Fact]
    public void EventUpsertDto_EndAtBeforeStartAt_FailsValidation()
    {
        var startAt = DateTime.UtcNow;
        var endAt = startAt.AddHours(-1);
        var dto = new EventUpsertDto("title", "desc", startAt, endAt);

        var isValid = TryValidate(dto, out var results);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(EventUpsertDto.EndAt)));
    }

    [Fact]
    public void EventUpsertDto_ValidData_PassesValidation()
    {
        var startAt = DateTime.UtcNow;
        var endAt = startAt.AddHours(1);
        var dto = new EventUpsertDto("title", "desc", startAt, endAt);

        var isValid = TryValidate(dto, out var results);

        Assert.True(isValid);
        Assert.Empty(results);
    }

    private static bool TryValidate(EventUpsertDto dto, out List<ValidationResult> results)
    {
        results = [];
        var context = new ValidationContext(dto);
        return Validator.TryValidateObject(dto, context, results, validateAllProperties: true);
    }
}
