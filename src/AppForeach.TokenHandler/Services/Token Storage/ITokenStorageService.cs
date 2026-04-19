using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace AppForeach.TokenHandler.Services;

public interface ITokenStorageService
{
    Task StoreAsync(string sessionId, OpenIdConnectMessage tokenResponse, CancellationToken cancellationToken = default);
    ValueTask<OpenIdConnectMessage?> GetAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<string>> GetSessionIdsAsync(CancellationToken cancellationToken = default);
    Task RemoveAsync(string sessionId, CancellationToken cancellationToken = default);
}
