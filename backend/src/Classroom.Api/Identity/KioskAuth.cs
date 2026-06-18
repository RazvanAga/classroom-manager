namespace Classroom.Api.Identity;

/// <summary>
/// Names and claim types for the kiosk authentication scheme (design.md §5.4). Kiosk is a
/// <em>distinct, reduced-scope</em> principal carried by its own cookie under its own scheme —
/// not the teacher principal with the admin UI hidden.
/// </summary>
public static class KioskAuth
{
    /// <summary>The kiosk cookie authentication scheme.</summary>
    public const string Scheme = "Kiosk";

    /// <summary>The kiosk cookie name (distinct from the teacher's <c>classroom.auth</c>).</summary>
    public const string CookieName = "classroom.kiosk";

    /// <summary>
    /// The "smart" default scheme: a policy scheme that forwards to <see cref="Scheme"/> when the
    /// kiosk cookie is present, otherwise to the teacher application scheme. This makes
    /// <c>HttpContext.User</c> the kiosk principal during a kiosk session.
    /// </summary>
    public const string PolicyScheme = "ClassroomDefault";

    /// <summary>Claim marking the principal as a kiosk session; value is <see cref="ModeValue"/>.</summary>
    public const string ModeClaim = "mode";

    /// <summary>The value of the <see cref="ModeClaim"/> for a kiosk principal.</summary>
    public const string ModeValue = "kiosk";

    /// <summary>Claim holding the class id the kiosk session is scoped to.</summary>
    public const string ClassIdClaim = "kiosk_class_id";

    /// <summary>
    /// Claim holding the id of the teacher who launched the kiosk session, used to verify the exit
    /// PIN and to restore the full teacher session on a correct PIN.
    /// </summary>
    public const string TeacherIdClaim = "kiosk_teacher_id";

    /// <summary>How long a kiosk session lives before it must be re-entered (short-lived; §5.4).</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(8);
}
