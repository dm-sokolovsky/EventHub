using EventHab.Application.Contracts;
using EventHab.Application.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Endpoints;

internal static class UserEndpoints
{
    internal static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/user/auth/");
        
        group.MapPost("/register", async (RegisterRequest request, IUserService users, CancellationToken ct) =>
            {
                var id = await users.RegisterAsync(request, ct);
                return Results.Created($"/api/users/{id}", new { id });
            })
            .AllowAnonymous()
            .Produces(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        group.MapPost("/login", async (LoginRequest request, IUserService users, CancellationToken ct) =>
                Results.Ok(await users.LoginAsync(request, ct)))
            .AllowAnonymous()
            .Produces<AuthResult>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized);
        
        return app;
    }
}