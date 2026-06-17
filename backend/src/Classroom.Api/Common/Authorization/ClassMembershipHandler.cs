using Classroom.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Classroom.Api.Common.Authorization;

/// <summary>
/// Resource-based handler (design.md §5.3): succeeds only when the current teacher belongs to the
/// target class. The resource is the class id from the route. Queries through <c>Classes</c> so the
/// soft-delete global query filter applies — a deleted class denies access too.
/// </summary>
public class ClassMembershipHandler(ClassroomDbContext db)
    : AuthorizationHandler<ClassMembershipRequirement, Guid>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ClassMembershipRequirement requirement,
        Guid classId)
    {
        var teacherId = context.User.GetTeacherId();

        var isMember = await db.Classes
            .AnyAsync(c => c.Id == classId && c.Teachers.Any(ct => ct.TeacherId == teacherId));

        if (isMember)
        {
            context.Succeed(requirement);
        }
    }
}
