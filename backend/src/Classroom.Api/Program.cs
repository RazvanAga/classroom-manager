using Classroom.Api.Features.Auth;
using Classroom.Api.Identity;
using Classroom.Infrastructure;
using Classroom.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Structured JSON logging to stdout (design.md §8 "Delivery & ops").
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();

// RFC 9457 ProblemDetails for every error response, including validation failures.
builder.Services.AddProblemDetails();

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

// Local-dev convenience: apply migrations and seed the teacher on boot. In production,
// migrations run as an explicit bundle deploy step (design.md §8.3, slice #17).
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ClassroomDbContext>();
    await db.Database.MigrateAsync();
    await SeedData.SeedTeacherAsync(scope.ServiceProvider, app.Configuration);
}

app.Run();

// Exposed so the integration-test harness can spin up the real app via WebApplicationFactory.
public partial class Program;
