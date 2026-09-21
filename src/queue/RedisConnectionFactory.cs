using StackExchange.Redis;

namespace queue;

public sealed class RedisConnectionFactory : IDisposable
{
    private readonly Lazy<ConnectionMultiplexer> _connection;

    public RedisConnectionFactory(QueueOptions options)
    {
        _connection = new Lazy<ConnectionMultiplexer>(() =>
            ConnectionMultiplexer.Connect(options.ConnectionString));
    }

    public IDatabase GetDatabase() => _connection.Value.GetDatabase();

    public void Dispose()
    {
        if (_connection.IsValueCreated)
        {
            _connection.Value.Dispose();
        }
    }
}