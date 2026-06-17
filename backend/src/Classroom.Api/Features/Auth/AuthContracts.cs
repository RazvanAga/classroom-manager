using FluentValidation;

namespace Classroom.Api.Features.Auth;

public record LoginRequest(string Email, string Password);

public record MeResponse(Guid Id, string Email, string DisplayName);

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}
