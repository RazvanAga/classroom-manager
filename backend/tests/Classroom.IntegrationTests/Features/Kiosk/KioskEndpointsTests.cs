using System.Net;
using System.Net.Http.Json;
using Classroom.IntegrationTests.Infrastructure;

namespace Classroom.IntegrationTests.Features.Kiosk;

/// <summary>
/// Kiosk mode (design.md §5.4): entering mints a reduced-scope principal scoped to one class; it may
/// only read the roster and shop/equip for that class; every teacher/admin endpoint rejects it (403);
/// the correct PIN exits and restores the full teacher session.
/// </summary>
public class KioskEndpointsTests(ClassroomApiFactory factory) : IntegrationTestBase(factory)
{
    private record ClassDto(Guid Id, string Name, DateTime CreatedAt, bool IsArchived, string Role);
    private record StudentDto(Guid Id, Guid ClassId, string DisplayName, string? Gender, DateTime CreatedAt);
    private record KioskSessionDto(Guid ClassId, string ClassName);
    private record StoreItemDto(
        Guid Id, string Slot, string OptionValue, string DisplayName, int Cost, string? Rarity, bool Affordable);
    private record StoreDto(Guid StudentId, int Wallet, string Style, List<StoreItemDto> Items);

    // The seeded teacher's default kiosk PIN (SeedData default; the factory doesn't override it).
    private const string SeededPin = "1234";

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

