using StackExchange.Redis;

namespace AppForeach.TokenHandler.Services;

internal sealed class RedisSessionIndexStore : ISessionIndexStore
{
    // Include the instance prefix used by the distributed cache so all keys share the same namespace.
    internal const string SessionIndexKey = "TokenHandler_token-handler:sessions";

    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public RedisSessionIndexStore(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    public async Task AddAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var db = _connectionMultiplexer.GetDatabase();
        await db.SetAddAsync(SessionIndexKey, sessionId);
    }

    public async Task RemoveAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var db = _connectionMultiplexer.GetDatabase();
        await db.SetRemoveAsync(SessionIndexKey, sessionId);
    }

    public async Task<IReadOnlyCollection<string>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var db = _connectionMultiplexer.GetDatabase();
        var members = await db.SetMembersAsync(SessionIndexKey);
        return members.Select(m => (string)m!).ToHashSet(StringComparer.Ordinal);
    }
}
