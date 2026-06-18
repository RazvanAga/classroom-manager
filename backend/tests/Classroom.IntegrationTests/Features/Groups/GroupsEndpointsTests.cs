using System.Net;
using System.Net.Http.Json;
using Classroom.IntegrationTests.Infrastructure;

namespace Classroom.IntegrationTests.Features.Groups;

public class GroupsEndpointsTests(ClassroomApiFactory factory) : IntegrationTestBase(factory)
{
    private record ClassDto(Guid Id, string Name, DateTime CreatedAt, bool IsArchived, string Role);
    private record MemberDto(Guid StudentId, string DisplayName, string? Gender);
    private record GroupDto(int GroupNumber, IReadOnlyList<MemberDto> Members);
    private record FormedDto(int GroupSize, bool BalancedByGender, int Seed, IReadOnlyList<GroupDto> Groups);
    private record SavedDto(Guid Id, Guid ClassId, string? Name, int GroupSize, bool BalancedByGender, DateTime CreatedAt, IReadOnlyList<GroupDto> Groups);
    private record SummaryDto(Guid Id, string? Name, int GroupSize, bool BalancedByGender, DateTime CreatedAt, int GroupCount, int StudentCount);

    private static string UniqueEmail() => $"teacher-{Guid.NewGuid():N}@classroom.local";

