using System.Text.Json;
using FinBot.Bll.Interfaces.Integration;
using Microsoft.Extensions.Caching.Distributed;

namespace FinBot.Integrations.Cache;

public class CacheStorage(IDistributedCache cache) : ICacheStorage
{
    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await cache.GetStringAsync(key);
        return value == null 
            ? default 
            : JsonSerializer.Deserialize<T>(value);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiration)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        };

        await cache.SetStringAsync(key, JsonSerializer.Serialize(value), options);
    }

    public async Task RemoveAsync(string key)
    {
        await cache.RemoveAsync(key);
    }
}