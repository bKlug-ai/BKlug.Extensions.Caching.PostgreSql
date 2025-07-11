using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Caching.Distributed;

/// <summary>
/// Extension methods for <see cref="IDistributedCache"/> that provide additional functionality
/// similar to what's available in <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/>.
/// </summary>
public static class PostgreSqlDistributedCacheExtensions
{
    /// <summary>
    /// Gets a value with the given key from the cache and deserializes it to the specified type.
    /// </summary>
    /// <typeparam name="TItem">The type of the object to get.</typeparam>
    /// <param name="cache">The <see cref="IDistributedCache"/> instance this method extends.</param>
    /// <param name="key">The key of the value to get.</param>
    /// <returns>The deserialized value associated with this key, or <c>default(TItem)</c> if the key is not present.</returns>
    public static TItem? Get<TItem>(this IDistributedCache cache, string key)
    {
        byte[]? cachedValue = cache.Get(key);
        if (cachedValue == null)
        {
            return default;
        }

        return JsonSerializer.Deserialize<TItem>(cachedValue);
    }

    /// <summary>
    /// Asynchronously gets a value with the given key from the cache and deserializes it to the specified type.
    /// </summary>
    /// <typeparam name="TItem">The type of the object to get.</typeparam>
    /// <param name="cache">The <see cref="IDistributedCache"/> instance this method extends.</param>
    /// <param name="key">The key of the value to get.</param>
    /// <param name="token">Optional. A <see cref="CancellationToken"/> to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the deserialized value associated with this key, or <c>default(TItem)</c> if the key is not present.</returns>
    public static async Task<TItem?> GetAsync<TItem>(this IDistributedCache cache, string key, CancellationToken token = default)
    {
        byte[]? cachedValue = await cache.GetAsync(key, token).ConfigureAwait(false);
        if (cachedValue == null)
        {
            return default;
        }

        return JsonSerializer.Deserialize<TItem>(cachedValue);
    }

    /// <summary>
    /// Try to get a value with the given key from the cache and deserialize it to the specified type.
    /// </summary>
    /// <typeparam name="TItem">The type of the object to get.</typeparam>
    /// <param name="cache">The <see cref="IDistributedCache"/> instance this method extends.</param>
    /// <param name="key">The key of the value to get.</param>
    /// <param name="value">When this method returns, contains the deserialized value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter.</param>
    /// <returns><c>true</c> if the cache contains an element with the specified key and it could be deserialized to the specified type; otherwise, <c>false</c>.</returns>
    public static bool TryGetValue<TItem>(this IDistributedCache cache, string key, out TItem? value)
    {
        byte[]? cachedValue = cache.Get(key);
        if (cachedValue == null)
        {
            value = default;
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<TItem>(cachedValue);
            return true;
        }
        catch
        {
            value = default;
            return false;
        }
    }

    /// <summary>
    /// Asynchronously tries to get a value with the given key from the cache and deserialize it to the specified type.
    /// </summary>
    /// <typeparam name="TItem">The type of the object to get.</typeparam>
    /// <param name="cache">The <see cref="IDistributedCache"/> instance this method extends.</param>
    /// <param name="key">The key of the value to get.</param>
    /// <param name="token">Optional. A <see cref="CancellationToken"/> to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a tuple with a boolean indicating whether the operation was successful and the deserialized value.</returns>
    public static async Task<(bool Success, TItem? Value)> TryGetValueAsync<TItem>(this IDistributedCache cache, string key, CancellationToken token = default)
    {
        byte[]? cachedValue = await cache.GetAsync(key, token).ConfigureAwait(false);
        if (cachedValue == null)
        {
            return (false, default);
        }

        try
        {
            var value = JsonSerializer.Deserialize<TItem>(cachedValue);
            return (true, value);
        }
        catch
        {
            return (false, default);
        }
    }

    /// <summary>
    /// Sets a serialized value with the given key in the cache.
    /// </summary>
    /// <typeparam name="TItem">The type of the object to set.</typeparam>
    /// <param name="cache">The <see cref="IDistributedCache"/> instance this method extends.</param>
    /// <param name="key">The key of the entry to set.</param>
    /// <param name="value">The value to serialize and associate with the key.</param>
    /// <returns>The value that was set.</returns>
    public static TItem Set<TItem>(this IDistributedCache cache, string key, TItem value)
    {
        byte[] serializedValue = JsonSerializer.SerializeToUtf8Bytes(value);
        cache.Set(key, serializedValue, new DistributedCacheEntryOptions());
        return value;
    }

