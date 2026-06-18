using Classroom.Domain.Common;
using Classroom.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace Classroom.Api.Identity;

public static class SeedData
{
    /// <summary>
    /// Ensures the single seeded teacher exists (design.md §5.2 — no public registration).
    /// Idempotent: safe to run on every startup and from the integration-test harness.
    /// </summary>
    public static async Task SeedTeacherAsync(IServiceProvider services, IConfiguration configuration)
    {
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();

        var email = configuration["SeedTeacher:Email"] ?? "teacher@classroom.local";
        var displayName = configuration["SeedTeacher:DisplayName"] ?? "Demo Teacher";
        var password = configuration["SeedTeacher:Password"] ?? "Passw0rd!";
        var kioskPin = configuration["SeedTeacher:KioskPin"] ?? "1234";

        if (await users.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var teacher = new ApplicationUser
        {
            Id = EntityId.New(), // GUID v7 PKs (design.md §8.5)
            UserName = email,
            Email = email,
            EmailConfirmed = true, // no SMTP in v1; seeded accounts are pre-confirmed
            DisplayName = displayName,
        };

        // Seed a default kiosk exit PIN so the demo teacher can leave kiosk mode out of the box
        // (design.md §5.4). Hashed with the same hasher as passwords — never plaintext.
        var pinHasher = services.GetRequiredService<IPasswordHasher<ApplicationUser>>();
        teacher.KioskPinHash = pinHasher.HashPassword(teacher, kioskPin);

        var result = await users.CreateAsync(teacher, password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
            throw new InvalidOperationException($"Failed to seed teacher '{email}': {errors}");
        }
    }
}
