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
}
