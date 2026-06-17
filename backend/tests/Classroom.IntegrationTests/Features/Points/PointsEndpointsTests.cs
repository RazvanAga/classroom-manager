using System.Net;
using System.Net.Http.Json;
using Classroom.Domain.Points;
using Classroom.Infrastructure.Persistence;
using Classroom.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Classroom.IntegrationTests.Features.Points;

public class PointsEndpointsTests(ClassroomApiFactory factory) : IntegrationTestBase(factory)
{
    private record ClassDto(Guid Id, string Name, DateTime CreatedAt, bool IsArchived, string Role);
    private record StudentDto(Guid Id, Guid ClassId, string DisplayName, string? Gender, DateTime CreatedAt);
    private record BehaviorDto(Guid Id, Guid ClassId, string Name, int DefaultPoints, DateTime CreatedAt);

    private record TransactionDto(
        Guid Id, Guid StudentId, int Amount, string Type,
        Guid? BehaviorId, Guid? BatchId, string? Reason, DateTime CreatedAt);
    private record AwardDto(Guid? BatchId, List<TransactionDto> Transactions);
    private record BalanceDto(Guid StudentId, int Wallet, int LifetimeEarned);
    private record LeaderboardEntryDto(Guid StudentId, string DisplayName, int Wallet, int LifetimeEarned);

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