    private static async Task GivePointsAsync(HttpClient client, Guid classId, Guid studentId, int amount)
    {
        var response = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/points/award",
            new { studentIds = new[] { studentId }, amount });
        response.EnsureSuccessStatusCode();
    }

    private static async Task<KioskSessionDto> EnterKioskAsync(HttpClient client, Guid classId)
    {
        var response = await SendWithTokenAsync(client, HttpMethod.Post, "/api/kiosk/enter", new { classId });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<KioskSessionDto>())!;
    }

    [Fact]
    public async Task Entering_kiosk_creates_a_reduced_scope_session_scoped_to_the_class()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Kiosk class");

        var session = await EnterKioskAsync(client, classId);
        Assert.Equal(classId, session.ClassId);
        Assert.Equal("Kiosk class", session.ClassName);

        // The kiosk session describes itself…
        var me = await client.GetAsync("/api/kiosk/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Equal(classId, (await me.Content.ReadFromJsonAsync<KioskSessionDto>())!.ClassId);

        // …but it is no longer the teacher: "who is the teacher?" is unauthenticated now.
        var teacherMe = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, teacherMe.StatusCode);
    }

    [Fact]
    public async Task Kiosk_can_read_roster_and_shop_and_equip_for_its_own_class()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Shop class");
        var studentId = await AddStudentAsync(client, classId, "Alice");
        await GivePointsAsync(client, classId, studentId, 100); // fund a purchase before going kiosk

        await EnterKioskAsync(client, classId);

        // Roster read (names to tap).
        var roster = await client.GetAsync($"/api/classes/{classId}/students");
        Assert.Equal(HttpStatusCode.OK, roster.StatusCode);
        var students = (await roster.Content.ReadFromJsonAsync<List<StudentDto>>())!;
        Assert.Contains(students, s => s.Id == studentId);

        // Shop list + a real purchase.
        var storeResp = await client.GetAsync($"/api/classes/{classId}/students/{studentId}/store");
        Assert.Equal(HttpStatusCode.OK, storeResp.StatusCode);
        var store = (await storeResp.Content.ReadFromJsonAsync<StoreDto>())!;
        var item = store.Items.Where(i => i.Affordable).OrderBy(i => i.Cost).First();

        var purchase = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/students/{studentId}/store/purchase", new { itemId = item.Id });
        Assert.Equal(HttpStatusCode.OK, purchase.StatusCode);

        // Avatar read + equip the freshly-bought option.
        var avatar = await client.GetAsync($"/api/classes/{classId}/students/{studentId}/avatar");
        Assert.Equal(HttpStatusCode.OK, avatar.StatusCode);
        var equip = await SendWithTokenAsync(client, HttpMethod.Put,
            $"/api/classes/{classId}/students/{studentId}/avatar/{item.Slot}", new { itemId = item.Id });
        Assert.Equal(HttpStatusCode.OK, equip.StatusCode);
    }

    [Fact]
    public async Task Kiosk_principal_is_rejected_from_every_teacher_and_admin_endpoint()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Locked-down class");
        var studentId = await AddStudentAsync(client, classId, "Bob");

        await EnterKioskAsync(client, classId);

        // Reads scoped to the teacher are 403 for kiosk (fail-closed, not a 500).
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/classes")).StatusCode);

        // Mutations carry a valid antiforgery token so they reach the handler and are denied there
        // (a missing token would 400 before authorization, hiding the real rejection).
        async Task AssertForbidden(HttpMethod method, string uri, object? body = null)
        {
            var response = await SendWithTokenAsync(client, method, uri, body);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // Points: award/deduct and undo.
        await AssertForbidden(HttpMethod.Post, $"/api/classes/{classId}/points/award",
            new { studentIds = new[] { studentId }, amount = 5 });
        await AssertForbidden(HttpMethod.Post,
            $"/api/classes/{classId}/points/transactions/{Guid.NewGuid()}/void");

        // Class management: create, delete, add co-teacher.
        await AssertForbidden(HttpMethod.Post, "/api/classes", new { name = "Hacked" });
        await AssertForbidden(HttpMethod.Delete, $"/api/classes/{classId}");
        await AssertForbidden(HttpMethod.Post, $"/api/classes/{classId}/teachers",
            new { email = "x@y.local" });

        // Behavior catalog CRUD.
        await AssertForbidden(HttpMethod.Post, $"/api/classes/{classId}/behaviors",
            new { name = "Bonus", defaultPoints = 3 });

        // Roster mutation (read is allowed; add/remove are not).
        await AssertForbidden(HttpMethod.Post, $"/api/classes/{classId}/students",
            new { displayName = "Sneaky" });
        await AssertForbidden(HttpMethod.Delete, $"/api/classes/{classId}/students/{studentId}");
    }

    [Fact]
    public async Task Kiosk_cannot_reach_another_class_than_its_claim()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classA = await CreateClassAsync(client, "Class A");
        var classB = await CreateClassAsync(client, "Class B");
        var studentB = await AddStudentAsync(client, classB, "Belongs to B");

        // Enter kiosk scoped to A, then try to reach B's roster and shop.
        await EnterKioskAsync(client, classA);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.GetAsync($"/api/classes/{classB}/students")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.GetAsync($"/api/classes/{classB}/students/{studentB}/store")).StatusCode);
    }

    [Fact]
    public async Task Wrong_PIN_stays_locked_correct_PIN_restores_the_teacher_session()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Exit class");
        await EnterKioskAsync(client, classId);

        // Wrong PIN: rejected, still in kiosk.
        var wrong = await SendWithTokenAsync(client, HttpMethod.Post, "/api/kiosk/exit", new { pin = "0000" });
        Assert.Equal(HttpStatusCode.Forbidden, wrong.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/kiosk/me")).StatusCode);

        // Correct PIN: exits and the full teacher session is back.
        var right = await SendWithTokenAsync(client, HttpMethod.Post, "/api/kiosk/exit", new { pin = SeededPin });
        Assert.Equal(HttpStatusCode.NoContent, right.StatusCode);

        // No kiosk session remains, and a teacher-only endpoint works again.
        Assert.NotEqual(HttpStatusCode.OK, (await client.GetAsync("/api/kiosk/me")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/classes")).StatusCode);
    }

    [Fact]
    public async Task A_teacher_can_set_a_pin_and_exit_with_it()
    {
        var email = UniqueEmail();
        await CreateTeacherAsync(email);
        var client = CreateClient();
        await LoginAsync(client, email, "Passw0rd!");
        var classId = await CreateClassAsync(client, "Custom PIN class");

        // A freshly-created teacher has no PIN; set one, then it gates the exit.
        var setPin = await SendWithTokenAsync(client, HttpMethod.Post, "/api/kiosk/pin", new { pin = "246810" });
        Assert.Equal(HttpStatusCode.NoContent, setPin.StatusCode);

        await EnterKioskAsync(client, classId);

        var wrong = await SendWithTokenAsync(client, HttpMethod.Post, "/api/kiosk/exit", new { pin = "111111" });
        Assert.Equal(HttpStatusCode.Forbidden, wrong.StatusCode);

        var right = await SendWithTokenAsync(client, HttpMethod.Post, "/api/kiosk/exit", new { pin = "246810" });
        Assert.Equal(HttpStatusCode.NoContent, right.StatusCode);
    }

    [Fact]
    public async Task A_non_member_teacher_cannot_enter_kiosk_for_a_class()
    {
        var owner = CreateClient();
        await LoginAsSeededTeacherAsync(owner);
        var classId = await CreateClassAsync(owner, "Owner's class");

        var outsiderEmail = UniqueEmail();
        await CreateTeacherAsync(outsiderEmail);
        var outsider = CreateClient();
        await LoginAsync(outsider, outsiderEmail, "Passw0rd!");

        var enter = await SendWithTokenAsync(outsider, HttpMethod.Post, "/api/kiosk/enter", new { classId });
        Assert.Equal(HttpStatusCode.Forbidden, enter.StatusCode);
    }

    [Fact]
    public async Task A_short_or_non_numeric_pin_is_rejected_by_validation()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);

        var tooShort = await SendWithTokenAsync(client, HttpMethod.Post, "/api/kiosk/pin", new { pin = "12" });
        Assert.Equal(HttpStatusCode.BadRequest, tooShort.StatusCode);

        var notNumeric = await SendWithTokenAsync(client, HttpMethod.Post, "/api/kiosk/pin", new { pin = "abcd" });
        Assert.Equal(HttpStatusCode.BadRequest, notNumeric.StatusCode);
    }

    [Fact]
    public async Task Kiosk_endpoints_require_authentication()
    {
        var client = CreateClient();
        var me = await client.GetAsync("/api/kiosk/me");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }
}
