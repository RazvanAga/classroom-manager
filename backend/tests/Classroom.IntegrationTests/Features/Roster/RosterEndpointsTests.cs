using System.Net;
using System.Net.Http.Json;
using Classroom.IntegrationTests.Infrastructure;

namespace Classroom.IntegrationTests.Features.Roster;

public class RosterEndpointsTests(ClassroomApiFactory factory) : IntegrationTestBase(factory)
{
    private record ClassDto(Guid Id, string Name, DateTime CreatedAt, bool IsArchived, string Role);
    private record StudentDto(Guid Id, Guid ClassId, string DisplayName, string? Gender, DateTime CreatedAt);

    private static string UniqueEmail() => $"teacher-{Guid.NewGuid():N}@classroom.local";

    private static async Task<Guid> CreateClassAsync(HttpClient client, string name)
    {
        var response = await SendWithTokenAsync(client, HttpMethod.Post, "/api/classes", new { name });
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<ClassDto>();
        return created!.Id;
    }

    private static async Task<List<StudentDto>> ListStudentsAsync(HttpClient client, Guid classId)
    {
        var response = await client.GetAsync($"/api/classes/{classId}/students");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<StudentDto>>())!;
    }

    [Fact]
    public async Task Single_add_creates_a_student_in_the_class()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Room 1");

        var add = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/students", new { displayName = "Alice", gender = "Female" });
        Assert.Equal(HttpStatusCode.Created, add.StatusCode);

        var roster = await ListStudentsAsync(client, classId);
        var alice = Assert.Single(roster);
        Assert.Equal("Alice", alice.DisplayName);
        Assert.Equal("Female", alice.Gender);
        Assert.Equal(classId, alice.ClassId);
    }

    [Fact]
    public async Task Single_add_without_a_gender_stores_null()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Room 2");

        var add = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/students", new { displayName = "Sam" });
        Assert.Equal(HttpStatusCode.Created, add.StatusCode);

        var student = Assert.Single(await ListStudentsAsync(client, classId));
        Assert.Null(student.Gender);
    }

    [Fact]
    public async Task Bulk_paste_creates_students_with_parsed_gender()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Room 3");

        var text = "Alice, F\nBob, M\nCharlie\n\n   \nDana, X";
        var bulk = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/students/bulk", new { text });
        bulk.EnsureSuccessStatusCode();

        var roster = (await ListStudentsAsync(client, classId)).OrderBy(s => s.CreatedAt).ToList();
        Assert.Equal(4, roster.Count);
        Assert.Equal(("Alice", "Female"), (roster[0].DisplayName, roster[0].Gender));
        Assert.Equal(("Bob", "Male"), (roster[1].DisplayName, roster[1].Gender));
        Assert.Equal(("Charlie", (string?)null), (roster[2].DisplayName, roster[2].Gender));
        // Unrecognized marker keeps the student with no gender (tolerant parser).
        Assert.Equal(("Dana", (string?)null), (roster[3].DisplayName, roster[3].Gender));
    }

    [Fact]
    public async Task Soft_deleted_students_are_excluded_from_the_roster()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Room 4");

        var add = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/students", new { displayName = "Leaving Student" });
        var created = await add.Content.ReadFromJsonAsync<StudentDto>();

        var delete = await SendWithTokenAsync(client, HttpMethod.Delete,
            $"/api/classes/{classId}/students/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var roster = await ListStudentsAsync(client, classId);
        Assert.DoesNotContain(roster, s => s.Id == created.Id);
    }

    [Fact]
    public async Task A_non_member_cannot_read_or_modify_the_roster()
    {
        var ownerClient = CreateClient();
        await LoginAsSeededTeacherAsync(ownerClient);
        var classId = await CreateClassAsync(ownerClient, "Private roster");

        var outsiderEmail = UniqueEmail();
        await CreateTeacherAsync(outsiderEmail);
        var outsiderClient = CreateClient();
        await LoginAsync(outsiderClient, outsiderEmail, "Passw0rd!");

        var read = await outsiderClient.GetAsync($"/api/classes/{classId}/students");
        Assert.Equal(HttpStatusCode.Forbidden, read.StatusCode);

        var add = await SendWithTokenAsync(outsiderClient, HttpMethod.Post,
            $"/api/classes/{classId}/students", new { displayName = "Intruder" });
        Assert.Equal(HttpStatusCode.Forbidden, add.StatusCode);

        var bulk = await SendWithTokenAsync(outsiderClient, HttpMethod.Post,
            $"/api/classes/{classId}/students/bulk", new { text = "Intruder" });
        Assert.Equal(HttpStatusCode.Forbidden, bulk.StatusCode);

        var delete = await SendWithTokenAsync(outsiderClient, HttpMethod.Delete,
            $"/api/classes/{classId}/students/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);

        // The owner's roster stayed empty despite the outsider's attempts.
        Assert.Empty(await ListStudentsAsync(ownerClient, classId));
    }

    [Fact]
    public async Task A_collaborator_can_modify_the_roster()
    {
        var ownerClient = CreateClient();
        await LoginAsSeededTeacherAsync(ownerClient);
        var classId = await CreateClassAsync(ownerClient, "Co-taught roster");

        var collaboratorEmail = UniqueEmail();
        await CreateTeacherAsync(collaboratorEmail);
        var add = await SendWithTokenAsync(ownerClient, HttpMethod.Post,
            $"/api/classes/{classId}/teachers", new { email = collaboratorEmail, role = "Collaborator" });
        add.EnsureSuccessStatusCode();

        var collaboratorClient = CreateClient();
        await LoginAsync(collaboratorClient, collaboratorEmail, "Passw0rd!");

        var addStudent = await SendWithTokenAsync(collaboratorClient, HttpMethod.Post,
            $"/api/classes/{classId}/students", new { displayName = "Pat" });
        Assert.Equal(HttpStatusCode.Created, addStudent.StatusCode);

        Assert.Single(await ListStudentsAsync(ownerClient, classId));
    }

    [Fact]
    public async Task Removing_an_unknown_student_returns_404()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Room 5");

        var delete = await SendWithTokenAsync(client, HttpMethod.Delete,
            $"/api/classes/{classId}/students/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
    }

    [Fact]
    public async Task Adding_a_student_with_a_blank_name_is_rejected()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Room 6");

        var add = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/students", new { displayName = "" });
        Assert.Equal(HttpStatusCode.BadRequest, add.StatusCode);
    }

    [Fact]
    public async Task Roster_endpoints_require_authentication()
    {
        var client = CreateClient();
        var classId = Guid.NewGuid();

        var list = await client.GetAsync($"/api/classes/{classId}/students");
        Assert.Equal(HttpStatusCode.Unauthorized, list.StatusCode);

        var add = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/students", new { displayName = "Nope" });
        Assert.Equal(HttpStatusCode.Unauthorized, add.StatusCode);
    }
}
