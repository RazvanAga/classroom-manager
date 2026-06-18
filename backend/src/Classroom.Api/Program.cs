using Classroom.Api.Common.Authorization;
using Classroom.Api.Features.Auth;
using Classroom.Api.Features.Avatars;
using Classroom.Api.Features.Behaviors;
using Classroom.Api.Features.Classes;
using Classroom.Api.Features.Kiosk;
using Classroom.Api.Features.Picker;
using Classroom.Api.Features.Points;
using Classroom.Api.Features.Roster;
using Classroom.Api.Features.Store;
using Classroom.Api.Identity;
using Classroom.Infrastructure;
using Classroom.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Structured JSON logging to stdout (design.md §8 "Delivery & ops").
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();

// RFC 9457 ProblemDetails for every error response, including validation failures.
builder.Services.AddProblemDetails();

// Serialize enums as strings on the wire (mirrors the "enums as strings" storage invariant and
// keeps the API self-describing, e.g. role "Owner" not 0).
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection.");
builder.Services.AddInfrastructure(connectionString);

builder.Services.AddClassroomIdentity();

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.Name = "classroom.xsrf";
});

builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

// Resource-based authorization handlers (design.md §5.3): membership for access, Owner for
// destructive ops. Scoped because they query the DbContext.
builder.Services.AddScoped<IAuthorizationHandler, ClassMembershipHandler>();
builder.Services.AddScoped<IAuthorizationHandler, ClassOwnerHandler>();
// Kiosk-reachable endpoints (roster read, shop, equip) accept a member-teacher OR a kiosk
// principal scoped to the same class (design.md §5.4).
builder.Services.AddScoped<IAuthorizationHandler, KioskOrMemberHandler>();

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages(); // turns bare 401/403/404 etc. into ProblemDetails bodies

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapOpenApi();
app.MapScalarApiReference();

// Double-submit antiforgery token endpoint; the SPA fetches this before mutating.
app.MapGet("/api/antiforgery/token", (IAntiforgery antiforgery, HttpContext context) =>
{
    var tokens = antiforgery.GetAndStoreTokens(context);
    return Results.Ok(new { token = tokens.RequestToken });
}).WithTags("Auth").AllowAnonymous();

app.MapAuthEndpoints();
app.MapClassEndpoints();
app.MapRosterEndpoints();
app.MapBehaviorEndpoints();
app.MapPointEndpoints();
app.MapAvatarEndpoints();
app.MapStoreEndpoints();
app.MapKioskEndpoints();
app.MapPickerEndpoints();

// Local-dev convenience: apply migrations and seed the teacher on boot. In production,
// migrations run as an explicit bundle deploy step (design.md §8.3, slice #17).
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ClassroomDbContext>();
    await db.Database.MigrateAsync();
    await SeedData.SeedTeacherAsync(scope.ServiceProvider, app.Configuration);
    // The avatar catalog is global (design.md §3.3): seed it once at startup, idempotently —
    // not per class. New students are granted the default items on creation (see RosterEndpoints).
    await AvatarCatalogSeeder.SeedAsync(db);
}

app.Run();

// Exposed so the integration-test harness can spin up the real app via WebApplicationFactory.
public partial class Program;
