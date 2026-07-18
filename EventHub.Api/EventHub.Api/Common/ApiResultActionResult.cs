using EventHub.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Common;

public class ApiResultActionResult : IActionResult
{
    private readonly ApiBaseResult _result;

    public ApiResultActionResult(ApiBaseResult result) => _result = result;

    public async Task ExecuteResultAsync(ActionContext context)
    {
        var objectResult = new ObjectResult(_result)
        {
            StatusCode = (int)_result.StatusCode
        };

        await objectResult.ExecuteResultAsync(context);
    }
}