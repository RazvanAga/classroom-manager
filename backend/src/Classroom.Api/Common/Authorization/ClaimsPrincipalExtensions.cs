using System.Security.Claims;
using Classroom.Api.Identity;

namespace Classroom.Api.Common.Authorization;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// The signed-in teacher's id, read from the <see cref="ClaimTypes.NameIdentifier"/> claim that
    /// ASP.NET Identity stamps onto the auth cookie. Throws if absent — callers are authenticated.
    /// </summary>
    public static Guid GetTeacherId(this ClaimsPrincipal principal) =>
        principal.TryGetTeacherId(out var id)
            ? id
            : throw new InvalidOperationException("The current principal has no teacher id claim.");

    /// <summary>
    /// Non-throwing variant: <c>true</c> with the teacher id when present. A <em>kiosk</em> principal
    /// has no teacher id claim, so this returns <c>false</c> — letting authorization handlers and
    /// teacher-only endpoints <b>fail closed</b> (a clean 403) instead of throwing a 500 (design.md §5.4).
    /// </summary>
    public static bool TryGetTeacherId(this ClaimsPrincipal principal, out Guid teacherId)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out teacherId);
    }

    /// <summary>Whether the principal is a reduced-scope kiosk session (design.md §5.4).</summary>
    public static bool IsKiosk(this ClaimsPrincipal principal) =>
        principal.HasClaim(KioskAuth.ModeClaim, KioskAuth.ModeValue);

    /// <summary>The class id a kiosk session is scoped to, or <c>null</c> if not a kiosk principal.</summary>
    public static Guid? GetKioskClassId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(KioskAuth.ClassIdClaim), out var id) ? id : null;

    /// <summary>The id of the teacher who launched the kiosk session, used to verify the exit PIN.</summary>
    public static Guid? GetKioskTeacherId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(KioskAuth.TeacherIdClaim), out var id) ? id : null;
}
