using Auth.Application.Authorization.Commands;
using FluentValidation;

namespace Auth.Application.Authorization
{
    public class AuthorizationCommandValidator : AbstractValidator<AuthorizationCommand>
    {
        public AuthorizationCommandValidator()
        {
            RuleFor(c => c.OpenIddictRequest.ClientId)
                .NotEmpty().WithMessage("Client ID must be provided.").NotNull().WithMessage("Client ID cannot be empty.");
            RuleFor(c => c.OpenIddictRequest.RedirectUri)
                .NotEmpty().WithMessage("Redirect URI must be provided.").NotNull().WithMessage("RedirectURI cannot be empty.");
        }
    }
}
