using System.Security.Claims;
using Classroom.Api.Common.Authorization;
using Classroom.Api.Common.Security;
using Classroom.Api.Common.Validation;
using Classroom.Api.Identity;
using Classroom.Domain.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Classroom.Infrastructure.Persistence;

namespace Classroom.Api.Features.Kiosk;

public static class KioskEndpoints
{
    public static IEndpointRouteBuilder MapKioskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/kiosk").WithTags("Kiosk").RequireAuthorization();

        group.MapPost("/enter", EnterAsync)
            .AddEndpointFilter<ValidationFilter<EnterKioskRequest>>()
            .AddEndpointFilter<AntiforgeryFilter>()
            .WithSummary("Mint a reduced-scope kiosk session for a class (teacher must be a member).");

        group.MapPost("/exit", ExitAsync)
            .AddEndpointFilter<ValidationFilter<KioskPinRequest>>()
            .AddEndpointFilter<AntiforgeryFilter>()
            .WithSummary("Exit kiosk mode with the correct PIN; restores the full teacher session.");

        group.MapPost("/pin", SetPinAsync)
            .AddEndpointFilter<ValidationFilter<KioskPinRequest>>()
            .AddEndpointFilter<AntiforgeryFilter>()
            .WithSummary("Set the current teacher's kiosk exit PIN.");

        group.MapGet("/me", MeAsync)
            .WithSummary("Describe the current kiosk session (kiosk principal only).");

        return app;
    }

    private static async Task<IResult> EnterAsync(
        EnterKioskRequest request,
        ClaimsPrincipal user,
        HttpContext http,
        ClassroomDbContext db,
        IAuthorizationService authz)
    {
        // Only a teacher who is a member of the class may launch kiosk for it. The membership
        // requirement fails closed for a non-teacher principal (design.md §5.4), and we need the
        // teacher id to stash on the kiosk session for the PIN-gated exit.
        if (!user.TryGetTeacherId(out var teacherId)
            || !(await authz.AuthorizeAsync(user, request.ClassId, new ClassMembershipRequirement())).Succeeded)
        {
            return Forbidden();
        }

        var klass = await db.Classes
            .Where(c => c.Id == request.ClassId)
            .Select(c => new { c.Id, c.Name, c.CurrencyIcon })
            .FirstOrDefaultAsync();
        if (klass is null)
        {
            return Forbidden();
        }

        // The reduced-scope kiosk principal. The NameIdentifier is a non-GUID session sentinel: it
        // gives antiforgery a stable per-session id for kiosk mutations, yet can never be parsed as a
        // teacher id (TryGetTeacherId uses Guid.TryParse) — so authorization still fails closed.
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, $"kiosk:{Guid.NewGuid()}"),
                new Claim(KioskAuth.ModeClaim, KioskAuth.ModeValue),
                new Claim(KioskAuth.ClassIdClaim, klass.Id.ToString()),
                new Claim(KioskAuth.TeacherIdClaim, teacherId.ToString()),
            ],
            authenticationType: KioskAuth.Scheme);

        // Sign the teacher OUT and the kiosk principal IN: while kiosk is active there is no teacher
        // cookie for a child to fall back to (design.md §5.4). The correct PIN restores it on exit.
        await http.SignOutAsync(IdentityConstants.ApplicationScheme);
        await http.SignInAsync(
            KioskAuth.Scheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });

        return Results.Ok(new KioskSessionResponse(klass.Id, klass.Name, klass.CurrencyIcon));
    }

    private static async Task<IResult> ExitAsync(
        KioskPinRequest request,
        ClaimsPrincipal user,
        HttpContext http,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IPasswordHasher<ApplicationUser> pinHasher)
    {
        // Exit is a kiosk-only operation: a teacher principal has nothing to exit.
        if (!user.IsKiosk() || user.GetKioskTeacherId() is not { } teacherId)
        {
            return Forbidden();
        }

        var teacher = await userManager.FindByIdAsync(teacherId.ToString());
        if (teacher?.KioskPinHash is null)
        {
            return InvalidPin();
        }

        // Constant-time verification via the same hasher used for passwords.
        var result = pinHasher.VerifyHashedPassword(teacher, teacher.KioskPinHash, request.Pin);
        if (result == PasswordVerificationResult.Failed)
        {
            return InvalidPin();
        }

        // Correct PIN: restore the full teacher session and tear down the kiosk one.
        await signInManager.SignInAsync(teacher, isPersistent: true);
        await http.SignOutAsync(KioskAuth.Scheme);
        return Results.NoContent();
    }

    private static async Task<IResult> SetPinAsync(
        KioskPinRequest request,
        ClaimsPrincipal user,
        UserManager<ApplicationUser> userManager,
        IPasswordHasher<ApplicationUser> pinHasher)
    {
        // Setting the PIN is a teacher operation; a kiosk principal has no teacher id and is denied.
        if (!user.TryGetTeacherId(out var teacherId))
        {
            return Forbidden();
        }

        var teacher = await userManager.FindByIdAsync(teacherId.ToString());
        if (teacher is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status401Unauthorized);
        }

        teacher.KioskPinHash = pinHasher.HashPassword(teacher, request.Pin);
        await userManager.UpdateAsync(teacher);
        return Results.NoContent();
    }

    private static async Task<IResult> MeAsync(
        ClaimsPrincipal user,
        ClassroomDbContext db)
    {
        if (!user.IsKiosk() || user.GetKioskClassId() is not { } classId)
        {
            return Forbidden();
        }

        var klass = await db.Classes
            .Where(c => c.Id == classId)
            .Select(c => new KioskSessionResponse(c.Id, c.Name, c.CurrencyIcon))
            .FirstOrDefaultAsync();

        return klass is null ? Forbidden() : Results.Ok(klass);
    }

    private static IResult Forbidden() => Results.Problem(
        title: "Forbidden",
        detail: "This action is not available in the current session.",
        statusCode: StatusCodes.Status403Forbidden);

    private static IResult InvalidPin() => Results.Problem(
        title: "Invalid PIN",
        detail: "That PIN is incorrect.",
        statusCode: StatusCodes.Status403Forbidden);
}
