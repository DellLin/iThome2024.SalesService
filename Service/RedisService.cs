using StackExchange.Redis;

namespace iThome2024.SalesService.Service;

public class RedisService
{
    private IDatabase _db;

    public RedisService(string connectionString)
    {
        ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(connectionString);
        _db = redis.GetDatabase();
    }
    public void StringSet(string key, string value)
    {
        _db.StringSet(key, value);
    }
    public string StringGet(string key)
    {
        return _db.StringGet(key).ToString();
    }
}
