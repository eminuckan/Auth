using Auth.Application.Common.Helpers;
using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;

namespace Auth.Application.Authorization.Commands
{
    public record TokenCommand(OpenIddictRequest OpenIddictRequest, HttpContext HttpContext) : IRequest<ErrorOr<ClaimsPrincipal>>;

    public class TokenCommandHandler : IRequestHandler<TokenCommand, ErrorOr<ClaimsPrincipal>>
    {
        private readonly IOpenIddictApplicationManager _applicationManager;

        public TokenCommandHandler(IOpenIddictApplicationManager applicationManager)
        {
            _applicationManager = applicationManager;
        }
        public async Task<ErrorOr<ClaimsPrincipal>> Handle(TokenCommand request, CancellationToken cancellationToken)
        {
            var openIddictRequest = request.OpenIddictRequest;
            var httpContext = request.HttpContext;
            var clientId = openIddictRequest.ClientId!;

            var authResult = await httpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            if (!authResult.Succeeded)
            {
                return Error.Custom(1, OpenIddictConstants.Errors.AccessDenied, "User is not authenticated.");
            }

            var application = await _applicationManager.FindByClientIdAsync(clientId, cancellationToken);

            // ToDo: custom error types for auth errors
            if (application is null)
            {
                return Error.NotFound("Invalid client.");
            }
            
            var claimsPrincipal = authResult.Principal;
            claimsPrincipal.SetDestinations(AuthorizationHelpers.GetDestinations);

            return claimsPrincipal;
        }
    }
}
