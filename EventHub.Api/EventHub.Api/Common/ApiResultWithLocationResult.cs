using EventHub.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace EventHub.Api.Common;

public class ApiResultWithLocationResult : IActionResult
{
    private readonly ApiBaseResult _result;
    private readonly string _actionName;
    private readonly object? _routeValues;

    public ApiResultWithLocationResult(ApiBaseResult result, string actionName, object? routeValues = null)
    {
        _result = result;
        _actionName = actionName;
        _routeValues = routeValues;
    }

    public async Task ExecuteResultAsync(ActionContext context)
    {
        var urlHelperFactory = context.HttpContext.RequestServices.GetRequiredService<IUrlHelperFactory>();
        var url = urlHelperFactory.GetUrlHelper(context);

        var location = url.Action(_actionName, _routeValues)
                       ?? throw new InvalidOperationException($"No route matches action '{_actionName}'.");

        context.HttpContext.Response.Headers.Location = location;

        var objectResult = new ObjectResult(_result)
        {
            StatusCode = (int)_result.StatusCode
        };

        await objectResult.ExecuteResultAsync(context);
    }
}