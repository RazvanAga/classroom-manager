using System.Net;
using System.Net.Http.Json;
using Classroom.IntegrationTests.Infrastructure;

namespace Classroom.IntegrationTests.Features.Points;

public class UndoEndpointsTests(ClassroomApiFactory factory) : IntegrationTestBase(factory)
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
    private record BatchVoidDto(Guid BatchId, int VoidedCount);
    private record ActivityItemDto(
        Guid Id, Guid StudentId, string StudentName, int Amount, string Type,
        Guid? BehaviorId, string? BehaviorName, Guid? BatchId, string? Reason,
        DateTime CreatedAt, DateTime? VoidedAt);

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

    private static async Task<AwardDto> AwardAsync(
        HttpClient client, Guid classId, Guid[] studentIds, Guid behaviorId)
    {
        var response = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/points/award", new { studentIds, behaviorId });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AwardDto>())!;
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

    private static async Task<List<ActivityItemDto>> GetActivityAsync(HttpClient client, Guid classId)
    {
        var response = await client.GetAsync($"/api/classes/{classId}/points/transactions");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<ActivityItemDto>>())!;
    }

    [Fact]
    public async Task Voiding_a_transaction_removes_it_from_wallet_and_lifetime()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Undo class");
        var studentId = await AddStudentAsync(client, classId, "Alice");
        var reward = await AddBehaviorAsync(client, classId, "Helped a peer", 5);

        var award = await AwardAsync(client, classId, [studentId], reward.Id);
        var txId = award.Transactions[0].Id;

        var before = await GetBalanceAsync(client, classId, studentId);
        Assert.Equal(5, before.Wallet);
        Assert.Equal(5, before.LifetimeEarned);

        var voidResponse = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/points/transactions/{txId}/void");
        Assert.Equal(HttpStatusCode.NoContent, voidResponse.StatusCode);

        var after = await GetBalanceAsync(client, classId, studentId);
        Assert.Equal(0, after.Wallet);
        Assert.Equal(0, after.LifetimeEarned);
    }

    [Fact]
    public async Task Voiding_a_bulk_award_by_batch_id_voids_all_of_its_rows()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Bulk undo class");
        var alice = await AddStudentAsync(client, classId, "Alice");
        var bob = await AddStudentAsync(client, classId, "Bob");
        var cara = await AddStudentAsync(client, classId, "Cara");
        var reward = await AddBehaviorAsync(client, classId, "Teamwork", 2);

        var award = await AwardAsync(client, classId, [alice, bob, cara], reward.Id);
        var batchId = award.BatchId!.Value;

        var voidResponse = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/points/batches/{batchId}/void");
        Assert.Equal(HttpStatusCode.OK, voidResponse.StatusCode);
        var result = (await voidResponse.Content.ReadFromJsonAsync<BatchVoidDto>())!;
        Assert.Equal(batchId, result.BatchId);
        Assert.Equal(3, result.VoidedCount);

        // Every student's totals are back to zero.
        foreach (var id in new[] { alice, bob, cara })
        {
            var balance = await GetBalanceAsync(client, classId, id);
            Assert.Equal(0, balance.Wallet);
            Assert.Equal(0, balance.LifetimeEarned);
        }

        var board = await GetLeaderboardAsync(client, classId);
        Assert.All(board, e => Assert.Equal(0, e.LifetimeEarned));
    }

    [Fact]
    public async Task A_voided_row_is_retained_in_the_activity_feed_for_audit()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Audit class");
        var studentId = await AddStudentAsync(client, classId, "Alice");
        var reward = await AddBehaviorAsync(client, classId, "Great work", 3);

        var award = await AwardAsync(client, classId, [studentId], reward.Id);
        var txId = award.Transactions[0].Id;

        await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/points/transactions/{txId}/void");

        // The row is still present (audit trail) but marked voided and excluded from the balance.
        var activity = await GetActivityAsync(client, classId);
        var row = Assert.Single(activity, a => a.Id == txId);
        Assert.NotNull(row.VoidedAt);
        Assert.Equal("Alice", row.StudentName);
        Assert.Equal("Great work", row.BehaviorName);
    }

    [Fact]
    public async Task Voiding_an_already_voided_transaction_is_a_conflict()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Double undo class");
        var studentId = await AddStudentAsync(client, classId, "Alice");
        var reward = await AddBehaviorAsync(client, classId, "On task", 1);

        var award = await AwardAsync(client, classId, [studentId], reward.Id);
        var txId = award.Transactions[0].Id;

        var first = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/points/transactions/{txId}/void");
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

        var second = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/points/transactions/{txId}/void");
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task A_transaction_cannot_be_voided_through_a_mismatched_class_route()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classA = await CreateClassAsync(client, "Class A");
        var classB = await CreateClassAsync(client, "Class B");
        var studentInA = await AddStudentAsync(client, classA, "Alice");
        var rewardInA = await AddBehaviorAsync(client, classA, "Helped", 4);

        var award = await AwardAsync(client, classA, [studentInA], rewardInA.Id);
        var txId = award.Transactions[0].Id;

        // The transaction belongs to class A; voiding via class B's route must 404, not cross-void.
        var voidResponse = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classB}/points/transactions/{txId}/void");
        Assert.Equal(HttpStatusCode.NotFound, voidResponse.StatusCode);

        // And the row is untouched.
        var balance = await GetBalanceAsync(client, classA, studentInA);
        Assert.Equal(4, balance.Wallet);
    }

    [Fact]
    public async Task A_non_member_cannot_list_or_void_transactions()
    {
        var ownerClient = CreateClient();
        await LoginAsSeededTeacherAsync(ownerClient);
        var classId = await CreateClassAsync(ownerClient, "Private undo");
        var studentId = await AddStudentAsync(ownerClient, classId, "Alice");
        var reward = await AddBehaviorAsync(ownerClient, classId, "Helped", 2);
        var award = await AwardAsync(ownerClient, classId, [studentId], reward.Id);
        var txId = award.Transactions[0].Id;

        var outsiderEmail = UniqueEmail();
        await CreateTeacherAsync(outsiderEmail);
        var outsiderClient = CreateClient();
        await LoginAsync(outsiderClient, outsiderEmail, "Passw0rd!");

        var list = await outsiderClient.GetAsync($"/api/classes/{classId}/points/transactions");
        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);

        var single = await SendWithTokenAsync(outsiderClient, HttpMethod.Post,
            $"/api/classes/{classId}/points/transactions/{txId}/void");
        Assert.Equal(HttpStatusCode.Forbidden, single.StatusCode);

        var batch = await SendWithTokenAsync(outsiderClient, HttpMethod.Post,
            $"/api/classes/{classId}/points/batches/{Guid.NewGuid()}/void");
        Assert.Equal(HttpStatusCode.Forbidden, batch.StatusCode);
    }

    [Fact]
    public async Task Undo_endpoints_require_authentication()
    {
        var client = CreateClient();
        var classId = Guid.NewGuid();

        var list = await client.GetAsync($"/api/classes/{classId}/points/transactions");
        Assert.Equal(HttpStatusCode.Unauthorized, list.StatusCode);

        var single = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/points/transactions/{Guid.NewGuid()}/void");
        Assert.Equal(HttpStatusCode.Unauthorized, single.StatusCode);
    }
}
