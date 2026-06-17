using Classroom.Domain.Students;
using FluentValidation;

namespace Classroom.Api.Features.Roster;

public record AddStudentRequest(string DisplayName, Gender? Gender);

/// <summary>Raw pasted text: one student per line, either <c>Name</c> or <c>Name, F/M</c>.</summary>
public record BulkAddStudentsRequest(string Text);

public record StudentResponse(Guid Id, Guid ClassId, string DisplayName, Gender? Gender, DateTime CreatedAt);

public class AddStudentRequestValidator : AbstractValidator<AddStudentRequest>
{
    public AddStudentRequestValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Gender).IsInEnum().When(x => x.Gender.HasValue);
    }
}

public class BulkAddStudentsRequestValidator : AbstractValidator<BulkAddStudentsRequest>
{
    public BulkAddStudentsRequestValidator()
    {
        RuleFor(x => x.Text).NotEmpty();
    }
}
