using System.Security.Claims;
using Classroom.Api.Common.Authorization;
using Classroom.Api.Common.Security;
using Classroom.Api.Common.Validation;
using Classroom.Api.Identity;
using Classroom.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Classroom.Api.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .AddEndpointFilter<ValidationFilter<LoginRequest>>()
            .AddEndpointFilter<AntiforgeryFilter>()
            .WithSummary("Sign in a seeded teacher and issue the auth cookie.");

        group.MapPost("/demo-login", DemoLoginAsync)
            .AllowAnonymous()
            .AddEndpointFilter<AntiforgeryFilter>()
            .WithSummary("One-click sign-in to the shared, writable demo account.");

        group.MapPost("/logout", LogoutAsync)
            .RequireAuthorization()
            .AddEndpointFilter<AntiforgeryFilter>()
            .WithSummary("Sign out the current teacher.");

        group.MapGet("/me", Me)
            .RequireAuthorization()
            .WithSummary("Return the currently signed-in teacher.");

        return app;
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return InvalidCredentials();
        }

        var result = await signInManager.PasswordSignInAsync(
            user, request.Password, isPersistent: true, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            return Results.Problem(
                title: "Account locked",
                detail: "Too many failed attempts. Try again later.",
                statusCode: StatusCodes.Status423Locked);
        }

        if (!result.Succeeded)
        {
            return InvalidCredentials();
        }

        return Results.NoContent();
    }

    private static async Task<IResult> DemoLoginAsync(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IOptions<DemoAccountOptions> demoOptions)
    {
        // No credentials are taken from the caller: the demo identity is server-side config. Gating on
        // the account's existence (not the master switch) means once seeded it's reachable; when the
        // demo was never seeded — disabled feature, or before the boot-time seed lands — it 404s.
        var demo = await userManager.FindByEmailAsync(demoOptions.Value.Email);
        if (demo is null)
        {
            return Results.Problem(
                title: "Demo unavailable",
                detail: "The demo account is not available.",
                statusCode: StatusCodes.Status404NotFound);
        }

        await signInManager.SignInAsync(demo, isPersistent: true);
        return Results.NoContent();
    }

    private static async Task<IResult> LogoutAsync(SignInManager<ApplicationUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> Me(
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager)
    {
        // A kiosk principal is not a teacher (its id claim is a non-GUID session sentinel); treat
        // "who is the teacher?" as unauthenticated so the SPA shows the kiosk view, not a 500.
        if (principal.IsKiosk())
        {
            return Results.Problem(statusCode: StatusCodes.Status401Unauthorized);
        }

        var user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            // Cookie was valid but the user no longer exists.
            return Results.Problem(statusCode: StatusCodes.Status401Unauthorized);
        }

        return Results.Ok(new MeResponse(user.Id, user.Email!, user.DisplayName));
    }

    private static IResult InvalidCredentials() => Results.Problem(
        title: "Invalid credentials",
        detail: "The email or password is incorrect.",
        statusCode: StatusCodes.Status401Unauthorized);
}
