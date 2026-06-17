using Classroom.Domain.Classes;
using FluentValidation;

namespace Classroom.Api.Features.Classes;

public record CreateClassRequest(string Name);

public record AddTeacherRequest(string Email, ClassRole? Role);

/// <summary>A class as seen by the current teacher; <see cref="Role"/> is their own role in it.</summary>
public record ClassResponse(Guid Id, string Name, DateTime CreatedAt, bool IsArchived, ClassRole Role);

public class CreateClassRequestValidator : AbstractValidator<CreateClassRequest>
{
    public CreateClassRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public class AddTeacherRequestValidator : AbstractValidator<AddTeacherRequest>
{
    public AddTeacherRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Role).IsInEnum().When(x => x.Role.HasValue);
    }
}
