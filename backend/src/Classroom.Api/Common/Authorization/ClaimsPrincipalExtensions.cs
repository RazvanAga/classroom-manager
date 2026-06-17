using System.Security.Claims;

namespace Classroom.Api.Common.Authorization;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// The signed-in teacher's id, read from the <see cref="ClaimTypes.NameIdentifier"/> claim that
    /// ASP.NET Identity stamps onto the auth cookie. Throws if absent — callers are authenticated.
    /// </summary>
    public static Guid GetTeacherId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id)
            ? id
            : throw new InvalidOperationException("The current principal has no teacher id claim.");
    }
}
