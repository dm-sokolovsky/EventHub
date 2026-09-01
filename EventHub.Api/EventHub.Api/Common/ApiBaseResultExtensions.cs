using EventHub.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Common;

public static class ApiBaseResultExtensions
{
    public static IActionResult ToActionResult(this ApiBaseResult result)
        => new ApiResultActionResult(result);
    
    public static IActionResult ToActionResultWithLocation(
        this ApiBaseResult result, string actionName, string? controllerName = null, object? routeValues = null)
        => new ApiResultWithLocationResult(result, actionName, controllerName, routeValues);
}