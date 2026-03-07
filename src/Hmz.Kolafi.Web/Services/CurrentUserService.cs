using System.Security.Claims;
using Hmz.Kolafi.Core.Interfaces;

namespace Hmz.Kolafi.Web.Services;

/// <summary>
/// Resolves the current user's ID from the JWT claims in HttpContext.
/// Registered as Scoped in the Web DI container and passed into Infrastructure interceptors.
/// </summary>
public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
  public string? UserId =>
    httpContextAccessor.HttpContext?.User?.FindFirstValue("sub")
    ?? httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
}
