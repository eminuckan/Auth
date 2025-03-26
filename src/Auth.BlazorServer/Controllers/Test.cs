using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth.BlazorServer.Controllers
{
    [Route("api")]
    [ApiController]
    public class Test : ControllerBase
    {
        [HttpGet("test")]
        [Authorize]
        public IActionResult TestAction()
        {
            return Ok("Test");
        }
    }
}
