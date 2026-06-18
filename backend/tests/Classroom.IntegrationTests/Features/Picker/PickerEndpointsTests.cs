using System.Net;
using System.Net.Http.Json;
using Classroom.IntegrationTests.Infrastructure;

namespace Classroom.IntegrationTests.Features.Picker;

public class PickerEndpointsTests(ClassroomApiFactory factory) : IntegrationTestBase(factory)
{
    private record ClassDto(Guid Id, string Name, DateTime CreatedAt, bool IsArchived, string Role);
    private record StudentDto(Guid Id, Guid ClassId, string DisplayName, string? Gender, DateTime CreatedAt);
    private record PickDto(Guid PickedStudentId, string PickedDisplayName, bool CycleReset, IReadOnlyList<Guid> AlreadyPickedIds);

    private static string UniqueEmail() => $"teacher-{Guid.NewGuid():N}@classroom.local";

    private static async Task<Guid> CreateClassAsync(HttpClient client, string name)
    {
        var response = await SendWithTokenAsync(client, HttpMethod.Post, "/api/classes", new { name });
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<ClassDto>();
        return created!.Id;
    }

    private static async Task AddStudentsAsync(HttpClient client, Guid classId, params string[] names)
    {
        var response = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/students/bulk", new { text = string.Join('\n', names) });
        response.EnsureSuccessStatusCode();
    }

    private static async Task<List<Guid>> RosterIdsAsync(HttpClient client, Guid classId)
    {
        var response = await client.GetAsync($"/api/classes/{classId}/students");
        response.EnsureSuccessStatusCode();
        var roster = await response.Content.ReadFromJsonAsync<List<StudentDto>>();
        return roster!.Select(s => s.Id).ToList();
    }

    private static async Task<(HttpStatusCode Status, PickDto? Pick)> PickAsync(
        HttpClient client, Guid classId, IReadOnlyList<Guid> alreadyPicked)
    {
        var response = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/picker/pick", new { alreadyPickedIds = alreadyPicked });
        if (!response.IsSuccessStatusCode)
        {
            return (response.StatusCode, null);
        }

        return (response.StatusCode, await response.Content.ReadFromJsonAsync<PickDto>());
    }

    [Fact]
    public async Task A_full_cycle_picks_everyone_once_then_resets()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Picker class");
        await AddStudentsAsync(client, classId, "Alice", "Bob", "Charlie", "Dana");
        var roster = await RosterIdsAsync(client, classId);

        // Drive a whole cycle, feeding each response's set into the next request like the SPA does.
        var picks = new List<Guid>();
        IReadOnlyList<Guid> carried = [];
        for (var i = 0; i < roster.Count; i++)
        {
            var (_, pick) = await PickAsync(client, classId, carried);
            Assert.False(pick!.CycleReset);
            picks.Add(pick.PickedStudentId);
            carried = pick.AlreadyPickedIds;
        }

        // Everyone was picked exactly once — no repeats within the cycle.
        Assert.Equal(roster.Count, picks.Distinct().Count());
        Assert.Equal(roster.OrderBy(x => x), picks.OrderBy(x => x));

        // One more pick exhausts the cycle, so the server signals a reset and starts fresh.
        var (_, resetPick) = await PickAsync(client, classId, carried);
        Assert.True(resetPick!.CycleReset);
        Assert.Equal([resetPick.PickedStudentId], resetPick.AlreadyPickedIds);
    }

    [Fact]
    public async Task The_picked_student_belongs_to_the_class()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Single-student class");
        await AddStudentsAsync(client, classId, "Solo");
        var roster = await RosterIdsAsync(client, classId);

        var (status, pick) = await PickAsync(client, classId, []);

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal(roster.Single(), pick!.PickedStudentId);
        Assert.Equal("Solo", pick.PickedDisplayName);
    }

    [Fact]
    public async Task Picking_from_an_empty_class_returns_400()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Empty class");

        var (status, _) = await PickAsync(client, classId, []);

        Assert.Equal(HttpStatusCode.BadRequest, status);
    }

    [Fact]
    public async Task A_non_member_cannot_pick()
    {
        var ownerClient = CreateClient();
        await LoginAsSeededTeacherAsync(ownerClient);
        var classId = await CreateClassAsync(ownerClient, "Private picker");
        await AddStudentsAsync(ownerClient, classId, "Alice");

        var outsiderEmail = UniqueEmail();
        await CreateTeacherAsync(outsiderEmail);
        var outsiderClient = CreateClient();
        await LoginAsync(outsiderClient, outsiderEmail, "Passw0rd!");

        var (status, _) = await PickAsync(outsiderClient, classId, []);
        Assert.Equal(HttpStatusCode.Forbidden, status);
    }

    [Fact]
    public async Task Picking_requires_authentication()
    {
        var client = CreateClient();
        var classId = Guid.NewGuid();

        var (status, _) = await PickAsync(client, classId, []);
        Assert.Equal(HttpStatusCode.Unauthorized, status);
    }
}
