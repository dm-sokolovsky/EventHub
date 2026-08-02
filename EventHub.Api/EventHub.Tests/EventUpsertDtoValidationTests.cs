using System.ComponentModel.DataAnnotations;
using System.Reflection;
using EventHub.Api.Contracts;

namespace EventHub.Tests;

public class EventUpsertDtoValidationTests
{
    // ASP.NET Core validates record primary-constructor parameters directly (not the
    // generated properties) when binding a request body, so [Required] must live on the
    // constructor parameter, not the property (see EventHub.Api/Contracts/Event/EventDto.cs).
    // Validator.TryValidateObject only inspects properties and would misreport this as valid,
    // so it can't be used here — reflect on the constructor parameter instead.
    [Theory]
    [InlineData("Title")]
    [InlineData("StartAt")]
    [InlineData("EndAt")]
    public void EventUpsertDto_RequiredFields_HaveRequiredAttributeOnConstructorParameter(string parameterName)
    {
        var ctor = typeof(EventUpsertDto).GetConstructors().Single();
        var parameter = ctor.GetParameters().Single(p => p.Name == parameterName);

        Assert.True(parameter.IsDefined(typeof(RequiredAttribute)));
    }

    [Fact]
    public void EventUpsertDto_EndAtBeforeStartAt_FailsValidation()
    {
        var startAt = DateTime.UtcNow;
        var endAt = startAt.AddHours(-1);
        var dto = new EventUpsertDto("title", "desc", startAt, endAt);

        var results = dto.Validate(new ValidationContext(dto)).ToList();

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(EventUpsertDto.EndAt)));
    }

    [Fact]
    public void EventUpsertDto_ValidDates_PassesValidation()
    {
        var startAt = DateTime.UtcNow;
        var endAt = startAt.AddHours(1);
        var dto = new EventUpsertDto("title", "desc", startAt, endAt);

        var results = dto.Validate(new ValidationContext(dto));

        Assert.Empty(results);
    }
}
