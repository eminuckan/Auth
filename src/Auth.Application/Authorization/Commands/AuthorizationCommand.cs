using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using Auth.Domain.Entities;
using Auth.Application.Common.Helpers;


namespace Auth.Application.Authorization.Commands
{
    public record AuthorizationCommand(OpenIddictRequest OpenIddictRequest, HttpContext HttpContext) : IRequest<ErrorOr<ClaimsPrincipal>>;
    public class AuthorizationCommandHandler : IRequestHandler<AuthorizationCommand, ErrorOr<ClaimsPrincipal>>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IOpenIddictApplicationManager _applicationManager;

        public AuthorizationCommandHandler(UserManager<AppUser> userManager, IOpenIddictApplicationManager applicationManager)
        {
            _userManager = userManager;
            _applicationManager = applicationManager;
        }
        public async Task<ErrorOr<ClaimsPrincipal>> Handle(AuthorizationCommand request, CancellationToken cancellationToken)
        {
            var openIdRequest = request.OpenIddictRequest;
            var httpContext = request.HttpContext;
            var redirectUri = openIdRequest.RedirectUri!;
            var clientId = openIdRequest.ClientId!;

            var client = await _applicationManager.FindByClientIdAsync(clientId, cancellationToken);

            if (client is null)
            {
                return Error.NotFound("Client not found.");
            }

            var redirectUris = await _applicationManager.GetRedirectUrisAsync(client, cancellationToken);

            if (!redirectUris.Contains(redirectUri))
            {
                // check erroror docs for integer type parameter
                return Error.Custom(1, OpenIddictConstants.Errors.InvalidClient, "The specified redirect_uri is not valid for this client.");
            }

            var authResult = await httpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
            if (!authResult.Succeeded)
            {
                return Error.Custom(1, "UserNotAuthenticated", "User is not authenticated.");
            }

            var user = await _userManager.GetUserAsync(authResult.Principal);
            if (user is null)
            {
                return Error.NotFound("User not found.");
            }

            var userClaims = await _userManager.GetClaimsAsync(user);
            var allowedClients = userClaims.Where(c => c.Type == "AllowedClient").Select(c => c.Value).ToList();
            if (!allowedClients.Contains(clientId))
            {
                return Error.Custom(1, OpenIddictConstants.Errors.AccessDenied, "User is not authorized for this client.");
            }

            var claims = new List<Claim>
            {
                new(OpenIddictConstants.Claims.Subject, await _userManager.GetUserIdAsync(user)),
                new(OpenIddictConstants.Claims.Email, await _userManager.GetEmailAsync(user) ?? string.Empty)
            };

            var roles = await _userManager.GetRolesAsync(user);
            claims.AddRange(roles.Select(r => new Claim(OpenIddictConstants.Claims.Role, r)));

            var identity = new ClaimsIdentity(claims, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            var claimsPrincipal = new ClaimsPrincipal(identity);

            claimsPrincipal.SetDestinations(AuthorizationHelpers.GetDestinations);

            var scopes = openIdRequest.GetScopes();
            claimsPrincipal.SetScopes(scopes);

            return claimsPrincipal;
        }
    }
}
