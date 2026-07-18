using EventHub.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Common;

public static class ApiBaseResultExtensions
{
    public static IActionResult ToActionResult(this ApiBaseResult result)
        => new ApiResultActionResult(result);
}