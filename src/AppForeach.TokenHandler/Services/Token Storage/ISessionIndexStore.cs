namespace AppForeach.TokenHandler.Services;

public interface ISessionIndexStore
{
    Task AddAsync(string sessionId, CancellationToken cancellationToken = default);
    Task RemoveAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<string>> GetAllAsync(CancellationToken cancellationToken = default);
}
