using Classroom.Domain.Identity;
using Classroom.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

namespace Classroom.Api.Identity;

public static class IdentitySetup
{
    /// <summary>
    /// Cookie-based ASP.NET Core Identity for teachers (design.md §5.1). No public registration,
    /// no SMTP — accounts are seeded. Cookie is HttpOnly + SameSite; auth failures return
    /// 401/403 (an API) rather than redirecting to a login page.
    /// </summary>
    public static IServiceCollection AddClassroomIdentity(this IServiceCollection services)
    {
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                // Brute-force protection via Identity's built-in lockout (design.md §8 "Delivery & ops").
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ClassroomDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        // The default scheme is a "smart" policy scheme (KioskAuth.PolicyScheme): when the kiosk
        // cookie is present it forwards to the kiosk scheme so HttpContext.User is the reduced-scope
        // kiosk principal; otherwise it forwards to the teacher application scheme (design.md §5.4).
        var authBuilder = services.AddAuthentication(options =>
        {
            options.DefaultScheme = KioskAuth.PolicyScheme;
        });
        authBuilder.AddIdentityCookies();

        // The kiosk session: its own short-lived cookie under its own scheme. Same API-style
        // 401/403 (no login redirect) and HttpOnly/SameSite hardening as the teacher cookie.
        authBuilder.AddCookie(KioskAuth.Scheme, options =>
        {
            options.Cookie.Name = KioskAuth.CookieName;
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.ExpireTimeSpan = KioskAuth.Lifetime;
            options.SlidingExpiration = false; // short-lived by design; re-enter to refresh
            options.Events.OnRedirectToLogin = ApiStatusCode(StatusCodes.Status401Unauthorized);
            options.Events.OnRedirectToAccessDenied = ApiStatusCode(StatusCodes.Status403Forbidden);
        });

        authBuilder.AddPolicyScheme(KioskAuth.PolicyScheme, displayName: "Classroom (teacher or kiosk)", options =>
        {
            options.ForwardDefaultSelector = context =>
                context.Request.Cookies.ContainsKey(KioskAuth.CookieName)
                    ? KioskAuth.Scheme
                    : IdentityConstants.ApplicationScheme;
        });

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "classroom.auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.ExpireTimeSpan = TimeSpan.FromDays(7);
            options.SlidingExpiration = true;

            // This is an API, not an MVC app: don't 302 to /Account/Login — surface the status.
            options.Events.OnRedirectToLogin = ApiStatusCode(StatusCodes.Status401Unauthorized);
            options.Events.OnRedirectToAccessDenied = ApiStatusCode(StatusCodes.Status403Forbidden);
        });

        services.AddAuthorization();

        return services;
    }

    /// <summary>
    /// Cookie redirect event that turns the would-be login/access-denied redirect into a bare
    /// status code — the app is an API, so it surfaces 401/403 rather than 302-ing to an MVC page.
    /// </summary>
    private static Func<RedirectContext<CookieAuthenticationOptions>, Task> ApiStatusCode(int statusCode) =>
        context =>
        {
            context.Response.StatusCode = statusCode;
            return Task.CompletedTask;
        };
}
