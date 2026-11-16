using System.Security.Claims;
using UsersService.Application.Services.Abstractions;

namespace UsersService.Api.Common;

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal Principal => accessor.HttpContext?.User
                                         ?? throw new InvalidOperationException("Нет HTTP-контекста");

    public Guid Id => Guid.Parse(Principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public bool IsAdmin => Principal.IsInRole("Admin");
}
