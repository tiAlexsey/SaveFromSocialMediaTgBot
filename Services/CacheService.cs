using System.Text.Json;
using Abstract.Data.Constants;
using Abstract.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Services;

public class CacheService(IDistributedCache cache, ILogger<CacheService> logger) : ICacheService
{
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> fetchFunction, CancellationToken ct = default)
    {
        var cached = await GetAsync<T>(key, ct);
        if (cached != null)
        {
            return cached;
        }

        var result = await fetchFunction();

        if (result is null)
        {
            throw new NullReferenceException($"{MessageConstants.ErrorEmptyFetchFunc}: {fetchFunction.Method.Name}");
        }

        await SetAsync(key, result, ct);
        return result;
    }

    public async Task SetAsync<T>(string key, T value, CancellationToken ct = default)
    {
        try
        {
            var serialized = JsonSerializer.Serialize(value, _jsonOptions);
            await cache.SetStringAsync(key, serialized, new DistributedCacheEntryOptions(), token: ct);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, ex.Message);
            throw new InvalidOperationException($"{MessageConstants.ErrorSerializeValue} '{key}'", ex);
        }
    }

    private async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        try
        {
            var serialized = await cache.GetStringAsync(key, ct);
            return string.IsNullOrWhiteSpace(serialized)
                ? default
                : JsonSerializer.Deserialize<T>(serialized, _jsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, ex.Message);
            throw new InvalidOperationException($"{MessageConstants.ErrorDeserializeValue} '{key}'", ex);
        }
    }

    private async Task RemoveAsync(string key)
    {
        await cache.RemoveAsync(key);
    }
}