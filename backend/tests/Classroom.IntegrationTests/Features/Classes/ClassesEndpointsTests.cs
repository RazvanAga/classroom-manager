using System.Net;
using System.Net.Http.Json;
using Classroom.IntegrationTests.Infrastructure;

namespace Classroom.IntegrationTests.Features.Classes;

public class ClassesEndpointsTests(ClassroomApiFactory factory) : IntegrationTestBase(factory)
{
    private record ClassDto(Guid Id, string Name, string CurrencyIcon, DateTime CreatedAt, bool IsArchived, string Role);

    private static string UniqueEmail() => $"teacher-{Guid.NewGuid():N}@classroom.local";

    private static async Task<Guid> CreateClassAsync(HttpClient client, string name)
    {
        var response = await SendWithTokenAsync(client, HttpMethod.Post, "/api/classes", new { name });
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<ClassDto>();
        return created!.Id;
    }

    private static async Task<List<ClassDto>> ListAsync(HttpClient client, bool includeArchived = false)
    {
        var uri = includeArchived ? "/api/classes?includeArchived=true" : "/api/classes";
        var response = await client.GetAsync(uri);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<ClassDto>>())!;
    }

    private static async Task<ClassDto> GetClassAsync(HttpClient client, Guid id)
    {
        var response = await client.GetAsync($"/api/classes/{id}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ClassDto>())!;
    }

    [Fact]
    public async Task Creating_a_class_makes_the_creator_the_Owner()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);

        var id = await CreateClassAsync(client, "Room 5A");

        var mine = await ListAsync(client);
        var created = Assert.Single(mine, c => c.Id == id);
        Assert.Equal("Room 5A", created.Name);
        Assert.Equal("Owner", created.Role);
        Assert.False(created.IsArchived);
    }

    [Fact]
    public async Task A_teacher_sees_only_classes_they_belong_to()
    {
        var ownerClient = CreateClient();
        await LoginAsSeededTeacherAsync(ownerClient);
        var id = await CreateClassAsync(ownerClient, "Owner-only class");

        var otherEmail = UniqueEmail();
        await CreateTeacherAsync(otherEmail);
        var otherClient = CreateClient();
        await LoginAsync(otherClient, otherEmail, "Passw0rd!");

        var otherList = await ListAsync(otherClient);
        Assert.DoesNotContain(otherList, c => c.Id == id);
    }

    [Fact]
    public async Task A_non_member_is_denied_reading_a_class()
    {
        var ownerClient = CreateClient();
        await LoginAsSeededTeacherAsync(ownerClient);
        var id = await CreateClassAsync(ownerClient, "Private class");

        var outsiderEmail = UniqueEmail();
        await CreateTeacherAsync(outsiderEmail);
        var outsiderClient = CreateClient();
        await LoginAsync(outsiderClient, outsiderEmail, "Passw0rd!");

        var response = await outsiderClient.GetAsync($"/api/classes/{id}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task A_non_member_is_denied_writing_to_a_class()
    {
        var ownerClient = CreateClient();
        await LoginAsSeededTeacherAsync(ownerClient);
        var id = await CreateClassAsync(ownerClient, "Write-protected class");

        var outsiderEmail = UniqueEmail();
        await CreateTeacherAsync(outsiderEmail);
        var outsiderClient = CreateClient();
        await LoginAsync(outsiderClient, outsiderEmail, "Passw0rd!");

        var archive = await SendWithTokenAsync(outsiderClient, HttpMethod.Post, $"/api/classes/{id}/archive");
        Assert.Equal(HttpStatusCode.Forbidden, archive.StatusCode);

        var delete = await SendWithTokenAsync(outsiderClient, HttpMethod.Delete, $"/api/classes/{id}");
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task A_Collaborator_cannot_perform_Owner_only_operations()
    {
        var ownerClient = CreateClient();
        await LoginAsSeededTeacherAsync(ownerClient);
        var id = await CreateClassAsync(ownerClient, "Co-taught class");

        var collaboratorEmail = UniqueEmail();
        await CreateTeacherAsync(collaboratorEmail);
        var addResponse = await SendWithTokenAsync(ownerClient, HttpMethod.Post,
            $"/api/classes/{id}/teachers", new { email = collaboratorEmail, role = "Collaborator" });
        addResponse.EnsureSuccessStatusCode();

        var collaboratorClient = CreateClient();
        await LoginAsync(collaboratorClient, collaboratorEmail, "Passw0rd!");

        // Can read (membership) ...
        var read = await collaboratorClient.GetAsync($"/api/classes/{id}");
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);

        // ... but cannot delete or manage teachers (Owner-only).
        var delete = await SendWithTokenAsync(collaboratorClient, HttpMethod.Delete, $"/api/classes/{id}");
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);

        var addAnother = await SendWithTokenAsync(collaboratorClient, HttpMethod.Post,
            $"/api/classes/{id}/teachers", new { email = UniqueEmail(), role = "Collaborator" });
        Assert.Equal(HttpStatusCode.Forbidden, addAnother.StatusCode);

        var removeOwner = await SendWithTokenAsync(collaboratorClient, HttpMethod.Delete,
            $"/api/classes/{id}/teachers/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Forbidden, removeOwner.StatusCode);
    }

    [Fact]
    public async Task An_Owner_can_add_a_collaborator_who_then_sees_the_class_and_can_be_removed()
    {
        var ownerClient = CreateClient();
        await LoginAsSeededTeacherAsync(ownerClient);
        var id = await CreateClassAsync(ownerClient, "Shared class");

        var collaboratorEmail = UniqueEmail();
        var collaboratorId = await CreateTeacherAsync(collaboratorEmail);

        var add = await SendWithTokenAsync(ownerClient, HttpMethod.Post,
            $"/api/classes/{id}/teachers", new { email = collaboratorEmail, role = "Collaborator" });
        Assert.Equal(HttpStatusCode.NoContent, add.StatusCode);

        var collaboratorClient = CreateClient();
        await LoginAsync(collaboratorClient, collaboratorEmail, "Passw0rd!");
        var collaboratorList = await ListAsync(collaboratorClient);
        var seen = Assert.Single(collaboratorList, c => c.Id == id);
        Assert.Equal("Collaborator", seen.Role);

        var remove = await SendWithTokenAsync(ownerClient, HttpMethod.Delete,
            $"/api/classes/{id}/teachers/{collaboratorId}");
        Assert.Equal(HttpStatusCode.NoContent, remove.StatusCode);

        var afterRemoval = await ListAsync(collaboratorClient);
        Assert.DoesNotContain(afterRemoval, c => c.Id == id);
    }

    [Fact]
    public async Task An_Owner_can_delete_a_class_and_it_disappears()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var id = await CreateClassAsync(client, "To be deleted");

        var delete = await SendWithTokenAsync(client, HttpMethod.Delete, $"/api/classes/{id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        // Soft-deleted: hidden by the global query filter, so even the Owner can no longer reach it.
        var read = await client.GetAsync($"/api/classes/{id}");
        Assert.Equal(HttpStatusCode.Forbidden, read.StatusCode);

        var list = await ListAsync(client, includeArchived: true);
        Assert.DoesNotContain(list, c => c.Id == id);
    }

    [Fact]
    public async Task An_archived_class_is_excluded_from_the_active_list_but_visible_with_the_flag()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var id = await CreateClassAsync(client, "Last year's class");

        var archive = await SendWithTokenAsync(client, HttpMethod.Post, $"/api/classes/{id}/archive");
        Assert.Equal(HttpStatusCode.NoContent, archive.StatusCode);

        var active = await ListAsync(client);
        Assert.DoesNotContain(active, c => c.Id == id);

        var all = await ListAsync(client, includeArchived: true);
        var archived = Assert.Single(all, c => c.Id == id);
        Assert.True(archived.IsArchived);
    }

    [Fact]
    public async Task Adding_an_unknown_teacher_returns_404_and_a_duplicate_returns_409()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var id = await CreateClassAsync(client, "Roster guard class");

        var unknown = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{id}/teachers", new { email = UniqueEmail(), role = "Collaborator" });
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);

        var collaboratorEmail = UniqueEmail();
        await CreateTeacherAsync(collaboratorEmail);
        var first = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{id}/teachers", new { email = collaboratorEmail, role = "Collaborator" });
        first.EnsureSuccessStatusCode();

        var duplicate = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{id}/teachers", new { email = collaboratorEmail, role = "Collaborator" });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task A_new_class_defaults_to_the_star_currency_icon()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var id = await CreateClassAsync(client, "Default icon class");

        var fromGet = await GetClassAsync(client, id);
        Assert.Equal("Star", fromGet.CurrencyIcon);

        var fromList = Assert.Single(await ListAsync(client), c => c.Id == id);
        Assert.Equal("Star", fromList.CurrencyIcon);
    }

    [Fact]
    public async Task A_member_can_change_the_currency_icon_to_an_allowed_value()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var id = await CreateClassAsync(client, "Recolorable class");

        var change = await SendWithTokenAsync(client, HttpMethod.Put,
            $"/api/classes/{id}/currency-icon", new { icon = "Gem" });
        Assert.Equal(HttpStatusCode.NoContent, change.StatusCode);

        var updated = await GetClassAsync(client, id);
        Assert.Equal("Gem", updated.CurrencyIcon);
    }

    [Fact]
    public async Task An_invalid_currency_icon_is_rejected()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var id = await CreateClassAsync(client, "Strict icon class");

        var change = await SendWithTokenAsync(client, HttpMethod.Put,
            $"/api/classes/{id}/currency-icon", new { icon = "banana" });
        Assert.Equal(HttpStatusCode.BadRequest, change.StatusCode);

        // The icon is unchanged after a rejected request.
        var unchanged = await GetClassAsync(client, id);
        Assert.Equal("Star", unchanged.CurrencyIcon);
    }

    [Fact]
    public async Task A_non_member_cannot_change_the_currency_icon()
    {
        var ownerClient = CreateClient();
        await LoginAsSeededTeacherAsync(ownerClient);
        var id = await CreateClassAsync(ownerClient, "Icon-protected class");

        var outsiderEmail = UniqueEmail();
        await CreateTeacherAsync(outsiderEmail);
        var outsiderClient = CreateClient();
        await LoginAsync(outsiderClient, outsiderEmail, "Passw0rd!");

        var change = await SendWithTokenAsync(outsiderClient, HttpMethod.Put,
            $"/api/classes/{id}/currency-icon", new { icon = "Gem" });
        Assert.Equal(HttpStatusCode.Forbidden, change.StatusCode);
    }

    [Fact]
    public async Task Class_endpoints_require_authentication()
    {
        var client = CreateClient();

        var list = await client.GetAsync("/api/classes");
        Assert.Equal(HttpStatusCode.Unauthorized, list.StatusCode);

        var create = await SendWithTokenAsync(client, HttpMethod.Post, "/api/classes", new { name = "Nope" });
        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
    }
}
