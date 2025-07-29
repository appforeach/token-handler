using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppForeach.TokenHandler.Controllers;
[ApiController]
[Route("[controller]")]
public class TokenHandlerController : ControllerBase
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
