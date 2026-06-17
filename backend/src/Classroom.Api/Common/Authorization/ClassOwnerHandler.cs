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
        var teacherId = context.User.GetTeacherId();

        var isOwner = await db.Classes
            .AnyAsync(c => c.Id == classId
                && c.Teachers.Any(ct => ct.TeacherId == teacherId && ct.Role == ClassRole.Owner));

        if (isOwner)
        {
            context.Succeed(requirement);
        }
    }
}
