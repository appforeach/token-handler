using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Poc.Yarp.Token_Handler.Controllers;

[ApiController]
[Route("[controller]")]
public class TokenHandlerControllerV2 : ControllerBase
{
    [Route("authorize")]
    public IActionResult Athorize(string returnUrl = "/")
    {
        return Challenge(new AuthenticationProperties { RedirectUri = returnUrl }, "oidc");
    }

    [Route("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("Cookies");
        await HttpContext.SignOutAsync("oidc", new AuthenticationProperties
        {
            RedirectUri = "/"
        });

        return Redirect("/");
    }
}
