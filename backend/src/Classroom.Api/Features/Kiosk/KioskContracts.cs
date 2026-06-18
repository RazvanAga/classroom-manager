using Classroom.Domain.Identity;
using FluentValidation;

namespace Classroom.Api.Features.Kiosk;

/// <summary>Body for entering kiosk mode: the class the kiosk session is scoped to.</summary>
public record EnterKioskRequest(Guid ClassId);

/// <summary>Body for exiting kiosk mode (and for setting the PIN): the numeric PIN.</summary>
public record KioskPinRequest(string Pin);

/// <summary>The current kiosk session, for the kiosk UI's header.</summary>
public record KioskSessionResponse(Guid ClassId, string ClassName);

public class EnterKioskRequestValidator : AbstractValidator<EnterKioskRequest>
{
    public EnterKioskRequestValidator()
    {
        RuleFor(x => x.ClassId).NotEmpty();
    }
}

public class KioskPinRequestValidator : AbstractValidator<KioskPinRequest>
{
    public KioskPinRequestValidator()
    {
        RuleFor(x => x.Pin)
            .Must(KioskPinPolicy.IsValidFormat)
            .WithMessage($"The PIN must be {KioskPinPolicy.MinLength}–{KioskPinPolicy.MaxLength} digits.");
    }
}
