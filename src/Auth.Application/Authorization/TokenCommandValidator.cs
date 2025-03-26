using Auth.Application.Authorization.Commands;
using FluentValidation;
using OpenIddict.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auth.Application.Authorization
{
    public class TokenCommandValidator : AbstractValidator<TokenCommand>
    {
        public TokenCommandValidator()
        {
            RuleFor(c => c.OpenIddictRequest.ClientId)
                .NotEmpty().WithMessage("Client ID must be provided.")
                .NotNull().WithMessage("Client ID cannot be empty.");
            RuleFor(c => c.OpenIddictRequest.RedirectUri)
                .NotEmpty().WithMessage("Redirect URI must be provided.")
                .NotNull().WithMessage("RedirectURI cannot be empty.");
            RuleFor(c => c.OpenIddictRequest.GrantType)
                .NotEmpty().WithMessage("Grant type must be provided.")
                .NotNull().WithMessage("Grant type cannot be empty.");
            RuleFor(c => c.OpenIddictRequest)
                .Must(c => c.IsAuthorizationCodeGrantType() || c.IsRefreshTokenGrantType())
                .WithMessage("The specified grant type is not supported.");
        }
    }
}
