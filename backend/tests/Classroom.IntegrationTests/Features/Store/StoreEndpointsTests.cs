using System.Net;
using System.Net.Http.Json;
using Classroom.IntegrationTests.Infrastructure;

namespace Classroom.IntegrationTests.Features.Store;

public class StoreEndpointsTests(ClassroomApiFactory factory) : IntegrationTestBase(factory)
{
    private record ClassDto(Guid Id, string Name, DateTime CreatedAt, bool IsArchived, string Role);
    private record StudentDto(Guid Id, Guid ClassId, string DisplayName, string? Gender, DateTime CreatedAt);

    private record StoreItemDto(
        Guid Id, string Slot, string OptionValue, string DisplayName,
        int Cost, string? Rarity, bool Affordable);
    private record StoreDto(Guid StudentId, int Wallet, string Style, List<StoreItemDto> Items);
    private record PurchaseDto(Guid StudentId, Guid ItemId, int Cost, int Wallet);
    private record BalanceDto(Guid StudentId, int Wallet, int LifetimeEarned);

    // Mirrors the avatar DTOs so a purchase can be confirmed to enter inventory and be equippable.
    private record AvatarItemDto(
        Guid Id, string Slot, string OptionValue, string DisplayName,
        int Cost, string? Rarity, bool IsDefault);
    private record EquippedSlotDto(string Slot, Guid ItemId, string OptionValue);
    private record StudentAvatarDto(
        Guid StudentId, string Style, List<EquippedSlotDto> Equipped, List<AvatarItemDto> Owned);

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

