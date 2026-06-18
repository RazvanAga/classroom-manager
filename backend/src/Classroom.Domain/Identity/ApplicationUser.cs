using Microsoft.AspNetCore.Identity;

namespace Classroom.Domain.Identity;

/// <summary>
/// A teacher account. Students are class-owned entities, not users (see design.md §1.1),
/// so the only Identity principal in the system is the teacher.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>Name shown in the UI, e.g. "Ms. Rivera".</summary>
    public required string DisplayName { get; set; }

    /// <summary>
    /// Hashed numeric PIN required to exit kiosk mode (design.md §5.4). Hashed with the same
    /// <see cref="Microsoft.AspNetCore.Identity.IPasswordHasher{TUser}"/> as the password — never
    /// stored in plaintext, verified in constant time. Null until the teacher sets one.
    /// </summary>
    public string? KioskPinHash { get; set; }
}
