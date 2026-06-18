using Classroom.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Classroom.Api.Common.Authorization;

/// <summary>
/// Resource-based handler for the kiosk-reachable endpoints (design.md §5.4). Succeeds when the
/// principal is <b>either</b> a kiosk session whose <c>classId</c> claim matches the route class
/// <b>or</b> a teacher who is a member of that class. A kiosk session addressing a <em>different</em>
/// class is denied (the claim must match the route), and a teacher path reuses the same membership
/// query as <see cref="ClassMembershipHandler"/>.
/// </summary>
public class KioskOrMemberHandler(ClassroomDbContext db)
    : AuthorizationHandler<KioskOrMemberRequirement, Guid>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        KioskOrMemberRequirement requirement,
        Guid classId)
    {
        if (context.User.IsKiosk())
        {
            // A kiosk principal is allowed only against the exact class it was scoped to.
            if (context.User.GetKioskClassId() == classId)
            {
                context.Succeed(requirement);
            }

            return;
        }

        if (!context.User.TryGetTeacherId(out var teacherId))
        {
            return;
        }

        var isMember = await db.Classes
            .AnyAsync(c => c.Id == classId && c.Teachers.Any(ct => ct.TeacherId == teacherId));

        if (isMember)
        {
            context.Succeed(requirement);
        }
    }
}
