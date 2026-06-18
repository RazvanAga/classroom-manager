using System.Net;
using System.Net.Http.Json;
using Classroom.Domain.Groups;
using Classroom.Domain.Points;
using Classroom.Domain.Students;
using Classroom.Infrastructure.Persistence;
using Classroom.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Classroom.IntegrationTests.Features.Roster;

public class StudentPurgeTests(ClassroomApiFactory factory) : IntegrationTestBase(factory)
{
    private record ClassDto(Guid Id, string Name, DateTime CreatedAt, bool IsArchived, string Role);
    private record StudentDto(Guid Id, Guid ClassId, string DisplayName, string? Gender, DateTime CreatedAt);
    private record BehaviorDto(Guid Id, Guid ClassId, string Name, int DefaultPoints, DateTime CreatedAt);
    private record BalanceDto(Guid StudentId, int Wallet, int LifetimeEarned);

    private static string UniqueEmail() => $"teacher-{Guid.NewGuid():N}@classroom.local";

    private static async Task<Guid> CreateClassAsync(HttpClient client, string name)
    {
        var response = await SendWithTokenAsync(client, HttpMethod.Post, "/api/classes", new { name });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ClassDto>())!.Id;
    }

    private static async Task<Guid> AddStudentAsync(HttpClient client, Guid classId, string name, string? gender = null)
    {
        var response = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/students", new { displayName = name, gender });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StudentDto>())!.Id;
    }

    private static async Task<BehaviorDto> AddBehaviorAsync(HttpClient client, Guid classId, string name, int points)
    {
        var response = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/behaviors", new { name, defaultPoints = points });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BehaviorDto>())!;
    }

    private static Task AwardAsync(HttpClient client, Guid classId, Guid studentId, Guid behaviorId) =>
        SendWithTokenAsync(client, HttpMethod.Post, $"/api/classes/{classId}/points/award",
            new { studentIds = new[] { studentId }, behaviorId });

    private static Task AdjustAsync(HttpClient client, Guid classId, Guid studentId, int amount, string reason) =>
        SendWithTokenAsync(client, HttpMethod.Post, $"/api/classes/{classId}/points/award",
            new { studentIds = new[] { studentId }, amount, reason });

    private static async Task<List<StudentDto>> ListRosterAsync(HttpClient client, Guid classId)
    {
        var response = await client.GetAsync($"/api/classes/{classId}/students");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<StudentDto>>())!;
    }

    private static Task<HttpResponseMessage> PurgeAsync(HttpClient client, Guid classId, Guid studentId) =>
        SendWithTokenAsync(client, HttpMethod.Post, $"/api/classes/{classId}/students/{studentId}/purge");

    // Saves a grouping that places the student, exercising the group-placement cleanup path.
    private async Task<Guid> PlaceInGroupingAsync(Guid classId, Guid studentId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClassroomDbContext>();
        var grouping = new Grouping { ClassId = classId, GroupSize = 2, BalancedByGender = false };
        grouping.Members.Add(new GroupMember
        {
            GroupingId = grouping.Id,
            StudentId = studentId,
            GroupNumber = 0,
        });
        db.Groupings.Add(grouping);
        await db.SaveChangesAsync();
        return grouping.Id;
    }

    private async Task<Student?> LoadStudentRawAsync(Guid studentId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClassroomDbContext>();
        return await db.Students.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == studentId);
    }

    [Fact]
    public async Task Purge_erases_pii_avatar_and_inventory_and_anonymizes_the_ledger()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Purge class");
        var studentId = await AddStudentAsync(client, classId, "Alice", "Female");
        var reward = await AddBehaviorAsync(client, classId, "Helped a peer", 5);

        await AwardAsync(client, classId, studentId, reward.Id);
        await AdjustAsync(client, classId, studentId, 3, "Alice was great today"); // reason holds a name
        await PlaceInGroupingAsync(classId, studentId);

        var purge = await PurgeAsync(client, classId, studentId);
        Assert.Equal(HttpStatusCode.NoContent, purge.StatusCode);

        // The student row survives, anonymized and tombstoned.
        var raw = await LoadStudentRawAsync(studentId);
        Assert.NotNull(raw);
        Assert.Equal(Student.PurgedDisplayName, raw!.DisplayName);
        Assert.Null(raw.Gender);
        Assert.NotNull(raw.PurgedAt);
        Assert.NotNull(raw.DeletedAt); // also soft-deleted so the existing filter hides it

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClassroomDbContext>();

        // Avatar config, inventory and group placements are gone.
        Assert.False(await db.StudentEquipped.AnyAsync(e => e.StudentId == studentId));
        Assert.False(await db.StudentOwnedItems.AnyAsync(o => o.StudentId == studentId));
        Assert.False(await db.GroupMembers.AnyAsync(m => m.StudentId == studentId));

        // Ledger rows are retained (anonymized, not orphaned) — every row still resolves to its parent.
        var ledger = await db.PointTransactions.Where(t => t.StudentId == studentId).ToListAsync();
        Assert.Equal(2, ledger.Count);
        Assert.All(ledger, t => Assert.Null(t.Reason)); // free-text reasons scrubbed
        Assert.True(await db.Students.IgnoreQueryFilters().AnyAsync(s => s.Id == studentId));
    }

    [Fact]
    public async Task A_purged_student_disappears_from_the_roster_and_leaderboard()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Hidden after purge");
        var studentId = await AddStudentAsync(client, classId, "Bob");

        await PurgeAsync(client, classId, studentId);

        Assert.DoesNotContain(await ListRosterAsync(client, classId), s => s.Id == studentId);

        var board = await client.GetAsync($"/api/classes/{classId}/leaderboard");
        board.EnsureSuccessStatusCode();
        var entries = await board.Content.ReadFromJsonAsync<List<BalanceDto>>();
        Assert.DoesNotContain(entries!, e => e.StudentId == studentId);
    }

    [Fact]
    public async Task Purge_is_stronger_than_soft_delete()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Strength contrast");
        var removed = await AddStudentAsync(client, classId, "Removed Rita", "Female");
        var purged = await AddStudentAsync(client, classId, "Purged Pat", "Male");

        // Soft-delete one, purge the other.
        await SendWithTokenAsync(client, HttpMethod.Delete, $"/api/classes/{classId}/students/{removed}");
        await PurgeAsync(client, classId, purged);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClassroomDbContext>();

        // Soft-delete preserves PII and inventory (it is undoable); purge has destroyed them.
        var removedRaw = await db.Students.IgnoreQueryFilters().FirstAsync(s => s.Id == removed);
        Assert.Equal("Removed Rita", removedRaw.DisplayName);
        Assert.Null(removedRaw.PurgedAt);
        Assert.True(await db.StudentOwnedItems.AnyAsync(o => o.StudentId == removed));

        var purgedRaw = await db.Students.IgnoreQueryFilters().FirstAsync(s => s.Id == purged);
        Assert.Equal(Student.PurgedDisplayName, purgedRaw.DisplayName);
        Assert.NotNull(purgedRaw.PurgedAt);
        Assert.False(await db.StudentOwnedItems.AnyAsync(o => o.StudentId == purged));
    }

    [Fact]
    public async Task Purging_one_student_leaves_another_students_standing_intact()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Integrity class");
        var alice = await AddStudentAsync(client, classId, "Alice");
        var bob = await AddStudentAsync(client, classId, "Bob");
        var reward = await AddBehaviorAsync(client, classId, "Great work", 4);

        await AwardAsync(client, classId, alice, reward.Id);
        await AwardAsync(client, classId, bob, reward.Id);

        await PurgeAsync(client, classId, alice);

        // Bob is untouched; Alice's ledger rows still exist (not orphaned), so class history is intact.
        var bobBalance = await client.GetAsync($"/api/classes/{classId}/students/{bob}/balance");
        bobBalance.EnsureSuccessStatusCode();
        Assert.Equal(4, (await bobBalance.Content.ReadFromJsonAsync<BalanceDto>())!.LifetimeEarned);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClassroomDbContext>();
        Assert.Equal(1, await db.PointTransactions.CountAsync(t => t.StudentId == alice));
    }

    [Fact]
    public async Task An_already_removed_student_can_still_be_purged()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Remove then purge");
        var studentId = await AddStudentAsync(client, classId, "Cara");

        await SendWithTokenAsync(client, HttpMethod.Delete, $"/api/classes/{classId}/students/{studentId}");
        var purge = await PurgeAsync(client, classId, studentId);
        Assert.Equal(HttpStatusCode.NoContent, purge.StatusCode);

        var raw = await LoadStudentRawAsync(studentId);
        Assert.Equal(Student.PurgedDisplayName, raw!.DisplayName);
        Assert.NotNull(raw.PurgedAt);
    }

    [Fact]
    public async Task A_collaborator_cannot_purge_only_owners_can()
    {
        var ownerClient = CreateClient();
        await LoginAsSeededTeacherAsync(ownerClient);
        var classId = await CreateClassAsync(ownerClient, "Owner-gated purge");
        var studentId = await AddStudentAsync(ownerClient, classId, "Alice");

        // Add a second teacher as a Collaborator.
        var collaboratorEmail = UniqueEmail();
        await CreateTeacherAsync(collaboratorEmail);
        await SendWithTokenAsync(ownerClient, HttpMethod.Post, $"/api/classes/{classId}/teachers",
            new { email = collaboratorEmail, role = "Collaborator" });

        var collaboratorClient = CreateClient();
        await LoginAsync(collaboratorClient, collaboratorEmail, "Passw0rd!");

        var purge = await PurgeAsync(collaboratorClient, classId, studentId);
        Assert.Equal(HttpStatusCode.Forbidden, purge.StatusCode);

        // And the student is untouched.
        var raw = await LoadStudentRawAsync(studentId);
        Assert.Equal("Alice", raw!.DisplayName);
        Assert.Null(raw.PurgedAt);
    }

    [Fact]
    public async Task A_non_member_cannot_purge()
    {
        var ownerClient = CreateClient();
        await LoginAsSeededTeacherAsync(ownerClient);
        var classId = await CreateClassAsync(ownerClient, "Private purge");
        var studentId = await AddStudentAsync(ownerClient, classId, "Alice");

        var outsiderEmail = UniqueEmail();
        await CreateTeacherAsync(outsiderEmail);
        var outsiderClient = CreateClient();
        await LoginAsync(outsiderClient, outsiderEmail, "Passw0rd!");

        var purge = await PurgeAsync(outsiderClient, classId, studentId);
        Assert.Equal(HttpStatusCode.Forbidden, purge.StatusCode);
    }

    [Fact]
    public async Task Purging_a_student_from_another_class_is_not_found()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classA = await CreateClassAsync(client, "Class A");
        var classB = await CreateClassAsync(client, "Class B");
        var studentInB = await AddStudentAsync(client, classB, "Outsider");

        // Owner of A, but the student lives in B — the class-scoped lookup misses it.
        var purge = await PurgeAsync(client, classA, studentInB);
        Assert.Equal(HttpStatusCode.NotFound, purge.StatusCode);
    }

    [Fact]
    public async Task Purge_requires_authentication()
    {
        var client = CreateClient();
        var purge = await PurgeAsync(client, Guid.NewGuid(), Guid.NewGuid());
        Assert.Equal(HttpStatusCode.Unauthorized, purge.StatusCode);
    }
}
