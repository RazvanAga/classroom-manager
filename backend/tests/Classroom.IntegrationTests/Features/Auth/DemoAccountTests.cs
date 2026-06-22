using System.Net;
using System.Net.Http.Json;
using Classroom.Api.Identity;
using Classroom.Domain.Classes;
using Classroom.Domain.Identity;
using Classroom.Domain.Points;
using Classroom.Infrastructure.Persistence;
using Classroom.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Classroom.IntegrationTests.Features.Auth;

/// <summary>
/// The demo slice (#16): the seeder builds a rich, known state and the one-click login drops a reviewer
/// straight into a fully-interactive session. The background re-seed job is disabled in the harness
/// (<see cref="ClassroomApiFactory"/>); these tests drive <see cref="DemoSeeder"/> explicitly.
/// </summary>
public class DemoAccountTests(ClassroomApiFactory factory) : IntegrationTestBase(factory)
{
    private record MeDto(Guid Id, string Email, string DisplayName);
    private record ClassDto(Guid Id, string Name, string CurrencyIcon, DateTime CreatedAt, bool IsArchived, string Role);
    private record StudentDto(Guid Id, Guid ClassId, string DisplayName, string? Gender, DateTime CreatedAt);
    private record LeaderboardEntryDto(Guid StudentId, string DisplayName, int Wallet, int LifetimeEarned);

    private async Task ReseedDemoAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DemoSeeder>();
        await seeder.ReseedAsync();
    }

    private async Task<Guid> DemoTeacherIdAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClassroomDbContext>();
        var teacher = await db.Users.SingleAsync(u => u.Email == ClassroomApiFactory.DemoEmail);
        return teacher.Id;
    }

    [Fact]
    public async Task Reseed_builds_a_rich_populated_demo_class()
    {
        await ReseedDemoAsync();

        var teacherId = await DemoTeacherIdAsync();
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClassroomDbContext>();

        var demoClass = await db.Classes
            .Where(c => c.Teachers.Any(ct => ct.TeacherId == teacherId && ct.Role == ClassRole.Owner))
            .SingleAsync();
        var classId = demoClass.Id;

        // The redesign showcase (slice #19): "Clasa Steluțelor" with the star currency icon.
        Assert.Equal("Clasa Steluțelor", demoClass.Name);
        Assert.Equal(CurrencyIcon.Star, demoClass.CurrencyIcon);

        var studentIds = await db.Students.Where(s => s.ClassId == classId).Select(s => s.Id).ToListAsync();
        Assert.Equal(24, studentIds.Count);

        // Behavior catalog seeded; a varied, multi-row history; at least one student spent on avatars.
        Assert.Equal(6, await db.Behaviors.CountAsync(b => b.ClassId == classId));
        Assert.True(await db.PointTransactions.CountAsync(t => studentIds.Contains(t.StudentId)) > 100);
        Assert.True(await db.PointTransactions.AnyAsync(t =>
            studentIds.Contains(t.StudentId) && t.Type == PointTransactionType.Purchase));

        // Owned items exceed the 5 free defaults per student (24 × 5 = 120), proving purchases were
        // granted, and a saved grouping exists so the group-maker has something to show on arrival.
        Assert.True(await db.StudentOwnedItems.CountAsync(o => studentIds.Contains(o.StudentId)) > 120);
        Assert.True(await db.Groupings.AnyAsync(g => g.ClassId == classId));
    }

    [Fact]
    public async Task Reseed_is_idempotent_and_does_not_accumulate()
    {
        await ReseedDemoAsync();
        await ReseedDemoAsync();

        var teacherId = await DemoTeacherIdAsync();
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClassroomDbContext>();

        // Exactly one demo class survives — the second reset wiped the first, not stacked on it.
        Assert.Equal(1, await db.ClassTeachers.CountAsync(ct => ct.TeacherId == teacherId));
    }

    [Fact]
    public async Task Reseed_restores_the_configured_kiosk_pin_even_after_it_drifts()
    {
        // First reseed creates the demo teacher with the configured PIN (1234 in the harness).
        await ReseedDemoAsync();

        // Simulate drift: a demo visitor changed the PIN via Settings (or the account was first created
        // under a different config). The class wipe never touches the teacher's PIN.
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClassroomDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<ApplicationUser>>();
            var teacher = await db.Users.SingleAsync(u => u.Email == ClassroomApiFactory.DemoEmail);
            teacher.KioskPinHash = hasher.HashPassword(teacher, "999999");
            await db.SaveChangesAsync();
        }

        // The reseed must reconcile the PIN back to the configured value.
        await ReseedDemoAsync();

        var client = CreateClient();
        await SendWithTokenAsync(client, HttpMethod.Post, "/api/auth/demo-login");
        var classes = await client.GetFromJsonAsync<List<ClassDto>>("/api/classes");
        var demoClass = Assert.Single(classes!);

        var enter = await SendWithTokenAsync(client, HttpMethod.Post, "/api/kiosk/enter",
            new { classId = demoClass.Id });
        enter.EnsureSuccessStatusCode();

        // The drifted "999999" no longer works; the configured "1234" exits cleanly.
        var wrong = await SendWithTokenAsync(client, HttpMethod.Post, "/api/kiosk/exit", new { pin = "999999" });
        Assert.Equal(HttpStatusCode.Forbidden, wrong.StatusCode);

        var right = await SendWithTokenAsync(client, HttpMethod.Post, "/api/kiosk/exit", new { pin = "1234" });
        Assert.Equal(HttpStatusCode.NoContent, right.StatusCode);
    }

    [Fact]
    public async Task Demo_login_drops_the_reviewer_into_an_interactive_session()
    {
        await ReseedDemoAsync();

        var client = CreateClient();
        var login = await SendWithTokenAsync(client, HttpMethod.Post, "/api/auth/demo-login");
        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);

        // The session is the demo teacher.
        var me = await client.GetFromJsonAsync<MeDto>("/api/auth/me");
        Assert.Equal(ClassroomApiFactory.DemoEmail, me!.Email);

        // …and it can read the populated class and act in it (fully interactive — design.md §9.2).
        var classes = await client.GetFromJsonAsync<List<ClassDto>>("/api/classes");
        var demoClass = Assert.Single(classes!);
        Assert.Equal("Clasa Steluțelor", demoClass.Name);
        Assert.Equal("Star", demoClass.CurrencyIcon);

        var students = await client.GetFromJsonAsync<List<StudentDto>>(
            $"/api/classes/{demoClass.Id}/students");
        Assert.Equal(24, students!.Count);

        // The leaderboard is varied (some students lead), confirming the rich history landed.
        var leaderboard = await client.GetFromJsonAsync<List<LeaderboardEntryDto>>(
            $"/api/classes/{demoClass.Id}/leaderboard");
        Assert.Equal(24, leaderboard!.Count);
        Assert.True(leaderboard.Max(e => e.LifetimeEarned) > leaderboard.Min(e => e.LifetimeEarned));
    }

    [Fact]
    public async Task Demo_login_requires_an_antiforgery_token()
    {
        var client = CreateClient();
        // No token attached: the double-submit antiforgery filter must reject the mutating call.
        using var response = await client.PostAsync("/api/auth/demo-login", content: null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
