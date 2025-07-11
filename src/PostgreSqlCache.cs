using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace BKlug.Extensions.Caching.PostgreSql;

/// <summary>
/// Implementation of IDistributedCache using PostgreSQL as backend.
/// </summary>
public class PostgreSqlCache : IDistributedCache
{
    private readonly IDatabaseOperations _dbOperations;
    private readonly TimeSpan _defaultSlidingExpiration;
    private readonly ILogger<PostgreSqlCache>? _logger;

    public PostgreSqlCache(
        IOptions<PostgreSqlCacheOptions> options,
        IDatabaseOperations databaseOperations,
        ILogger<PostgreSqlCache>? logger = null)
    {
        _dbOperations = databaseOperations ?? throw new ArgumentNullException(nameof(databaseOperations));
        _logger = logger;

        var cacheOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));

        if (cacheOptions.DefaultSlidingExpiration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cacheOptions.DefaultSlidingExpiration),
                cacheOptions.DefaultSlidingExpiration,
                "The sliding expiration value must be positive.");
        }

        _defaultSlidingExpiration = cacheOptions.DefaultSlidingExpiration;
    }

    /// <inheritdoc />
    public byte[] Get(string key)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        try
        {
            return _dbOperations.GetCacheItem(key);
        }
        catch (NpgsqlException ex)
        {
            _logger?.LogError(ex, "Error retrieving cache item with key {Key}", key);
            // For connection errors, we return null (like item not found)
            // This prevents cascade failures if PostgreSQL becomes temporarily unavailable
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error retrieving cache item with key {Key}", key);
            throw; // Rethrow unexpected exceptions
        }
    }

    /// <inheritdoc />
    public async Task<byte[]> GetAsync(string key, CancellationToken token)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        try
        {
            return await _dbOperations.GetCacheItemAsync(key, token);
        }
        catch (NpgsqlException ex)
        {
            _logger?.LogError(ex, "Error retrieving cache item with key {Key}", key);
            // For connection errors, we return null (like item not found)
            // This prevents cascade failures if PostgreSQL becomes temporarily unavailable
            return null;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Just rethrow cancellation, this is expected
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error retrieving cache item with key {Key}", key);
            throw; // Rethrow unexpected exceptions
        }
    }

    /// <inheritdoc />
    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        GetOptions(ref options);

        try
        {
            _dbOperations.SetCacheItem(key, new ArraySegment<byte>(value), options);
        }
        catch (NpgsqlException ex)
        {
            _logger?.LogError(ex, "Error setting cache item with key {Key}", key);
            // Silently continue on database errors - cache is non-critical
            // This prevents application failures when database connectivity is lost
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error setting cache item with key {Key}", key);
            throw; // Rethrow unexpected exceptions
        }
    }

    /// <inheritdoc />
    public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        GetOptions(ref options);

        try
        {
            await _dbOperations.SetCacheItemAsync(key, new ArraySegment<byte>(value), options, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Just rethrow cancellation, this is expected
            throw;
        }
        catch (NpgsqlException ex)
        {
            _logger?.LogError(ex, "Error setting cache item with key {Key}", key);
            // Silently continue on database errors - cache is non-critical
            // This prevents application failures when database connectivity is lost
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error setting cache item with key {Key}", key);
            throw; // Rethrow unexpected exceptions
        }
    }

    /// <inheritdoc />
    public void Refresh(string key)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        _dbOperations.RefreshCacheItem(key);
    }

    /// <inheritdoc />
    public async Task RefreshAsync(string key, CancellationToken token)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        await _dbOperations.RefreshCacheItemAsync(key, token);
    }

    /// <inheritdoc />
    public void Remove(string key)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        _dbOperations.DeleteCacheItem(key);
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken token)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        return _dbOperations.DeleteCacheItemAsync(key, token);
    }

    private void GetOptions(ref DistributedCacheEntryOptions options)
    {
        if (!options.AbsoluteExpiration.HasValue
            && !options.AbsoluteExpirationRelativeToNow.HasValue
            && !options.SlidingExpiration.HasValue)
        {
            options = new DistributedCacheEntryOptions()
            {
                SlidingExpiration = _defaultSlidingExpiration
            };
        }
    }
}
