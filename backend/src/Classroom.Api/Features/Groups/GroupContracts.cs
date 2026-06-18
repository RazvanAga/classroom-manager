using Classroom.Domain.Students;
using FluentValidation;

namespace Classroom.Api.Features.Groups;

/// <summary>Form (but don't persist) a grouping. The server returns a seed the client can save to reproduce it.</summary>
public record FormGroupingRequest(int GroupSize, bool BalanceByGender);

/// <summary>
/// Save a previously-formed grouping. Carrying the same <paramref name="Seed"/> the preview returned
/// re-derives the identical arrangement server-side (grouping is deterministic), so the saved result
/// matches what the teacher saw without trusting client-supplied membership.
/// </summary>
public record SaveGroupingRequest(string? Name, int GroupSize, bool BalanceByGender, int Seed);

public record GroupMemberResponse(Guid StudentId, string DisplayName, Gender? Gender);

public record FormedGroupResponse(int GroupNumber, IReadOnlyList<GroupMemberResponse> Members);

/// <summary>An ephemeral, formed-but-unsaved grouping plus the seed needed to save it as-is.</summary>
public record FormedGroupingResponse(
    int GroupSize, bool BalancedByGender, int Seed, IReadOnlyList<FormedGroupResponse> Groups);

/// <summary>A saved grouping with its full membership.</summary>
public record SavedGroupingResponse(
    Guid Id,
    Guid ClassId,
    string? Name,
    int GroupSize,
    bool BalancedByGender,
    DateTime CreatedAt,
    IReadOnlyList<FormedGroupResponse> Groups);

/// <summary>A saved grouping in list form — counts instead of full membership.</summary>
public record GroupingSummaryResponse(
    Guid Id,
    string? Name,
    int GroupSize,
    bool BalancedByGender,
    DateTime CreatedAt,
    int GroupCount,
    int StudentCount);

public class FormGroupingRequestValidator : AbstractValidator<FormGroupingRequest>
{
    public FormGroupingRequestValidator()
    {
        RuleFor(x => x.GroupSize).InclusiveBetween(1, 50);
    }
}

public class SaveGroupingRequestValidator : AbstractValidator<SaveGroupingRequest>
{
    public SaveGroupingRequestValidator()
    {
        RuleFor(x => x.GroupSize).InclusiveBetween(1, 50);
        RuleFor(x => x.Name).MaximumLength(100).When(x => x.Name is not null);
    }
}
