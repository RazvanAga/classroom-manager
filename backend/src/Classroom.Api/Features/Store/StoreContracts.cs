using Classroom.Domain.Avatars;
using FluentValidation;

namespace Classroom.Api.Features.Store;

/// <summary>
/// One buyable catalog option in the store, with the student's <see cref="Affordable"/> flag computed
/// against their current wallet. Mirrors <c>AvatarItemResponse</c> but adds affordability so the UI can
/// disable what the student can't yet afford. Already-owned options are excluded from the listing.
/// </summary>
public record StoreItemResponse(
    Guid Id,
    AvatarSlot Slot,
    string OptionValue,
    string DisplayName,
    int Cost,
    AvatarRarity? Rarity,
    bool Affordable);

/// <summary>
/// The store as seen by one student: their spendable wallet, the locked DiceBear style, and the
/// buyable items (the global catalog minus what they already own).
/// </summary>
public record StoreResponse(
    Guid StudentId,
    int Wallet,
    string Style,
    IReadOnlyList<StoreItemResponse> Items);

/// <summary>Buy one catalog option for a student. The cost is the catalog's, never client-supplied.</summary>
public record PurchaseRequest(Guid ItemId);

/// <summary>The outcome of a successful purchase: the item now owned and the resulting wallet.</summary>
public record PurchaseResponse(Guid StudentId, Guid ItemId, int Cost, int Wallet);

public class PurchaseRequestValidator : AbstractValidator<PurchaseRequest>
{
    public PurchaseRequestValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty().WithMessage("An item id is required.");
    }
}
