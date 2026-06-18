using System.Security.Claims;
using Classroom.Api.Common.Authorization;
using Classroom.Api.Common.Security;
using Classroom.Api.Common.Validation;
using Classroom.Domain.Avatars;
using Classroom.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Classroom.Api.Features.Avatars;

public static class AvatarsEndpoints
{
    public static IEndpointRouteBuilder MapAvatarEndpoints(this IEndpointRouteBuilder app)
    {
        // The catalog is global (design.md §3.3): one shop for everyone, readable by any teacher.
        app.MapGet("/api/avatar/catalog", CatalogAsync)
            .WithTags("Avatars")
            .RequireAuthorization()
            .WithSummary("List the global avatar catalog (all options, with the locked DiceBear style).");

        // Per-student avatar ops are class-scoped, matching the roster's nesting from slice #4.
        var group = app.MapGroup("/api/classes/{classId:guid}/students/{studentId:guid}/avatar")
            .WithTags("Avatars")
            .RequireAuthorization();

        group.MapGet("/", GetAvatarAsync)
            .WithSummary("Get a student's equipped avatar config and the options they own.");

        group.MapPut("/{slot}", EquipAsync)
            .AddEndpointFilter<ValidationFilter<EquipItemRequest>>()
            .AddEndpointFilter<AntiforgeryFilter>()
            .WithSummary("Equip an owned option in a slot (free). Switching is unrestricted.");

        return app;
    }

    private static async Task<IResult> CatalogAsync(ClassroomDbContext db)
    {
        var items = await db.AvatarItems
            .OrderBy(i => i.Slot)
            .ThenByDescending(i => i.IsDefault)
            .ThenBy(i => i.DisplayName)
            .Select(i => new AvatarItemResponse(
                i.Id, i.Slot, i.OptionValue, i.DisplayName, i.Cost, i.Rarity, i.IsDefault))
            .ToListAsync();

        return Results.Ok(new AvatarCatalogResponse(AvatarCatalog.Style, items));
    }

    private static async Task<IResult> GetAvatarAsync(
        Guid classId,
        Guid studentId,
        ClaimsPrincipal user,
        ClassroomDbContext db,
        IAuthorizationService authz)
    {
        // Avatar read is kiosk-reachable (design.md §5.4): the kiosk shows the student their avatar.
        if (!await IsKioskOrMember(authz, user, classId))
        {
            return Forbidden();
        }

        if (!await StudentInClass(db, classId, studentId))
        {
            return StudentNotFound();
        }

        // Equipped config (the render input): one row per slot, projected to its DiceBear value.
        // Order on the entity's slot column before projecting (ordering the projected record can't
        // be translated to SQL).
        var equipped = await db.StudentEquipped
            .Where(e => e.StudentId == studentId)
            .OrderBy(e => e.Slot)
            .Join(db.AvatarItems, e => e.ItemId, i => i.Id,
                (e, i) => new EquippedSlotResponse(e.Slot, i.Id, i.OptionValue))
            .ToListAsync();

        // Everything the student owns and may equip for free (drives the slot pickers).
        var owned = await db.StudentOwnedItems
            .Where(o => o.StudentId == studentId)
            .Join(db.AvatarItems, o => o.ItemId, i => i.Id, (o, i) => i)
            .OrderBy(i => i.Slot)
            .ThenBy(i => i.DisplayName)
            .Select(i => new AvatarItemResponse(
                i.Id, i.Slot, i.OptionValue, i.DisplayName, i.Cost, i.Rarity, i.IsDefault))
            .ToListAsync();

        return Results.Ok(new StudentAvatarResponse(studentId, AvatarCatalog.Style, equipped, owned));
    }

    private static async Task<IResult> EquipAsync(
        Guid classId,
        Guid studentId,
        string slot,
        EquipItemRequest request,
        ClaimsPrincipal user,
        ClassroomDbContext db,
        IAuthorizationService authz)
    {
        // Equip is kiosk-reachable (design.md §5.4): the student changes their own owned options.
        if (!await IsKioskOrMember(authz, user, classId))
        {
            return Forbidden();
        }

        if (!Enum.TryParse<AvatarSlot>(slot, ignoreCase: true, out var parsedSlot))
        {
            return Results.Problem(
                title: "Unknown slot",
                detail: $"'{slot}' is not a valid avatar slot.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (!await StudentInClass(db, classId, studentId))
        {
            return StudentNotFound();
        }

        // The student must own the target item, and it must belong to the slot being equipped. A
        // single check covers "unowned", "no such item", and "wrong slot" — all client errors (400).
        var owns = await db.StudentOwnedItems
            .Where(o => o.StudentId == studentId && o.ItemId == request.ItemId)
            .Join(db.AvatarItems, o => o.ItemId, i => i.Id, (o, i) => i.Slot)
            .AnyAsync(s => s == parsedSlot);
        if (!owns)
        {
            return Results.Problem(
                title: "Cannot equip that item",
                detail: "You can only equip an option the student owns for this slot.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        // Upsert the equipped row for this slot (composite PK (StudentId, Slot)).
        var current = await db.StudentEquipped
            .FirstOrDefaultAsync(e => e.StudentId == studentId && e.Slot == parsedSlot);
        if (current is null)
        {
            db.StudentEquipped.Add(new StudentEquipped
            {
                StudentId = studentId,
                Slot = parsedSlot,
                ItemId = request.ItemId,
            });
        }
        else
        {
            current.ItemId = request.ItemId;
        }

        await db.SaveChangesAsync();

        var optionValue = await db.AvatarItems
            .Where(i => i.Id == request.ItemId)
            .Select(i => i.OptionValue)
            .FirstAsync();
        return Results.Ok(new EquippedSlotResponse(parsedSlot, request.ItemId, optionValue));
    }

    private static async Task<bool> IsKioskOrMember(
        IAuthorizationService authz, ClaimsPrincipal user, Guid classId)
    {
        var result = await authz.AuthorizeAsync(user, classId, new KioskOrMemberRequirement());
        return result.Succeeded;
    }

    private static Task<bool> StudentInClass(ClassroomDbContext db, Guid classId, Guid studentId) =>
        db.Students.AnyAsync(s => s.Id == studentId && s.ClassId == classId);

    private static IResult Forbidden() => Results.Problem(
        title: "Forbidden",
        detail: "You do not have access to this class.",
        statusCode: StatusCodes.Status403Forbidden);

    private static IResult StudentNotFound() => Results.Problem(
        title: "Student not found",
        detail: "No such student exists in this class.",
        statusCode: StatusCodes.Status404NotFound);
}
