using StackExchange.Redis;
using System.Threading.Tasks;

namespace iThome2024.SalesService.Service;

public class RedisService
{
    private IDatabase _db;

    public RedisService(string connectionString)
    {
        ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(connectionString);
        _db = redis.GetDatabase();
    }

    public async Task StringSetAsync(string key, string value)
    {
        await _db.StringSetAsync(key, value);
    }

    public async Task<string> StringGetAsync(string key)
    {
        return (await _db.StringGetAsync(key)).ToString();
    }

    public async Task KeyDeleteAsync(string key)
    {
        await _db.KeyDeleteAsync(key);
    }

    public async Task<bool> KeyExistsAsync(string key)
    {
        return await _db.KeyExistsAsync(key);
    }

    public async Task HashSetAsync(string key, string field, string value)
    {
        await _db.HashSetAsync(key, field, value);
    }

    public async Task<string> HashGetAsync(string key, string field)
    {
        return (await _db.HashGetAsync(key, field)).ToString();
    }
    public async Task HashSetAllAsync(string key, Dictionary<string, string> entries)
    {
        var hashEntries = entries.Select(x => new HashEntry(x.Key, x.Value)).ToArray();
        await _db.HashSetAsync(key, hashEntries);
    }
    public async Task<Dictionary<string, string>> HashGetAllAsync(string key)
    {
        var hashEntries = await _db.HashGetAllAsync(key);
        return hashEntries.ToDictionary(
            x => x.Name.ToString(),
            x => x.Value.ToString());
    }

    public async Task SortedSetAddAsync(string key, double score, string member)
    {
        await _db.SortedSetAddAsync(key, member, score);
    }

    public async Task<List<string>> SortedSetRangeByRankAsync(string key, long start = 0, long stop = -1)
    {
        var sortedSetEntries = await _db.SortedSetRangeByRankAsync(key, start, stop);
        return sortedSetEntries.Select(x => x.ToString()).ToList();
    }
    public async Task<bool> SetAddAsync(string key, string value)
    {
        return await _db.SetAddAsync(key, value);
    }

}