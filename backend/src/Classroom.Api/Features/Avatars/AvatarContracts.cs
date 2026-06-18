using Classroom.Domain.Avatars;
using FluentValidation;

namespace Classroom.Api.Features.Avatars;

/// <summary>One catalog option: a single value for a single slot. Used by both the catalog and a
/// student's owned-items list. <see cref="OptionValue"/> is the literal DiceBear value to render.</summary>
public record AvatarItemResponse(
    Guid Id,
    AvatarSlot Slot,
    string OptionValue,
    string DisplayName,
    int Cost,
    AvatarRarity? Rarity,
    bool IsDefault);

/// <summary>The whole global catalog, plus the locked DiceBear style the frontend renders with.</summary>
public record AvatarCatalogResponse(string Style, IReadOnlyList<AvatarItemResponse> Items);

/// <summary>What a student has equipped in one slot (the value the frontend composes into the avatar).</summary>
public record EquippedSlotResponse(AvatarSlot Slot, Guid ItemId, string OptionValue);

/// <summary>
/// A student's full avatar state: the locked style, the currently equipped option per slot (the
/// render config), and every option the student owns and may equip for free (design.md §3.3).
/// </summary>
public record StudentAvatarResponse(
    Guid StudentId,
    string Style,
    IReadOnlyList<EquippedSlotResponse> Equipped,
    IReadOnlyList<AvatarItemResponse> Owned);

/// <summary>Equip a slot with an option the student already owns. Equipping is free.</summary>
public record EquipItemRequest(Guid ItemId);

public class EquipItemRequestValidator : AbstractValidator<EquipItemRequest>
{
    public EquipItemRequestValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty().WithMessage("An item id is required.");
    }
}
