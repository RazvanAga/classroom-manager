using Classroom.Domain.Classes;
using Classroom.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Classroom.Api.Common.Authorization;

/// <summary>
/// Resource-based handler (design.md §5.3): succeeds only when the current teacher is an Owner of the
/// target class. Gates destructive ops — delete class, add/remove teacher.
/// </summary>
public class ClassOwnerHandler(ClassroomDbContext db)
    : AuthorizationHandler<ClassOwnerRequirement, Guid>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ClassOwnerRequirement requirement,
        Guid classId)
    {
        // Fail closed for a non-teacher principal (e.g. a kiosk session): deny rather than throw, so
        // destructive teacher-only endpoints return a clean 403 (design.md §5.4).
        if (!context.User.TryGetTeacherId(out var teacherId))
        {
            return;
        }

        var isOwner = await db.Classes
            .AnyAsync(c => c.Id == classId
                && c.Teachers.Any(ct => ct.TeacherId == teacherId && ct.Role == ClassRole.Owner));

        if (isOwner)
        {
            context.Succeed(requirement);
        }
    }
}
