using System.Net;
using System.Net.Http.Json;
using Classroom.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Classroom.IntegrationTests.Features.Avatars;

public class AvatarsEndpointsTests(ClassroomApiFactory factory) : IntegrationTestBase(factory)
{
    private record ClassDto(Guid Id, string Name, DateTime CreatedAt, bool IsArchived, string Role);
    private record StudentDto(Guid Id, Guid ClassId, string DisplayName, string? Gender, DateTime CreatedAt);

    private record AvatarItemDto(
        Guid Id, string Slot, string OptionValue, string DisplayName,
        int Cost, string? Rarity, bool IsDefault);
    private record CatalogDto(string Style, List<AvatarItemDto> Items);
    private record EquippedSlotDto(string Slot, Guid ItemId, string OptionValue);
    private record StudentAvatarDto(
        Guid StudentId, string Style, List<EquippedSlotDto> Equipped, List<AvatarItemDto> Owned);

    // The always-on slots modeled in this slice (matches AvatarSlot).
    private static readonly string[] AllSlots = ["Hair", "HairColor", "SkinColor", "Eyes", "Mouth"];

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

    private static async Task<CatalogDto> GetCatalogAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/avatar/catalog");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CatalogDto>())!;
    }

    private static async Task<StudentAvatarDto> GetAvatarAsync(HttpClient client, Guid classId, Guid studentId)
    {
        var response = await client.GetAsync($"/api/classes/{classId}/students/{studentId}/avatar");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StudentAvatarDto>())!;
    }

    [Fact]
    public async Task Catalog_is_seeded_with_one_locked_style_and_a_default_per_slot()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);

        var catalog = await GetCatalogAsync(client);

        Assert.Equal("adventurer", catalog.Style);
        Assert.NotEmpty(catalog.Items);

        // Exactly one free default per slot, so a new student always gets a complete avatar.
        foreach (var slot in AllSlots)
        {
            var defaults = catalog.Items.Where(i => i.Slot == slot && i.IsDefault).ToList();
            Assert.Single(defaults);
        }
        // Costs are 0 in this slice (the store is slice #9).
        Assert.All(catalog.Items, i => Assert.Equal(0, i.Cost));
        // Enum fields serialize as strings on the wire.
        Assert.All(catalog.Items, i => Assert.Contains(i.Slot, AllSlots));
    }

    [Fact]
    public async Task New_student_owns_the_defaults_and_renders_a_valid_equipped_config()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Avatar class");
        var studentId = await AddStudentAsync(client, classId, "Alice");

        var avatar = await GetAvatarAsync(client, classId, studentId);

        Assert.Equal("adventurer", avatar.Style);
        // One equipped option per slot, and each is a default the student owns.
        Assert.Equal(AllSlots.OrderBy(s => s), avatar.Equipped.Select(e => e.Slot).OrderBy(s => s));
        Assert.All(avatar.Equipped, e => Assert.False(string.IsNullOrWhiteSpace(e.OptionValue)));

        var ownedIds = avatar.Owned.Select(o => o.Id).ToHashSet();
        Assert.All(avatar.Equipped, e => Assert.Contains(e.ItemId, ownedIds));
        // The owned set is exactly the defaults (no buying yet in this slice).
        Assert.Equal(AllSlots.Length, avatar.Owned.Count);
        Assert.All(avatar.Owned, o => Assert.True(o.IsDefault));
    }

    [Fact]
    public async Task Equipping_an_owned_option_swaps_the_slot_and_can_switch_freely()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Equip class");
        var studentId = await AddStudentAsync(client, classId, "Bob");

        // Give Bob a second Hair option to switch to (no store yet, so grant ownership directly).
        var catalog = await GetCatalogAsync(client);
        var hairOptions = catalog.Items.Where(i => i.Slot == "Hair").ToList();
        var defaultHair = hairOptions.Single(i => i.IsDefault);
        var altHair = hairOptions.First(i => !i.IsDefault);
        await GrantOwnershipAsync(studentId, altHair.Id);

        // Equip the alternate hair.
        var equip = await SendWithTokenAsync(client, HttpMethod.Put,
            $"/api/classes/{classId}/students/{studentId}/avatar/Hair", new { itemId = altHair.Id });
        Assert.Equal(HttpStatusCode.OK, equip.StatusCode);
        var equipped = (await equip.Content.ReadFromJsonAsync<EquippedSlotDto>())!;
        Assert.Equal("Hair", equipped.Slot);
        Assert.Equal(altHair.Id, equipped.ItemId);
        Assert.Equal(altHair.OptionValue, equipped.OptionValue);

        var afterFirst = await GetAvatarAsync(client, classId, studentId);
        Assert.Equal(altHair.Id, afterFirst.Equipped.Single(e => e.Slot == "Hair").ItemId);

        // Switch back to the default — switching is unrestricted and free.
        var switchBack = await SendWithTokenAsync(client, HttpMethod.Put,
            $"/api/classes/{classId}/students/{studentId}/avatar/Hair", new { itemId = defaultHair.Id });
        Assert.Equal(HttpStatusCode.OK, switchBack.StatusCode);

        var afterSwitch = await GetAvatarAsync(client, classId, studentId);
        Assert.Equal(defaultHair.Id, afterSwitch.Equipped.Single(e => e.Slot == "Hair").ItemId);
        // Still exactly one equipped row per slot (upsert, not insert).
        Assert.Equal(AllSlots.Length, afterSwitch.Equipped.Count);
    }

    [Fact]
    public async Task Equipping_an_unowned_item_is_rejected()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Unowned class");
        var studentId = await AddStudentAsync(client, classId, "Cara");

        // A real catalog Hair option the student does NOT own (only defaults are granted).
        var catalog = await GetCatalogAsync(client);
        var unowned = catalog.Items.First(i => i.Slot == "Hair" && !i.IsDefault);

        var equip = await SendWithTokenAsync(client, HttpMethod.Put,
            $"/api/classes/{classId}/students/{studentId}/avatar/Hair", new { itemId = unowned.Id });
        Assert.Equal(HttpStatusCode.BadRequest, equip.StatusCode);
    }

    [Fact]
    public async Task Equipping_an_item_from_the_wrong_slot_is_rejected()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Wrong slot class");
        var studentId = await AddStudentAsync(client, classId, "Dan");

        // The student owns the default Eyes option; trying to equip it in the Hair slot must fail.
        var avatar = await GetAvatarAsync(client, classId, studentId);
        var eyesItemId = avatar.Equipped.Single(e => e.Slot == "Eyes").ItemId;

        var equip = await SendWithTokenAsync(client, HttpMethod.Put,
            $"/api/classes/{classId}/students/{studentId}/avatar/Hair", new { itemId = eyesItemId });
        Assert.Equal(HttpStatusCode.BadRequest, equip.StatusCode);
    }

    [Fact]
    public async Task Equipping_into_an_unknown_slot_is_rejected()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Bad slot class");
        var studentId = await AddStudentAsync(client, classId, "Eve");
        var avatar = await GetAvatarAsync(client, classId, studentId);
        var anyItem = avatar.Owned[0].Id;

        var equip = await SendWithTokenAsync(client, HttpMethod.Put,
            $"/api/classes/{classId}/students/{studentId}/avatar/Nonsense", new { itemId = anyItem });
        Assert.Equal(HttpStatusCode.BadRequest, equip.StatusCode);
    }

    [Fact]
    public async Task A_non_member_cannot_read_or_equip_a_students_avatar()
    {
        var ownerClient = CreateClient();
        await LoginAsSeededTeacherAsync(ownerClient);
        var classId = await CreateClassAsync(ownerClient, "Private avatars");
        var studentId = await AddStudentAsync(ownerClient, classId, "Alice");
        var avatar = await GetAvatarAsync(ownerClient, classId, studentId);
        var ownedItem = avatar.Owned[0];

        var outsiderEmail = UniqueEmail();
        await CreateTeacherAsync(outsiderEmail);
        var outsiderClient = CreateClient();
        await LoginAsync(outsiderClient, outsiderEmail, "Passw0rd!");

        var read = await outsiderClient.GetAsync(
            $"/api/classes/{classId}/students/{studentId}/avatar");
        Assert.Equal(HttpStatusCode.Forbidden, read.StatusCode);

        var equip = await SendWithTokenAsync(outsiderClient, HttpMethod.Put,
            $"/api/classes/{classId}/students/{studentId}/avatar/{ownedItem.Slot}",
            new { itemId = ownedItem.Id });
        Assert.Equal(HttpStatusCode.Forbidden, equip.StatusCode);
    }

    [Fact]
    public async Task Reading_an_avatar_for_a_student_in_another_class_is_not_found()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classA = await CreateClassAsync(client, "Class A");
        var classB = await CreateClassAsync(client, "Class B");
        var studentInB = await AddStudentAsync(client, classB, "Outsider");

        // A student id from class B addressed through class A's route is a cross-tenant miss (404).
        var read = await client.GetAsync(
            $"/api/classes/{classA}/students/{studentInB}/avatar");
        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
    }

    [Fact]
    public async Task Avatar_endpoints_require_authentication()
    {
        var client = CreateClient();

        var catalog = await client.GetAsync("/api/avatar/catalog");
        Assert.Equal(HttpStatusCode.Unauthorized, catalog.StatusCode);

        var avatar = await client.GetAsync(
            $"/api/classes/{Guid.NewGuid()}/students/{Guid.NewGuid()}/avatar");
        Assert.Equal(HttpStatusCode.Unauthorized, avatar.StatusCode);
    }

    /// <summary>
    /// Grants a student ownership of a catalog item directly (there is no store yet in this slice),
    /// so equip-switching tests have a second owned option to switch to.
    /// </summary>
    private async Task GrantOwnershipAsync(Guid studentId, Guid itemId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<Classroom.Infrastructure.Persistence.ClassroomDbContext>();
        db.StudentOwnedItems.Add(new Classroom.Domain.Avatars.StudentOwnedItem
        {
            StudentId = studentId,
            ItemId = itemId,
        });
        await db.SaveChangesAsync();
    }
}
