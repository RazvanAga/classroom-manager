using FluentValidation;

namespace Classroom.Api.Features.Behaviors;

public record CreateBehaviorRequest(string Name, int DefaultPoints);

public record UpdateBehaviorRequest(string Name, int DefaultPoints);

public record BehaviorResponse(Guid Id, Guid ClassId, string Name, int DefaultPoints, DateTime CreatedAt);

public class CreateBehaviorRequestValidator : AbstractValidator<CreateBehaviorRequest>
{
    public CreateBehaviorRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        // DefaultPoints is intentionally unconstrained beyond range — it is signed (rewards/deductions)
        // and zero is harmless; the only hard bound is the int column itself.
    }
}

public class UpdateBehaviorRequestValidator : AbstractValidator<UpdateBehaviorRequest>
{
    public UpdateBehaviorRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
