using Auth.BlazorServer.Common;
using Auth.Application.Authorization.Commands;
using MediatR;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Server.AspNetCore;


// needs refactoring
namespace Auth.BlazorServer.Controllers
{
    [ApiController]
    [Route("connect")]
    public class AuthorizationController : ControllerBase
    {
        private readonly ISender _sender;

        public AuthorizationController(ISender sender) => _sender = sender;

        [HttpGet("authorize")]
        [HttpPost("authorize")]
        public async Task<IActionResult> Authorize()
        {
            var openIddictRequest = HttpContext.GetOpenIddictServerRequest() ?? throw new InvalidOperationException("OpenIddict server request not found.");
            var command = new AuthorizationCommand(openIddictRequest, HttpContext);
            var result = await _sender.Send(command);
            var problemDetails = result.ToProblemDetails();

            if (result.IsError)
            {
                if (result.Errors.Any(e => e.Code == "UserNotAuthenticated"))
                {
                    Dictionary<string, string?> parameters = Request.HasFormContentType
                        ? Request.Form.ToDictionary(v => v.Key, v => (string?)v.Value.ToString())
                        : Request.Query.ToDictionary(v => v.Key, v => (string?)v.Value.ToString());

                    return Challenge(
                       authenticationSchemes: IdentityConstants.ApplicationScheme,
                       properties: new AuthenticationProperties
                       {
                           RedirectUri = Request.PathBase + Request.Path + QueryString.Create(parameters)
                       });
                }
                return StatusCode((int)problemDetails.Status!, problemDetails);
            }

            return SignIn(result.Value, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        [HttpPost("token")]
        public async Task<IActionResult> Token()
        {
            var openIddictRequest = HttpContext.GetOpenIddictServerRequest() ?? throw new InvalidOperationException("OpenIddict server request not found.");
            var command = new TokenCommand(openIddictRequest, HttpContext);
            var result = await _sender.Send(command);
            var problemDetails = result.ToProblemDetails();

            var body = Request.Body;
            if (result.IsError)
            {
                if (result.Errors.Any(e => e.Code == "UserNotAuthenticated"))
                {
                    Dictionary<string, string?> parameters = Request.HasFormContentType
                         ? Request.Form.ToDictionary(v => v.Key, v => (string?)v.Value.ToString())
                         : Request.Query.ToDictionary(v => v.Key, v => (string?)v.Value.ToString());

                    return Challenge(
                       authenticationSchemes: IdentityConstants.ApplicationScheme,
                       properties: new AuthenticationProperties
                       {
                           RedirectUri = Request.PathBase + Request.Path + QueryString.Create(parameters)
                       });
                }
                return StatusCode((int)problemDetails.Status!, problemDetails);
            }
            return SignIn(result.Value, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
    }
}