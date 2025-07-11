using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace BKlug.Extensions.Caching.PostgreSql;

public interface IDatabaseOperations
{
    byte[] GetCacheItem(string key);

    Task<byte[]> GetCacheItemAsync(string key, CancellationToken cancellationToken);

    void RefreshCacheItem(string key);

    Task RefreshCacheItemAsync(string key, CancellationToken cancellationToken);

    void DeleteCacheItem(string key);

    Task DeleteCacheItemAsync(string key, CancellationToken cancellationToken);

    void SetCacheItem(string key, ArraySegment<byte> value, DistributedCacheEntryOptions options);

    Task SetCacheItemAsync(string key, ArraySegment<byte> value, DistributedCacheEntryOptions options, CancellationToken cancellationToken);

    /// <summary>
    /// Creates the database infrastructure including schema, table, deletion function and cron job.
    /// </summary>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CreateInfrastructureAsync(CancellationToken token = default);
}