    private static async Task<List<BehaviorDto>> ListBehaviorsAsync(HttpClient client, Guid classId)
    {
        var response = await client.GetAsync($"/api/classes/{classId}/behaviors");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<BehaviorDto>>())!;
    }

    private static async Task<BehaviorDto> AddBehaviorAsync(
        HttpClient client, Guid classId, string name, int defaultPoints)
    {
        var response = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/behaviors", new { name, defaultPoints });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BehaviorDto>())!;
    }

    private static async Task<BalanceDto> GetBalanceAsync(HttpClient client, Guid classId, Guid studentId)
    {
        var response = await client.GetAsync($"/api/classes/{classId}/students/{studentId}/balance");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BalanceDto>())!;
    }

    private static async Task<List<LeaderboardEntryDto>> GetLeaderboardAsync(HttpClient client, Guid classId)
    {
        var response = await client.GetAsync($"/api/classes/{classId}/leaderboard");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<LeaderboardEntryDto>>())!;
    }

    [Fact]
    public async Task Awarding_a_behavior_creates_a_row_and_updates_wallet_and_lifetime()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Award class");
        var studentId = await AddStudentAsync(client, classId, "Alice");
        var reward = await AddBehaviorAsync(client, classId, "Helped a peer", 5);

        var award = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/points/award",
            new { studentIds = new[] { studentId }, behaviorId = reward.Id });
        Assert.Equal(HttpStatusCode.Created, award.StatusCode);

        var result = (await award.Content.ReadFromJsonAsync<AwardDto>())!;
        var row = Assert.Single(result.Transactions);
        Assert.Equal(5, row.Amount);
        Assert.Equal("Award", row.Type);
        Assert.Equal(reward.Id, row.BehaviorId);
        Assert.Null(result.BatchId); // single student → no batch id
        Assert.Null(row.BatchId); // and the persisted row carries none either

        var balance = await GetBalanceAsync(client, classId, studentId);
        Assert.Equal(5, balance.Wallet);
        Assert.Equal(5, balance.LifetimeEarned);
    }

    [Fact]
    public async Task Deduction_lowers_the_wallet_but_not_lifetime()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Deduct class");
        var studentId = await AddStudentAsync(client, classId, "Bob");
        var reward = await AddBehaviorAsync(client, classId, "On task", 4);
        var penalty = await AddBehaviorAsync(client, classId, "Off task", -6);

        await SendWithTokenAsync(client, HttpMethod.Post, $"/api/classes/{classId}/points/award",
            new { studentIds = new[] { studentId }, behaviorId = reward.Id });
        await SendWithTokenAsync(client, HttpMethod.Post, $"/api/classes/{classId}/points/award",
            new { studentIds = new[] { studentId }, behaviorId = penalty.Id });

        var balance = await GetBalanceAsync(client, classId, studentId);
        Assert.Equal(-2, balance.Wallet); // 4 - 6, negative wallet is allowed
        Assert.Equal(4, balance.LifetimeEarned); // only the positive award counts
    }

    [Fact]
    public async Task An_explicit_amount_is_an_adjustment_that_moves_the_wallet_only()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Adjust class");
        var studentId = await AddStudentAsync(client, classId, "Cara");

        var award = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/points/award",
            new { studentIds = new[] { studentId }, amount = 10, reason = "Manual top-up" });
        Assert.Equal(HttpStatusCode.Created, award.StatusCode);
        var row = Assert.Single((await award.Content.ReadFromJsonAsync<AwardDto>())!.Transactions);
        Assert.Equal("Adjustment", row.Type);
        Assert.Equal("Manual top-up", row.Reason);

        var balance = await GetBalanceAsync(client, classId, studentId);
        Assert.Equal(10, balance.Wallet);
        Assert.Equal(0, balance.LifetimeEarned); // adjustments never build lifetime
    }

    [Fact]
    public async Task Bulk_award_writes_one_row_per_student_sharing_a_batch_id()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Bulk class");
        var alice = await AddStudentAsync(client, classId, "Alice");
        var bob = await AddStudentAsync(client, classId, "Bob");
        var cara = await AddStudentAsync(client, classId, "Cara");
        var reward = await AddBehaviorAsync(client, classId, "Teamwork", 2);

        var award = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/points/award",
            new { studentIds = new[] { alice, bob, cara }, behaviorId = reward.Id });
        Assert.Equal(HttpStatusCode.Created, award.StatusCode);

        var result = (await award.Content.ReadFromJsonAsync<AwardDto>())!;
        Assert.NotNull(result.BatchId);
        Assert.Equal(3, result.Transactions.Count);
        // One row per requested student, all sharing the single batch id.
        Assert.Equal(new[] { alice, bob, cara }.OrderBy(g => g),
            result.Transactions.Select(t => t.StudentId).OrderBy(g => g));
        Assert.All(result.Transactions, t => Assert.Equal(result.BatchId, t.BatchId));
        Assert.All(result.Transactions, t => Assert.Equal(2, t.Amount));

        // Each student's lifetime reflects the shared award.
        foreach (var id in new[] { alice, bob, cara })
        {
            var balance = await GetBalanceAsync(client, classId, id);
            Assert.Equal(2, balance.LifetimeEarned);
        }
    }

    [Fact]
    public async Task Leaderboard_ranks_students_by_lifetime_earned()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Ranked class");
        var low = await AddStudentAsync(client, classId, "Low");
        var high = await AddStudentAsync(client, classId, "High");
        var mid = await AddStudentAsync(client, classId, "Mid");
        var reward = await AddBehaviorAsync(client, classId, "Great work", 1);

        // High earns 3, Mid earns 2, Low earns 1 — awarded out of rank order on purpose.
        await SendWithTokenAsync(client, HttpMethod.Post, $"/api/classes/{classId}/points/award",
            new { studentIds = new[] { low }, behaviorId = reward.Id });
        for (var i = 0; i < 2; i++)
        {
            await SendWithTokenAsync(client, HttpMethod.Post, $"/api/classes/{classId}/points/award",
                new { studentIds = new[] { mid }, behaviorId = reward.Id });
        }
        for (var i = 0; i < 3; i++)
        {
            await SendWithTokenAsync(client, HttpMethod.Post, $"/api/classes/{classId}/points/award",
                new { studentIds = new[] { high }, behaviorId = reward.Id });
        }

        var board = await GetLeaderboardAsync(client, classId);
        Assert.Equal(new[] { high, mid, low }, board.Select(e => e.StudentId));
        Assert.Equal(new[] { 3, 2, 1 }, board.Select(e => e.LifetimeEarned));
    }

    [Fact]
    public async Task Leaderboard_breaks_ties_alphabetically_by_name()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Tie class");
        var zoe = await AddStudentAsync(client, classId, "Zoe");
        var amy = await AddStudentAsync(client, classId, "Amy");
        var reward = await AddBehaviorAsync(client, classId, "Equal", 1);

        // Both earn the same lifetime total; the only deterministic order is the name tie-break.
        await SendWithTokenAsync(client, HttpMethod.Post, $"/api/classes/{classId}/points/award",
            new { studentIds = new[] { zoe, amy }, behaviorId = reward.Id });

        var board = await GetLeaderboardAsync(client, classId);
        Assert.Equal(new[] { amy, zoe }, board.Select(e => e.StudentId));
    }

    [Fact]
    public async Task Voided_rows_are_excluded_from_balance_and_leaderboard()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Void class");
        var studentId = await AddStudentAsync(client, classId, "Alice");
        var reward = await AddBehaviorAsync(client, classId, "Helped a peer", 5);

        await SendWithTokenAsync(client, HttpMethod.Post, $"/api/classes/{classId}/points/award",
            new { studentIds = new[] { studentId }, behaviorId = reward.Id });

        // Seed a large, already-voided Award directly (the void endpoint is slice #7). Both the
        // leaderboard's raw-SQL sums and the balance fold must skip it.
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClassroomDbContext>();
            db.PointTransactions.Add(new PointTransaction
            {
                StudentId = studentId,
                Amount = 100,
                Type = PointTransactionType.Award,
                BehaviorId = reward.Id,
                VoidedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        var balance = await GetBalanceAsync(client, classId, studentId);
        Assert.Equal(5, balance.Wallet); // voided +100 ignored
        Assert.Equal(5, balance.LifetimeEarned);

        var entry = Assert.Single(await GetLeaderboardAsync(client, classId));
        Assert.Equal(5, entry.Wallet);
        Assert.Equal(5, entry.LifetimeEarned);
    }

    [Fact]
    public async Task Awarding_a_student_from_another_class_is_rejected()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classA = await CreateClassAsync(client, "Class A");
        var classB = await CreateClassAsync(client, "Class B");
        var studentInB = await AddStudentAsync(client, classB, "Outsider");
        var rewardInA = (await ListBehaviorsAsync(client, classA))[0];

        // A student id from class B cannot be awarded through class A's route.
        var award = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classA}/points/award",
            new { studentIds = new[] { studentInB }, behaviorId = rewardInA.Id });
        Assert.Equal(HttpStatusCode.NotFound, award.StatusCode);
    }

    [Fact]
    public async Task A_non_member_cannot_award_read_balance_or_view_the_leaderboard()
    {
        var ownerClient = CreateClient();
        await LoginAsSeededTeacherAsync(ownerClient);
        var classId = await CreateClassAsync(ownerClient, "Private points");
        var studentId = await AddStudentAsync(ownerClient, classId, "Alice");
        var reward = (await ListBehaviorsAsync(ownerClient, classId))[0];

        var outsiderEmail = UniqueEmail();
        await CreateTeacherAsync(outsiderEmail);
        var outsiderClient = CreateClient();
        await LoginAsync(outsiderClient, outsiderEmail, "Passw0rd!");

        var award = await SendWithTokenAsync(outsiderClient, HttpMethod.Post,
            $"/api/classes/{classId}/points/award",
            new { studentIds = new[] { studentId }, behaviorId = reward.Id });
        Assert.Equal(HttpStatusCode.Forbidden, award.StatusCode);

        var balance = await outsiderClient.GetAsync(
            $"/api/classes/{classId}/students/{studentId}/balance");
        Assert.Equal(HttpStatusCode.Forbidden, balance.StatusCode);

        var board = await outsiderClient.GetAsync($"/api/classes/{classId}/leaderboard");
        Assert.Equal(HttpStatusCode.Forbidden, board.StatusCode);
    }

    [Fact]
    public async Task Award_requires_either_a_behavior_or_an_amount_but_not_both()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Validation class");
        var studentId = await AddStudentAsync(client, classId, "Alice");
        var reward = (await ListBehaviorsAsync(client, classId))[0];

        var neither = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/points/award",
            new { studentIds = new[] { studentId } });
        Assert.Equal(HttpStatusCode.BadRequest, neither.StatusCode);

        var both = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/points/award",
            new { studentIds = new[] { studentId }, behaviorId = reward.Id, amount = 3 });
        Assert.Equal(HttpStatusCode.BadRequest, both.StatusCode);

        var noStudents = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/points/award",
            new { studentIds = Array.Empty<Guid>(), behaviorId = reward.Id });
        Assert.Equal(HttpStatusCode.BadRequest, noStudents.StatusCode);
    }

    [Fact]
    public async Task Point_endpoints_require_authentication()
    {
        var client = CreateClient();
        var classId = Guid.NewGuid();

        var award = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/points/award",
            new { studentIds = new[] { Guid.NewGuid() }, amount = 1 });
        Assert.Equal(HttpStatusCode.Unauthorized, award.StatusCode);

        var board = await client.GetAsync($"/api/classes/{classId}/leaderboard");
        Assert.Equal(HttpStatusCode.Unauthorized, board.StatusCode);
    }
}
