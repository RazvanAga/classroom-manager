using System.Net;
using System.Net.Http.Json;
using Classroom.IntegrationTests.Infrastructure;

namespace Classroom.IntegrationTests.Features.Behaviors;

public class BehaviorsEndpointsTests(ClassroomApiFactory factory) : IntegrationTestBase(factory)
{
    private record ClassDto(Guid Id, string Name, DateTime CreatedAt, bool IsArchived, string Role);
    private record BehaviorDto(Guid Id, Guid ClassId, string Name, int DefaultPoints, DateTime CreatedAt);

    private static string UniqueEmail() => $"teacher-{Guid.NewGuid():N}@classroom.local";

    private static async Task<Guid> CreateClassAsync(HttpClient client, string name)
    {
        var response = await SendWithTokenAsync(client, HttpMethod.Post, "/api/classes", new { name });
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<ClassDto>();
        return created!.Id;
    }

    private static async Task<List<BehaviorDto>> ListBehaviorsAsync(HttpClient client, Guid classId)
    {
        var response = await client.GetAsync($"/api/classes/{classId}/behaviors");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<BehaviorDto>>())!;
    }

    [Fact]
    public async Task Creating_a_class_seeds_the_default_behavior_catalog()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Seeded class");

        var behaviors = await ListBehaviorsAsync(client, classId);

        Assert.NotEmpty(behaviors);
        // Editable per-class rows: a mix of rewards and deductions.
        Assert.Contains(behaviors, b => b.DefaultPoints > 0);
        Assert.Contains(behaviors, b => b.DefaultPoints < 0);
        Assert.All(behaviors, b => Assert.Equal(classId, b.ClassId));
    }

    [Fact]
    public async Task A_teacher_can_add_edit_and_remove_behaviors()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "CRUD class");

        // Add (with a negative default — signed values are allowed).
        var add = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/behaviors", new { name = "Late", defaultPoints = -3 });
        Assert.Equal(HttpStatusCode.Created, add.StatusCode);
        var created = (await add.Content.ReadFromJsonAsync<BehaviorDto>())!;
        Assert.Equal(-3, created.DefaultPoints);

        // Edit.
        var edit = await SendWithTokenAsync(client, HttpMethod.Put,
            $"/api/classes/{classId}/behaviors/{created.Id}", new { name = "Very late", defaultPoints = -5 });
        Assert.Equal(HttpStatusCode.OK, edit.StatusCode);
        var updated = (await edit.Content.ReadFromJsonAsync<BehaviorDto>())!;
        Assert.Equal("Very late", updated.Name);
        Assert.Equal(-5, updated.DefaultPoints);

        // Remove.
        var remove = await SendWithTokenAsync(client, HttpMethod.Delete,
            $"/api/classes/{classId}/behaviors/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, remove.StatusCode);

        var after = await ListBehaviorsAsync(client, classId);
        Assert.DoesNotContain(after, b => b.Id == created.Id);
    }

    [Fact]
    public async Task Editing_one_classs_behaviors_does_not_affect_another()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classA = await CreateClassAsync(client, "Class A");
        var classB = await CreateClassAsync(client, "Class B");

        var aBehaviors = await ListBehaviorsAsync(client, classA);
        var target = aBehaviors[0];

        var edit = await SendWithTokenAsync(client, HttpMethod.Put,
            $"/api/classes/{classA}/behaviors/{target.Id}",
            new { name = "Renamed in A only", defaultPoints = 99 });
        edit.EnsureSuccessStatusCode();

        // Class B's catalog is untouched (copies, not a shared FK).
        var bBehaviors = await ListBehaviorsAsync(client, classB);
        Assert.DoesNotContain(bBehaviors, b => b.Name == "Renamed in A only");
        Assert.DoesNotContain(bBehaviors, b => b.Id == target.Id);
    }

    [Fact]
    public async Task A_behavior_from_another_class_cannot_be_edited_via_a_mismatched_class_route()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classA = await CreateClassAsync(client, "Owner of behavior");
        var classB = await CreateClassAsync(client, "Other class");

        var target = (await ListBehaviorsAsync(client, classA))[0];

        // The behavior exists, but not under classB — scoping must 404, not cross-edit.
        var edit = await SendWithTokenAsync(client, HttpMethod.Put,
            $"/api/classes/{classB}/behaviors/{target.Id}", new { name = "Hijack", defaultPoints = 1 });
        Assert.Equal(HttpStatusCode.NotFound, edit.StatusCode);
    }

    [Fact]
    public async Task A_non_member_cannot_read_or_modify_the_catalog()
    {
        var ownerClient = CreateClient();
        await LoginAsSeededTeacherAsync(ownerClient);
        var classId = await CreateClassAsync(ownerClient, "Private catalog");
        var target = (await ListBehaviorsAsync(ownerClient, classId))[0];

        var outsiderEmail = UniqueEmail();
        await CreateTeacherAsync(outsiderEmail);
        var outsiderClient = CreateClient();
        await LoginAsync(outsiderClient, outsiderEmail, "Passw0rd!");

        var read = await outsiderClient.GetAsync($"/api/classes/{classId}/behaviors");
        Assert.Equal(HttpStatusCode.Forbidden, read.StatusCode);

        var add = await SendWithTokenAsync(outsiderClient, HttpMethod.Post,
            $"/api/classes/{classId}/behaviors", new { name = "Intruder", defaultPoints = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, add.StatusCode);

        var edit = await SendWithTokenAsync(outsiderClient, HttpMethod.Put,
            $"/api/classes/{classId}/behaviors/{target.Id}", new { name = "Hijack", defaultPoints = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, edit.StatusCode);

        var delete = await SendWithTokenAsync(outsiderClient, HttpMethod.Delete,
            $"/api/classes/{classId}/behaviors/{target.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task Adding_a_behavior_with_a_blank_name_is_rejected()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Validation class");

        var add = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/behaviors", new { name = "", defaultPoints = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, add.StatusCode);
    }

    [Fact]
    public async Task Behavior_endpoints_require_authentication()
    {
        var client = CreateClient();
        var classId = Guid.NewGuid();

        var list = await client.GetAsync($"/api/classes/{classId}/behaviors");
        Assert.Equal(HttpStatusCode.Unauthorized, list.StatusCode);

        var add = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/behaviors", new { name = "Nope", defaultPoints = 1 });
        Assert.Equal(HttpStatusCode.Unauthorized, add.StatusCode);
    }
}