    /// <summary>Credits a student's wallet directly via an explicit point adjustment (positive amount).</summary>
    private static async Task GivePointsAsync(HttpClient client, Guid classId, Guid studentId, int amount)
    {
        var response = await SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/points/award",
            new { studentIds = new[] { studentId }, amount });
        response.EnsureSuccessStatusCode();
    }

    private static async Task<StoreDto> GetStoreAsync(HttpClient client, Guid classId, Guid studentId)
    {
        var response = await client.GetAsync($"/api/classes/{classId}/students/{studentId}/store");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StoreDto>())!;
    }

    private static async Task<BalanceDto> GetBalanceAsync(HttpClient client, Guid classId, Guid studentId)
    {
        var response = await client.GetAsync($"/api/classes/{classId}/students/{studentId}/balance");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BalanceDto>())!;
    }

    private static async Task<StudentAvatarDto> GetAvatarAsync(HttpClient client, Guid classId, Guid studentId)
    {
        var response = await client.GetAsync($"/api/classes/{classId}/students/{studentId}/avatar");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StudentAvatarDto>())!;
    }

    private static Task<HttpResponseMessage> PurchaseAsync(
        HttpClient client, Guid classId, Guid studentId, Guid itemId) =>
        SendWithTokenAsync(client, HttpMethod.Post,
            $"/api/classes/{classId}/students/{studentId}/store/purchase", new { itemId });

    [Fact]
    public async Task Store_lists_buyable_options_excluding_owned_with_affordability_flags()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Store class");
        var studentId = await AddStudentAsync(client, classId, "Alice");

        // A modest wallet: some options are affordable, pricier ones are not.
        await GivePointsAsync(client, classId, studentId, 15);

        var store = await GetStoreAsync(client, classId, studentId);

        Assert.Equal("adventurer", store.Style);
        Assert.Equal(15, store.Wallet);
        Assert.NotEmpty(store.Items);
        // The free defaults are owned from creation, so they never appear in the store.
        Assert.All(store.Items, i => Assert.True(i.Cost > 0));
        // Affordability is computed against the wallet.
        Assert.All(store.Items, i => Assert.Equal(store.Wallet >= i.Cost, i.Affordable));
        Assert.Contains(store.Items, i => i.Affordable);
        Assert.Contains(store.Items, i => !i.Affordable);
    }

    [Fact]
    public async Task Buying_an_affordable_option_succeeds_drops_the_wallet_and_enters_inventory()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Buy class");
        var studentId = await AddStudentAsync(client, classId, "Bob");
        await GivePointsAsync(client, classId, studentId, 100);

        var store = await GetStoreAsync(client, classId, studentId);
        var item = store.Items.OrderBy(i => i.Cost).First();

        var purchase = await PurchaseAsync(client, classId, studentId, item.Id);
        Assert.Equal(HttpStatusCode.OK, purchase.StatusCode);
        var result = (await purchase.Content.ReadFromJsonAsync<PurchaseDto>())!;
        Assert.Equal(item.Id, result.ItemId);
        Assert.Equal(item.Cost, result.Cost);
        Assert.Equal(100 - item.Cost, result.Wallet);

        // The wallet really dropped (the negative Purchase ledger row), and lifetime is untouched.
        var balance = await GetBalanceAsync(client, classId, studentId);
        Assert.Equal(100 - item.Cost, balance.Wallet);

        // The option left the store and entered the inventory, where it is equippable (the free #8 path).
        var storeAfter = await GetStoreAsync(client, classId, studentId);
        Assert.DoesNotContain(storeAfter.Items, i => i.Id == item.Id);
        var avatar = await GetAvatarAsync(client, classId, studentId);
        Assert.Contains(avatar.Owned, o => o.Id == item.Id);

        var equip = await SendWithTokenAsync(client, HttpMethod.Put,
            $"/api/classes/{classId}/students/{studentId}/avatar/{item.Slot}", new { itemId = item.Id });
        Assert.Equal(HttpStatusCode.OK, equip.StatusCode);
    }

    [Fact]
    public async Task Buying_an_option_the_wallet_cannot_afford_is_rejected_with_402()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Broke class");
        var studentId = await AddStudentAsync(client, classId, "Cara");
        await GivePointsAsync(client, classId, studentId, 5);

        var store = await GetStoreAsync(client, classId, studentId);
        var tooDear = store.Items.First(i => i.Cost > 5);

        var purchase = await PurchaseAsync(client, classId, studentId, tooDear.Id);
        Assert.Equal(HttpStatusCode.PaymentRequired, purchase.StatusCode);

        // No row was written: wallet unchanged and the item is still buyable.
        var balance = await GetBalanceAsync(client, classId, studentId);
        Assert.Equal(5, balance.Wallet);
        var storeAfter = await GetStoreAsync(client, classId, studentId);
        Assert.Contains(storeAfter.Items, i => i.Id == tooDear.Id);
    }

    [Fact]
    public async Task A_negative_wallet_from_deductions_can_afford_nothing()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Red class");
        var studentId = await AddStudentAsync(client, classId, "Dan");
        // Deductions may drive the wallet negative (design.md §2.5); a purchase is the one blocked op.
        await GivePointsAsync(client, classId, studentId, -20);

        var store = await GetStoreAsync(client, classId, studentId);
        Assert.Equal(-20, store.Wallet);
        Assert.All(store.Items, i => Assert.False(i.Affordable));

        var cheapest = store.Items.OrderBy(i => i.Cost).First();
        var purchase = await PurchaseAsync(client, classId, studentId, cheapest.Id);
        Assert.Equal(HttpStatusCode.PaymentRequired, purchase.StatusCode);
    }

    [Fact]
    public async Task Buying_the_same_item_twice_is_rejected_by_the_unique_constraint()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Double-buy class");
        var studentId = await AddStudentAsync(client, classId, "Eve");
        await GivePointsAsync(client, classId, studentId, 200);

        var store = await GetStoreAsync(client, classId, studentId);
        var item = store.Items.OrderBy(i => i.Cost).First();

        var first = await PurchaseAsync(client, classId, studentId, item.Id);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await PurchaseAsync(client, classId, studentId, item.Id);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        // Charged exactly once.
        var balance = await GetBalanceAsync(client, classId, studentId);
        Assert.Equal(200 - item.Cost, balance.Wallet);
    }

    [Fact]
    public async Task Buying_a_nonexistent_item_is_not_found()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classId = await CreateClassAsync(client, "Ghost item class");
        var studentId = await AddStudentAsync(client, classId, "Finn");
        await GivePointsAsync(client, classId, studentId, 50);

        var purchase = await PurchaseAsync(client, classId, studentId, Guid.NewGuid());
        Assert.Equal(HttpStatusCode.NotFound, purchase.StatusCode);
    }

    [Fact]
    public async Task Concurrent_purchases_cannot_drive_the_wallet_negative()
    {
        // Two authenticated clients (separate cookie jars) so the two requests truly race.
        var setup = CreateClient();
        await LoginAsSeededTeacherAsync(setup);
        var classId = await CreateClassAsync(setup, "Race class");
        var studentId = await AddStudentAsync(setup, classId, "Gwen");

        var store = await GetStoreAsync(setup, classId, studentId);
        // Two distinct items: the unique constraint can't catch this race (different items), so only
        // the serializable txn + re-sum keeps the wallet from going negative.
        var two = store.Items.OrderBy(i => i.Cost).Take(2).ToList();
        Assert.Equal(2, two.Count);
        var first = two[0];
        var second = two[1];

        // Fund exactly one of them — enough for the cheaper, never both together.
        var wallet = Math.Max(first.Cost, second.Cost);
        await GivePointsAsync(setup, classId, studentId, wallet);

        var clientA = CreateClient();
        var clientB = CreateClient();
        await LoginAsSeededTeacherAsync(clientA);
        await LoginAsSeededTeacherAsync(clientB);

        var results = await Task.WhenAll(
            PurchaseAsync(clientA, classId, studentId, first.Id),
            PurchaseAsync(clientB, classId, studentId, second.Id));

        // Exactly one succeeds; the loser is rejected for insufficient funds after its re-sum.
        var statuses = results.Select(r => r.StatusCode).ToList();
        Assert.Equal(1, statuses.Count(s => s == HttpStatusCode.OK));
        Assert.Equal(1, statuses.Count(s => s == HttpStatusCode.PaymentRequired));

        // The headline invariant: the wallet never went below zero.
        var balance = await GetBalanceAsync(setup, classId, studentId);
        Assert.True(balance.Wallet >= 0, $"Wallet went negative: {balance.Wallet}");

        // And exactly one item was bought (the other is still in the store).
        var storeAfter = await GetStoreAsync(setup, classId, studentId);
        var remaining = new[] { first.Id, second.Id }.Count(id => storeAfter.Items.Any(i => i.Id == id));
        Assert.Equal(1, remaining);
    }

    [Fact]
    public async Task A_non_member_cannot_view_the_store_or_purchase()
    {
        var ownerClient = CreateClient();
        await LoginAsSeededTeacherAsync(ownerClient);
        var classId = await CreateClassAsync(ownerClient, "Private store");
        var studentId = await AddStudentAsync(ownerClient, classId, "Hana");
        await GivePointsAsync(ownerClient, classId, studentId, 100);
        var store = await GetStoreAsync(ownerClient, classId, studentId);
        var item = store.Items.OrderBy(i => i.Cost).First();

        var outsiderEmail = UniqueEmail();
        await CreateTeacherAsync(outsiderEmail);
        var outsider = CreateClient();
        await LoginAsync(outsider, outsiderEmail, "Passw0rd!");

        var read = await outsider.GetAsync($"/api/classes/{classId}/students/{studentId}/store");
        Assert.Equal(HttpStatusCode.Forbidden, read.StatusCode);

        var purchase = await PurchaseAsync(outsider, classId, studentId, item.Id);
        Assert.Equal(HttpStatusCode.Forbidden, purchase.StatusCode);
    }

    [Fact]
    public async Task Store_for_a_student_in_another_class_is_not_found()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);
        var classA = await CreateClassAsync(client, "Class A");
        var classB = await CreateClassAsync(client, "Class B");
        var studentInB = await AddStudentAsync(client, classB, "Outsider");

        var read = await client.GetAsync($"/api/classes/{classA}/students/{studentInB}/store");
        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
    }

    [Fact]
    public async Task Store_endpoints_require_authentication()
    {
        var client = CreateClient();

        var store = await client.GetAsync(
            $"/api/classes/{Guid.NewGuid()}/students/{Guid.NewGuid()}/store");
        Assert.Equal(HttpStatusCode.Unauthorized, store.StatusCode);
    }
}