    /// <summary>
    /// Sets a serialized value with the given key in the cache with an absolute expiration time.
    /// </summary>
    /// <typeparam name="TItem">The type of the object to set.</typeparam>
    /// <param name="cache">The <see cref="IDistributedCache"/> instance this method extends.</param>
    /// <param name="key">The key of the entry to set.</param>
    /// <param name="value">The value to serialize and associate with the key.</param>
    /// <param name="absoluteExpiration">The absolute expiration date for the cache entry.</param>
    /// <returns>The value that was set.</returns>
    public static TItem Set<TItem>(this IDistributedCache cache, string key, TItem value, DateTimeOffset absoluteExpiration)
    {
        byte[] serializedValue = JsonSerializer.SerializeToUtf8Bytes(value);
        cache.Set(key, serializedValue, new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = absoluteExpiration
        });
        return value;
    }

    /// <summary>
    /// Sets a serialized value with the given key in the cache with a relative expiration time from now.
    /// </summary>
    /// <typeparam name="TItem">The type of the object to set.</typeparam>
    /// <param name="cache">The <see cref="IDistributedCache"/> instance this method extends.</param>
    /// <param name="key">The key of the entry to set.</param>
    /// <param name="value">The value to serialize and associate with the key.</param>
    /// <param name="absoluteExpirationRelativeToNow">The relative expiration time from now.</param>
    /// <returns>The value that was set.</returns>
    public static TItem Set<TItem>(this IDistributedCache cache, string key, TItem value, TimeSpan absoluteExpirationRelativeToNow)
    {
        byte[] serializedValue = JsonSerializer.SerializeToUtf8Bytes(value);
        cache.Set(key, serializedValue, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow
        });
        return value;
    }

    /// <summary>
    /// Sets a serialized value with the given key in the cache with the given cache options.
    /// </summary>
    /// <typeparam name="TItem">The type of the object to set.</typeparam>
    /// <param name="cache">The <see cref="IDistributedCache"/> instance this method extends.</param>
    /// <param name="key">The key of the entry to set.</param>
    /// <param name="value">The value to serialize and associate with the key.</param>
    /// <param name="options">The cache options for the entry.</param>
    /// <returns>The value that was set.</returns>
    public static TItem Set<TItem>(this IDistributedCache cache, string key, TItem value, DistributedCacheEntryOptions options)
    {
        byte[] serializedValue = JsonSerializer.SerializeToUtf8Bytes(value);
        cache.Set(key, serializedValue, options);
        return value;
    }

    /// <summary>
    /// Asynchronously sets a serialized value with the given key in the cache.
    /// </summary>
    /// <typeparam name="TItem">The type of the object to set.</typeparam>
    /// <param name="cache">The <see cref="IDistributedCache"/> instance this method extends.</param>
    /// <param name="key">The key of the entry to set.</param>
    /// <param name="value">The value to serialize and associate with the key.</param>
    /// <param name="token">Optional. A <see cref="CancellationToken"/> to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous set operation. The task result contains the value that was set.</returns>
    public static async Task<TItem> SetAsync<TItem>(this IDistributedCache cache, string key, TItem value, CancellationToken token = default)
    {
        byte[] serializedValue = JsonSerializer.SerializeToUtf8Bytes(value);
        await cache.SetAsync(key, serializedValue, new DistributedCacheEntryOptions(), token).ConfigureAwait(false);
        return value;
    }

    /// <summary>
    /// Asynchronously sets a serialized value with the given key in the cache with an absolute expiration time.
    /// </summary>
    /// <typeparam name="TItem">The type of the object to set.</typeparam>
    /// <param name="cache">The <see cref="IDistributedCache"/> instance this method extends.</param>
    /// <param name="key">The key of the entry to set.</param>
    /// <param name="value">The value to serialize and associate with the key.</param>
    /// <param name="absoluteExpiration">The absolute expiration date for the cache entry.</param>
    /// <param name="token">Optional. A <see cref="CancellationToken"/> to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous set operation. The task result contains the value that was set.</returns>
    public static async Task<TItem> SetAsync<TItem>(this IDistributedCache cache, string key, TItem value, DateTimeOffset absoluteExpiration, CancellationToken token = default)
    {
        byte[] serializedValue = JsonSerializer.SerializeToUtf8Bytes(value);
        await cache.SetAsync(key, serializedValue, new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = absoluteExpiration
        }, token).ConfigureAwait(false);
        return value;
    }

    /// <summary>
    /// Asynchronously sets a serialized value with the given key in the cache with a relative expiration time from now.
    /// </summary>
    /// <typeparam name="TItem">The type of the object to set.</typeparam>
    /// <param name="cache">The <see cref="IDistributedCache"/> instance this method extends.</param>
    /// <param name="key">The key of the entry to set.</param>
    /// <param name="value">The value to serialize and associate with the key.</param>
    /// <param name="absoluteExpirationRelativeToNow">The relative expiration time from now.</param>
    /// <param name="token">Optional. A <see cref="CancellationToken"/> to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous set operation. The task result contains the value that was set.</returns>
    public static async Task<TItem> SetAsync<TItem>(this IDistributedCache cache, string key, TItem value, TimeSpan absoluteExpirationRelativeToNow, CancellationToken token = default)
    {
        byte[] serializedValue = JsonSerializer.SerializeToUtf8Bytes(value);
        await cache.SetAsync(key, serializedValue, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow
        }, token).ConfigureAwait(false);
        return value;
    }

    /// <summary>
    /// Asynchronously sets a serialized value with the given key in the cache with the given cache options.
    /// </summary>
    /// <typeparam name="TItem">The type of the object to set.</typeparam>
    /// <param name="cache">The <see cref="IDistributedCache"/> instance this method extends.</param>
    /// <param name="key">The key of the entry to set.</param>
    /// <param name="value">The value to serialize and associate with the key.</param>
    /// <param name="options">The cache options for the entry.</param>
    /// <param name="token">Optional. A <see cref="CancellationToken"/> to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous set operation. The task result contains the value that was set.</returns>
    public static async Task<TItem> SetAsync<TItem>(this IDistributedCache cache, string key, TItem value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        byte[] serializedValue = JsonSerializer.SerializeToUtf8Bytes(value);
        await cache.SetAsync(key, serializedValue, options, token).ConfigureAwait(false);
        return value;
    }

    /// <summary>
    /// Gets a value from the cache or creates it if it doesn't exist.
    /// </summary>
    /// <typeparam name="TItem">The type of the object to get or create.</typeparam>
    /// <param name="cache">The <see cref="IDistributedCache"/> instance this method extends.</param>
    /// <param name="key">The key of the entry to get or create.</param>
    /// <param name="factory">The factory function used to create the value if it doesn't exist in the cache.</param>
    /// <param name="options">Optional. The cache options for the entry if it needs to be created.</param>
    /// <returns>The value from the cache or the newly created value.</returns>
    public static TItem GetOrCreate<TItem>(this IDistributedCache cache, string key, Func<TItem> factory, DistributedCacheEntryOptions? options = null)
    {
        if (cache.TryGetValue<TItem>(key, out TItem? value) && value is not null)
        {
            return value;
        }

        value = factory();

        if (options is null)
        {
            cache.Set(key, JsonSerializer.SerializeToUtf8Bytes(value));
        }
        else
        {
            cache.Set(key, JsonSerializer.SerializeToUtf8Bytes(value), options);
        }

        return value;
    }

    /// <summary>
    /// Asynchronously gets a value from the cache or creates it if it doesn't exist.
    /// </summary>
    /// <typeparam name="TItem">The type of the object to get or create.</typeparam>
    /// <param name="cache">The <see cref="IDistributedCache"/> instance this method extends.</param>
    /// <param name="key">The key of the entry to get or create.</param>
    /// <param name="factory">The asynchronous factory function used to create the value if it doesn't exist in the cache.</param>
    /// <param name="options">Optional. The cache options for the entry if it needs to be created.</param>
    /// <param name="token">Optional. A <see cref="CancellationToken"/> to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the value from the cache or the newly created value.</returns>
    public static async Task<TItem> GetOrCreateAsync<TItem>(this IDistributedCache cache, string key, Func<Task<TItem>> factory, DistributedCacheEntryOptions? options = null, CancellationToken token = default)
    {
        var result = await cache.TryGetValueAsync<TItem>(key, token).ConfigureAwait(false);
        if (result.Success && result.Value is not null)
        {
            return result.Value;
        }

        var value = await factory().ConfigureAwait(false);

        byte[] serializedValue = JsonSerializer.SerializeToUtf8Bytes(value);
        if (options is null)
        {
            await cache.SetAsync(key, serializedValue, token).ConfigureAwait(false);
        }
        else
        {
            await cache.SetAsync(key, serializedValue, options, token).ConfigureAwait(false);
        }

        return value;
    }
}
