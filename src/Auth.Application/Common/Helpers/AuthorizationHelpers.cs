using OpenIddict.Abstractions;
using System.Security.Claims;

namespace Auth.Application.Common.Helpers
{
    public static class AuthorizationHelpers
    {
        public static IEnumerable<string> GetDestinations(Claim claim)
        {
            switch (claim.Type)
            {
                case OpenIddictConstants.Claims.Email:
                    yield return OpenIddictConstants.Destinations.IdentityToken;
                    break;

                case OpenIddictConstants.Claims.Role:
                    yield return OpenIddictConstants.Destinations.AccessToken;
                    yield return OpenIddictConstants.Destinations.IdentityToken;
                    break;

                default: 
                    yield return OpenIddictConstants.Destinations.AccessToken;
                    break;
            }
        }
    }
}
