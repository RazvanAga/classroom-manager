using System.Net;
using System.Net.Http.Json;
using Classroom.Domain.Points;
using Classroom.Infrastructure.Persistence;
using Classroom.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Classroom.IntegrationTests.Features.Reporting;

public class ReportingEndpointsTests(ClassroomApiFactory factory) : IntegrationTestBase(factory)
{
    private record ClassDto(Guid Id, string Name, DateTime CreatedAt, bool IsArchived, string Role);
    private record StudentDto(Guid Id, Guid ClassId, string DisplayName, string? Gender, DateTime CreatedAt);
    private record BehaviorDto(Guid Id, Guid ClassId, string Name, int DefaultPoints, DateTime CreatedAt);

    private record DayDto(DateOnly Date, int Earned, int Spent, int Net, int Balance);
    private record TimelineDto(
        Guid StudentId, string DisplayName, DateOnly From, DateOnly To,
        int OpeningBalance, int TotalEarned, int TotalSpent, List<DayDto> Days);

    private record BehaviorStatDto(Guid BehaviorId, string Name, int Count, int TotalPoints);
    private record BreakdownDto(
        DateOnly From, DateOnly To, int TotalAwarded, int TotalDeducted,
        int NetPoints, int AwardCount, List<BehaviorStatDto> Behaviors);

    private static string UniqueEmail() => $"teacher-{Guid.NewGuid():N}@classroom.local";

