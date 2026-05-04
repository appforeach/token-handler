using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace AppForeach.TokenHandler.Services;

public interface ITokenRefreshService
{
    bool ShouldRefresh(OpenIdConnectMessage tokenResponse, DateTimeOffset now);
    Task<OpenIdConnectMessage?> RefreshAsync(OpenIdConnectMessage tokenResponse, CancellationToken cancellationToken = default);
}