    private static async Task<Guid> CreateClassAsync(HttpClient client, string name)
    {
        var response = await SendWithTokenAsync(client, HttpMethod.Post, "/api/classes", new { name });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ClassDto>())!.Id;
    }

    private static async Task AddStudentsAsync(HttpClient client, Guid classId, params string[] lines)
    {
        var response = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/students/bulk", new { text = string.Join('\n', lines) });
        response.EnsureSuccessStatusCode();
    }

    private static IEnumerable<MemberDto> AllMembers(IEnumerable<GroupDto> groups) =>
        groups.SelectMany(g => g.Members);

    [Fact]
    public async Task Preview_forms_even_groups_over_the_whole_roster()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Groups class");
        await AddStudentsAsync(client, classId, "A", "B", "C", "D", "E", "F", "G");

        var response = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/groupings/preview", new { groupSize = 2, balanceByGender = false });
        response.EnsureSuccessStatusCode();
        var formed = (await response.Content.ReadFromJsonAsync<FormedDto>())!;

        // 7 students, size 2 => 4 groups, sizes differ by at most one.
        Assert.Equal(4, formed.Groups.Count);
        var sizes = formed.Groups.Select(g => g.Members.Count).ToList();
        Assert.True(sizes.Max() - sizes.Min() <= 1);
        // Everyone placed exactly once.
        Assert.Equal(7, AllMembers(formed.Groups).Select(m => m.StudentId).Distinct().Count());
    }

    [Fact]
    public async Task Saving_with_the_previews_seed_persists_the_same_arrangement()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Save class");
        await AddStudentsAsync(client, classId, "A", "B", "C", "D", "E");

        var previewRes = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/groupings/preview", new { groupSize = 2, balanceByGender = false });
        var preview = (await previewRes.Content.ReadFromJsonAsync<FormedDto>())!;

        var saveRes = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/groupings",
            new { name = "Lab pairs", groupSize = 2, balanceByGender = false, seed = preview.Seed });
        Assert.Equal(HttpStatusCode.Created, saveRes.StatusCode);
        var saved = (await saveRes.Content.ReadFromJsonAsync<SavedDto>())!;

        Assert.Equal("Lab pairs", saved.Name);

        // Retrieve it and confirm the membership matches the preview (per group, order-independent).
        var getRes = await client.GetAsync($"/api/classes/{classId}/groupings/{saved.Id}");
        getRes.EnsureSuccessStatusCode();
        var retrieved = (await getRes.Content.ReadFromJsonAsync<SavedDto>())!;

        var previewByGroup = preview.Groups.ToDictionary(
            g => g.GroupNumber, g => g.Members.Select(m => m.StudentId).OrderBy(x => x).ToList());
        var retrievedByGroup = retrieved.Groups.ToDictionary(
            g => g.GroupNumber, g => g.Members.Select(m => m.StudentId).OrderBy(x => x).ToList());

        Assert.Equal(previewByGroup.Keys.OrderBy(x => x), retrievedByGroup.Keys.OrderBy(x => x));
        foreach (var (groupNumber, members) in previewByGroup)
        {
            Assert.Equal(members, retrievedByGroup[groupNumber]);
        }
    }

    [Fact]
    public async Task A_saved_grouping_appears_in_the_class_list()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "List class");
        await AddStudentsAsync(client, classId, "A", "B", "C", "D");

        var previewRes = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/groupings/preview", new { groupSize = 2, balanceByGender = false });
        var preview = (await previewRes.Content.ReadFromJsonAsync<FormedDto>())!;

        var saveRes = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/groupings",
            new { name = (string?)null, groupSize = 2, balanceByGender = false, seed = preview.Seed });
        saveRes.EnsureSuccessStatusCode();

        var listRes = await client.GetAsync($"/api/classes/{classId}/groupings");
        listRes.EnsureSuccessStatusCode();
        var summaries = (await listRes.Content.ReadFromJsonAsync<List<SummaryDto>>())!;

        var summary = Assert.Single(summaries);
        Assert.Equal(2, summary.GroupCount);
        Assert.Equal(4, summary.StudentCount);
        Assert.Null(summary.Name);
    }

    [Fact]
    public async Task Gender_balancing_spreads_each_bucket_across_groups()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Balanced class");
        await AddStudentsAsync(client, classId,
            "F1, F", "F2, F", "F3, F", "F4, F",
            "M1, M", "M2, M", "M3, M", "M4, M");

        var response = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/groupings/preview", new { groupSize = 2, balanceByGender = true });
        response.EnsureSuccessStatusCode();
        var formed = (await response.Content.ReadFromJsonAsync<FormedDto>())!;

        // 8 students, size 2 => 4 groups; 4 of each gender spread => exactly one per group.
        Assert.Equal(4, formed.Groups.Count);
        foreach (var g in formed.Groups)
        {
            Assert.Equal(1, g.Members.Count(m => m.Gender == "Female"));
            Assert.Equal(1, g.Members.Count(m => m.Gender == "Male"));
        }
    }

    [Fact]
    public async Task Forming_over_an_empty_class_returns_400()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Empty class");

        var response = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/groupings/preview", new { groupSize = 2, balanceByGender = false });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task An_invalid_group_size_is_rejected()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Bad size class");
        await AddStudentsAsync(client, classId, "A", "B");

        var response = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/groupings/preview", new { groupSize = 0, balanceByGender = false });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_non_member_cannot_form_or_list_groupings()
    {
        var ownerClient = CreateClient();
        await LoginAsSeededTeacherAsync(ownerClient);
        var classId = await CreateClassAsync(ownerClient, "Private groups");
        await AddStudentsAsync(ownerClient, classId, "A", "B");

        var outsiderEmail = UniqueEmail();
        await CreateTeacherAsync(outsiderEmail);
        var outsiderClient = CreateClient();
        await LoginAsync(outsiderClient, outsiderEmail, "Passw0rd!");

        var preview = await SendWithTokenAsync(outsiderClient, HttpMethod.Post,
            $"/api/classes/{classId}/groupings/preview", new { groupSize = 2, balanceByGender = false });
        Assert.Equal(HttpStatusCode.Forbidden, preview.StatusCode);

        var list = await outsiderClient.GetAsync($"/api/classes/{classId}/groupings");
        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
    }

    [Fact]
    public async Task Retrieving_an_unknown_grouping_returns_404()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Missing grouping class");

        var response = await client.GetAsync($"/api/classes/{classId}/groupings/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Grouping_endpoints_require_authentication()
    {
        var client = CreateClient();
        var classId = Guid.NewGuid();

        var list = await client.GetAsync($"/api/classes/{classId}/groupings");
        Assert.Equal(HttpStatusCode.Unauthorized, list.StatusCode);

        var preview = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/groupings/preview", new { groupSize = 2, balanceByGender = false });
        Assert.Equal(HttpStatusCode.Unauthorized, preview.StatusCode);
    }
}
