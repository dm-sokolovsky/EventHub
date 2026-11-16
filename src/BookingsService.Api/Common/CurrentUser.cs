using System.Security.Claims;
using BookingsService.Application.Services.Abstractions;

namespace BookingsService.Api.Common;

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal Principal => accessor.HttpContext?.User
                                         ?? throw new InvalidOperationException("Нет HTTP-контекста");

    // UsersService выписывает userId в claim "sub" (JwtRegisteredClaimNames.Sub)
    public Guid Id => Guid.Parse(
        Principal.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? Principal.FindFirstValue("sub")
        ?? throw new InvalidOperationException("В токене нет идентификатора пользователя"));

    public bool IsAdmin => Principal.IsInRole("Admin");
}
