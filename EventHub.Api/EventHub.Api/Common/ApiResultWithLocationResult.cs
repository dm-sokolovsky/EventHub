using EventHub.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace EventHub.Api.Common;

public class ApiResultWithLocationResult : IActionResult
{
    private readonly ApiBaseResult _result;
    private readonly string _actionName;
    private readonly string? _controllerName;
    private readonly object? _routeValues;

    public ApiResultWithLocationResult(ApiBaseResult result, string actionName, string? controllerName, object? routeValues = null)
    {
        _result = result;
        _actionName = actionName;
        _controllerName = controllerName;
        _routeValues = routeValues;
    }

    public async Task ExecuteResultAsync(ActionContext context)
    {
        var urlHelperFactory = context.HttpContext.RequestServices.GetRequiredService<IUrlHelperFactory>();
        var url = urlHelperFactory.GetUrlHelper(context);

        var location = _controllerName is null
            ? url.Action(_actionName, _routeValues)
            : url.Action(_actionName, _controllerName, _routeValues);
        
        if (location is null)
            throw new InvalidOperationException($"No route matches action '{_actionName}'.");

        context.HttpContext.Response.Headers.Location = location;

        var objectResult = new ObjectResult(_result)
        {
            StatusCode = (int)_result.StatusCode
        };

        await objectResult.ExecuteResultAsync(context);
    }
}