    private static async Task<Guid> CreateClassAsync(HttpClient client, string name)
    {
        var response = await SendWithTokenAsync(client, HttpMethod.Post, "/api/classes", new { name });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ClassDto>())!.Id;
    }

    private static async Task<Guid> AddStudentAsync(HttpClient client, Guid classId, string name)
    {
        var response = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/students", new { displayName = name });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StudentDto>())!.Id;
    }

    private static async Task<BehaviorDto> AddBehaviorAsync(
        HttpClient client, Guid classId, string name, int defaultPoints)
    {
        var response = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/behaviors", new { name, defaultPoints });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BehaviorDto>())!;
    }

    private static Task AwardAsync(HttpClient client, Guid classId, Guid studentId, Guid behaviorId) =>
        SendWithTokenAsync(client, HttpMethod.Post, $"/api/classes/{classId}/points/award",
            new { studentIds = new[] { studentId }, behaviorId });

    // Writes a row directly so the test controls its CreatedAt (the award endpoint stamps "now").
    private async Task WriteRowAsync(
        Guid studentId, int amount, PointTransactionType type, DateTime createdAtUtc,
        Guid? behaviorId = null, DateTime? voidedAt = null)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClassroomDbContext>();
        db.PointTransactions.Add(new PointTransaction
        {
            StudentId = studentId,
            Amount = amount,
            Type = type,
            BehaviorId = behaviorId,
            CreatedAt = createdAtUtc,
            VoidedAt = voidedAt,
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Timeline_buckets_a_students_points_per_day_with_a_running_balance()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Timeline class");
        var studentId = await AddStudentAsync(client, classId, "Alice");
        var reward = await AddBehaviorAsync(client, classId, "Helped a peer", 5);

        await WriteRowAsync(studentId, 5, PointTransactionType.Award,
            new DateTime(2026, 6, 10, 9, 0, 0, DateTimeKind.Utc), reward.Id);
        await WriteRowAsync(studentId, -2, PointTransactionType.Deduction,
            new DateTime(2026, 6, 12, 9, 0, 0, DateTimeKind.Utc), reward.Id);

        var response = await client.GetAsync(
            $"/api/classes/{classId}/reports/students/{studentId}/timeline" +
            "?from=2026-06-10&to=2026-06-12&tzOffsetMinutes=0");
        response.EnsureSuccessStatusCode();
        var timeline = (await response.Content.ReadFromJsonAsync<TimelineDto>())!;

        Assert.Equal(3, timeline.Days.Count); // every day in the range, gaps filled
        Assert.Equal(0, timeline.OpeningBalance);
        Assert.Equal(5, timeline.TotalEarned);
        Assert.Equal(-2, timeline.TotalSpent);

        Assert.Equal(5, timeline.Days[0].Net); // 10th
        Assert.Equal(5, timeline.Days[0].Balance);
        Assert.Equal(0, timeline.Days[1].Net); // 11th — empty
        Assert.Equal(5, timeline.Days[1].Balance); // carried forward
        Assert.Equal(-2, timeline.Days[2].Net); // 12th
        Assert.Equal(3, timeline.Days[2].Balance);
    }

    [Fact]
    public async Task Timeline_opening_balance_reflects_activity_before_the_range()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Opening class");
        var studentId = await AddStudentAsync(client, classId, "Bob");
        var reward = await AddBehaviorAsync(client, classId, "On task", 3);

        // Before the window.
        await WriteRowAsync(studentId, 8, PointTransactionType.Award,
            new DateTime(2026, 6, 1, 9, 0, 0, DateTimeKind.Utc), reward.Id);
        // In the window.
        await WriteRowAsync(studentId, 3, PointTransactionType.Award,
            new DateTime(2026, 6, 10, 9, 0, 0, DateTimeKind.Utc), reward.Id);

        var response = await client.GetAsync(
            $"/api/classes/{classId}/reports/students/{studentId}/timeline" +
            "?from=2026-06-10&to=2026-06-10&tzOffsetMinutes=0");
        response.EnsureSuccessStatusCode();
        var timeline = (await response.Content.ReadFromJsonAsync<TimelineDto>())!;

        Assert.Equal(8, timeline.OpeningBalance);
        Assert.Equal(11, Assert.Single(timeline.Days).Balance); // 8 opening + 3 in range
    }

    [Fact]
    public async Task Timeline_excludes_voided_rows()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Void timeline");
        var studentId = await AddStudentAsync(client, classId, "Cara");
        var reward = await AddBehaviorAsync(client, classId, "Teamwork", 4);

        await WriteRowAsync(studentId, 4, PointTransactionType.Award,
            new DateTime(2026, 6, 10, 9, 0, 0, DateTimeKind.Utc), reward.Id);
        await WriteRowAsync(studentId, 100, PointTransactionType.Award,
            new DateTime(2026, 6, 10, 10, 0, 0, DateTimeKind.Utc), reward.Id,
            voidedAt: new DateTime(2026, 6, 10, 11, 0, 0, DateTimeKind.Utc));

        var response = await client.GetAsync(
            $"/api/classes/{classId}/reports/students/{studentId}/timeline" +
            "?from=2026-06-10&to=2026-06-10&tzOffsetMinutes=0");
        response.EnsureSuccessStatusCode();
        var timeline = (await response.Content.ReadFromJsonAsync<TimelineDto>())!;

        Assert.Equal(4, Assert.Single(timeline.Days).Net); // the voided +100 is ignored
    }

    [Fact]
    public async Task Timeline_buckets_by_the_teachers_local_day()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Local-day class");
        var studentId = await AddStudentAsync(client, classId, "Dan");
        var reward = await AddBehaviorAsync(client, classId, "Great work", 7);

        // 23:30 UTC on the 10th is 01:30 on the 11th at UTC+2 → must land on the 11th (local).
        await WriteRowAsync(studentId, 7, PointTransactionType.Award,
            new DateTime(2026, 6, 10, 23, 30, 0, DateTimeKind.Utc), reward.Id);

        var response = await client.GetAsync(
            $"/api/classes/{classId}/reports/students/{studentId}/timeline" +
            "?from=2026-06-10&to=2026-06-11&tzOffsetMinutes=120");
        response.EnsureSuccessStatusCode();
        var timeline = (await response.Content.ReadFromJsonAsync<TimelineDto>())!;

        Assert.Equal(0, timeline.Days[0].Net); // the 10th saw nothing locally
        Assert.Equal(7, timeline.Days[1].Net); // the award belongs to the 11th
    }

    [Fact]
    public async Task Behavior_breakdown_counts_and_totals_per_behavior_most_common_first()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Breakdown class");
        var alice = await AddStudentAsync(client, classId, "Alice");
        var bob = await AddStudentAsync(client, classId, "Bob");
        var helped = await AddBehaviorAsync(client, classId, "Helped a peer", 5);
        var offTask = await AddBehaviorAsync(client, classId, "Off task", -2);

        // "Helped a peer" awarded 3 times (15 pts), "Off task" once (-2 pts).
        await AwardAsync(client, classId, alice, helped.Id);
        await AwardAsync(client, classId, bob, helped.Id);
        await AwardAsync(client, classId, alice, helped.Id);
        await AwardAsync(client, classId, bob, offTask.Id);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var response = await client.GetAsync(
            $"/api/classes/{classId}/reports/behaviors" +
            $"?from={today.AddDays(-1):yyyy-MM-dd}&to={today.AddDays(1):yyyy-MM-dd}&tzOffsetMinutes=0");
        response.EnsureSuccessStatusCode();
        var breakdown = (await response.Content.ReadFromJsonAsync<BreakdownDto>())!;

        Assert.Equal(2, breakdown.Behaviors.Count);
        Assert.Equal("Helped a peer", breakdown.Behaviors[0].Name); // most common first
        Assert.Equal(3, breakdown.Behaviors[0].Count);
        Assert.Equal(15, breakdown.Behaviors[0].TotalPoints);
        Assert.Equal("Off task", breakdown.Behaviors[1].Name);
        Assert.Equal(1, breakdown.Behaviors[1].Count);
        Assert.Equal(-2, breakdown.Behaviors[1].TotalPoints);

        Assert.Equal(15, breakdown.TotalAwarded);
        Assert.Equal(-2, breakdown.TotalDeducted);
        Assert.Equal(13, breakdown.NetPoints);
        Assert.Equal(4, breakdown.AwardCount);
    }

    [Fact]
    public async Task Behavior_breakdown_excludes_voided_and_out_of_range_rows()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Filtered breakdown");
        var studentId = await AddStudentAsync(client, classId, "Eve");
        var reward = await AddBehaviorAsync(client, classId, "Participation", 1);

        await WriteRowAsync(studentId, 1, PointTransactionType.Award,
            new DateTime(2026, 6, 10, 9, 0, 0, DateTimeKind.Utc), reward.Id); // in range
        await WriteRowAsync(studentId, 1, PointTransactionType.Award,
            new DateTime(2026, 6, 10, 10, 0, 0, DateTimeKind.Utc), reward.Id,
            voidedAt: DateTime.UtcNow); // voided
        await WriteRowAsync(studentId, 1, PointTransactionType.Award,
            new DateTime(2026, 5, 1, 9, 0, 0, DateTimeKind.Utc), reward.Id); // before range

        var response = await client.GetAsync(
            $"/api/classes/{classId}/reports/behaviors?from=2026-06-10&to=2026-06-10&tzOffsetMinutes=0");
        response.EnsureSuccessStatusCode();
        var breakdown = (await response.Content.ReadFromJsonAsync<BreakdownDto>())!;

        var stat = Assert.Single(breakdown.Behaviors);
        Assert.Equal(1, stat.Count); // only the one in-range, non-voided row
        Assert.Equal(1, breakdown.AwardCount);
    }

    [Fact]
    public async Task Behavior_breakdown_ignores_ad_hoc_adjustments()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Adjustment breakdown");
        var studentId = await AddStudentAsync(client, classId, "Finn");

        // A manual amount with no behavior is an Adjustment — it must not appear in the breakdown.
        await SendWithTokenAsync(client, HttpMethod.Post, $"/api/classes/{classId}/points/award",
            new { studentIds = new[] { studentId }, amount = 10, reason = "Top-up" });

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var response = await client.GetAsync(
            $"/api/classes/{classId}/reports/behaviors" +
            $"?from={today.AddDays(-1):yyyy-MM-dd}&to={today.AddDays(1):yyyy-MM-dd}&tzOffsetMinutes=0");
        response.EnsureSuccessStatusCode();
        var breakdown = (await response.Content.ReadFromJsonAsync<BreakdownDto>())!;

        Assert.Empty(breakdown.Behaviors);
        Assert.Equal(0, breakdown.AwardCount);
    }

    [Fact]
    public async Task An_inverted_date_range_is_rejected()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Bad range");
        var studentId = await AddStudentAsync(client, classId, "Gus");

        var timeline = await client.GetAsync(
            $"/api/classes/{classId}/reports/students/{studentId}/timeline" +
            "?from=2026-06-12&to=2026-06-10");
        Assert.Equal(HttpStatusCode.BadRequest, timeline.StatusCode);

        var breakdown = await client.GetAsync(
            $"/api/classes/{classId}/reports/behaviors?from=2026-06-12&to=2026-06-10");
        Assert.Equal(HttpStatusCode.BadRequest, breakdown.StatusCode);
    }

    [Fact]
    public async Task A_timeline_for_a_student_in_another_class_is_not_found()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classA = await CreateClassAsync(client, "Class A");
        var classB = await CreateClassAsync(client, "Class B");
        var studentInB = await AddStudentAsync(client, classB, "Outsider");

        var response = await client.GetAsync(
            $"/api/classes/{classA}/reports/students/{studentInB}/timeline");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task A_non_member_cannot_view_reports()
    {
        var ownerClient = CreateClient();
        await LoginAsSeededTeacherAsync(ownerClient);
        var classId = await CreateClassAsync(ownerClient, "Private reports");
        var studentId = await AddStudentAsync(ownerClient, classId, "Alice");

        var outsiderEmail = UniqueEmail();
        await CreateTeacherAsync(outsiderEmail);
        var outsiderClient = CreateClient();
        await LoginAsync(outsiderClient, outsiderEmail, "Passw0rd!");

        var timeline = await outsiderClient.GetAsync(
            $"/api/classes/{classId}/reports/students/{studentId}/timeline");
        Assert.Equal(HttpStatusCode.Forbidden, timeline.StatusCode);

        var breakdown = await outsiderClient.GetAsync($"/api/classes/{classId}/reports/behaviors");
        Assert.Equal(HttpStatusCode.Forbidden, breakdown.StatusCode);
    }

    [Fact]
    public async Task Report_endpoints_require_authentication()
    {
        var client = CreateClient();
        var classId = Guid.NewGuid();

        var timeline = await client.GetAsync(
            $"/api/classes/{classId}/reports/students/{Guid.NewGuid()}/timeline");
        Assert.Equal(HttpStatusCode.Unauthorized, timeline.StatusCode);

        var breakdown = await client.GetAsync($"/api/classes/{classId}/reports/behaviors");
        Assert.Equal(HttpStatusCode.Unauthorized, breakdown.StatusCode);
    }
}
