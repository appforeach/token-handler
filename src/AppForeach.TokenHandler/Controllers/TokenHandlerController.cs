using AppForeach.TokenHandler.Extensions;
using AppForeach.TokenHandler.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppForeach.TokenHandler.Controllers;

[ApiController]
[Route("[controller]")]
public class TokenHandlerController : ControllerBase
{
    private readonly ITokenStorageService _tokenStorage;

    public TokenHandlerController(ITokenStorageService tokenStorage)
    {
        _tokenStorage = tokenStorage;
    }

    [Route("authorize")]
    public IActionResult Authorize(string returnUrl = "/")
    {
        return Challenge(new AuthenticationProperties { RedirectUri = returnUrl }, "oidc");
    }

    [Route("logout")]
    public async Task<IActionResult> Logout()
    {
        if (HttpContext.Request.Cookies.TryGetValue(ConfigurationExtensions.AuthenticationCookieName, out var sessionId)
            && !string.IsNullOrWhiteSpace(sessionId))
        {
            await _tokenStorage.RemoveAsync(sessionId);
            Response.Cookies.Delete(ConfigurationExtensions.AuthenticationCookieName);
        }

        await HttpContext.SignOutAsync("Cookies");
        await HttpContext.SignOutAsync("oidc", new AuthenticationProperties
        {
            RedirectUri = "/"
        });

        return Redirect("/");
    }
}
