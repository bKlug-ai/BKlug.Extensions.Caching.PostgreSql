using System;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace BKlug.Extensions.Caching.PostgreSql;

/// <summary>
/// Handles all database operations for the PostgreSQL distributed cache.
/// </summary>
internal sealed class DatabaseOperations : IDatabaseOperations
{
    private readonly ILogger<DatabaseOperations>? _logger;
    private readonly bool _updateOnGetCacheItem;
    private readonly bool _readOnlyMode;
    private readonly int _commandTimeout;

    public DatabaseOperations(IOptions<PostgreSqlCacheOptions> options, ILogger<DatabaseOperations>? logger = null)
    {
        var cacheOptions = options.Value;

        if (string.IsNullOrEmpty(cacheOptions.ConnectionString) && cacheOptions.DataSourceFactory is null)
        {
            throw new ArgumentException(
                $"Either {nameof(PostgreSqlCacheOptions.ConnectionString)} or {nameof(PostgreSqlCacheOptions.DataSourceFactory)} must be set.");
        }
        if (string.IsNullOrEmpty(cacheOptions.SchemaName))
        {
            throw new ArgumentException(
                $"{nameof(PostgreSqlCacheOptions.SchemaName)} cannot be empty or null.");
        }
        if (string.IsNullOrEmpty(cacheOptions.TableName))
        {
            throw new ArgumentException(
                $"{nameof(PostgreSqlCacheOptions.TableName)} cannot be empty or null.");
        }

        // Use optimized connection factory with pooling settings
        ConnectionFactory = cacheOptions.DataSourceFactory != null
            ? () => cacheOptions.DataSourceFactory.Invoke().CreateConnection()
            : new Func<NpgsqlConnection>(() => new NpgsqlConnection(cacheOptions.GetConnectionStringWithPooling() ?? cacheOptions.ConnectionString));

        _commandTimeout = cacheOptions.CommandTimeout;
        SystemClock = cacheOptions.SystemClock;

        SqlCommands = new SqlCommands(cacheOptions.SchemaName, cacheOptions.TableName, cacheOptions.CronSchedule);

        _logger = logger;
        _updateOnGetCacheItem = cacheOptions.UpdateOnGetCacheItem;
        _readOnlyMode = cacheOptions.ReadOnlyMode;
        // CreateInfrastructureAsync will be called from the service registration if InitializeSchema is true
    }

    private SqlCommands SqlCommands { get; }

    private Func<NpgsqlConnection> ConnectionFactory { get; }

    private ISystemClock SystemClock { get; }

    /// <summary>
    /// Creates all the necessary infrastructure (schema, table, function, cron job) in the database.
    /// </summary>
    /// <param name="token">Cancellation token.</param>
    public async Task CreateInfrastructureAsync(CancellationToken token = default)
    {
        if (_readOnlyMode)
        {
            _logger?.LogDebug("CreateInfrastructureAsync skipped due to ReadOnlyMode");
            return;
        }

        await using var connection = ConnectionFactory();
        await connection.OpenAsync(token);

        // Create schema and table with indexes
        _logger?.LogDebug("Creating schema and table");
        await using (var transaction = await connection.BeginTransactionAsync(token))
        {
            var createSchemaAndTable = new CommandDefinition(
                SqlCommands.CreateSchemaAndTableSql,
                transaction: transaction,
                cancellationToken: token,
                commandTimeout: _commandTimeout);
            await connection.ExecuteAsync(createSchemaAndTable);

            await transaction.CommitAsync(token);
        }

        // Create the delete function
        _logger?.LogDebug("Creating delete function");
        await using (var transaction = await connection.BeginTransactionAsync(token))
        {
            var createFunction = new CommandDefinition(
                SqlCommands.CreateDeleteFunctionSql,
                transaction: transaction,
                cancellationToken: token,
                commandTimeout: _commandTimeout);
            await connection.ExecuteAsync(createFunction);

            await transaction.CommitAsync(token);
        }

        // Set up the cron job
        _logger?.LogDebug("Setting up cron job");
        await using (var transaction = await connection.BeginTransactionAsync(token))
        {
            try
            {
                var setupCron = new CommandDefinition(
                    SqlCommands.ScheduleCronJobSql,
                    transaction: transaction,
                    cancellationToken: token,
                    commandTimeout: _commandTimeout);
                await connection.ExecuteAsync(setupCron);

                await transaction.CommitAsync(token);
                _logger?.LogInformation("PostgreSQL cache cron job scheduled successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to set up PostgreSQL cache cron job. Make sure the pg_cron extension is installed on your database.");
                await transaction.RollbackAsync(token);
            }
        }

        _logger?.LogInformation("Cache infrastructure setup complete");
    }

    public void DeleteCacheItem(string key)
    {
        if (_readOnlyMode)
        {
            _logger?.LogDebug("DeleteCacheItem skipped due to ReadOnlyMode");
            return;
        }

        using var connection = ConnectionFactory();
        connection.Open();

        var deleteCacheItem = new CommandDefinition(
            SqlCommands.DeleteCacheItemSql,
            new ItemIdOnly { Id = key },
            commandTimeout: _commandTimeout);
        connection.Execute(deleteCacheItem);

        _logger?.LogDebug($"Cache key deleted: {key}");
    }

    public async Task DeleteCacheItemAsync(string key, CancellationToken cancellationToken)
    {
        if (_readOnlyMode)
        {
            _logger?.LogDebug("DeleteCacheItem skipped due to ReadOnlyMode");
            return;
        }
        await using var connection = ConnectionFactory();
        await connection.OpenAsync(cancellationToken);

        var deleteCacheItem = new CommandDefinition(
            SqlCommands.DeleteCacheItemSql,
            new ItemIdOnly { Id = key },
            cancellationToken: cancellationToken,
            commandTimeout: _commandTimeout);
        await connection.ExecuteAsync(deleteCacheItem);

        _logger?.LogDebug($"Cache key deleted: {key}");
    }

    public byte[] GetCacheItem(string key) =>
        GetCacheItem(key, includeValue: true);

    public async Task<byte[]> GetCacheItemAsync(string key, CancellationToken cancellationToken) =>
        await GetCacheItemAsync(key, includeValue: true, cancellationToken);

    public void RefreshCacheItem(string key) =>
        GetCacheItem(key, includeValue: false);

    public async Task RefreshCacheItemAsync(string key, CancellationToken cancellationToken) =>
        await GetCacheItemAsync(key, includeValue: false, cancellationToken);

    public void SetCacheItem(string key, ArraySegment<byte> value, DistributedCacheEntryOptions options)
    {
        if (_readOnlyMode)
            return;

        var utcNow = SystemClock.UtcNow;

        var absoluteExpiration = GetAbsoluteExpiration(utcNow, options);
        ValidateOptions(options.SlidingExpiration, absoluteExpiration);

        using var connection = ConnectionFactory();

        var expiresAtTime = options.SlidingExpiration == null
            ? absoluteExpiration!.Value
            : utcNow.Add(options.SlidingExpiration.Value);

        // Use the ArraySegment directly with custom Npgsql parameter handling
        var param = new NpgsqlParameter("@Value", NpgsqlTypes.NpgsqlDbType.Bytea);
        if (value.Array != null)
        {
            // Avoid unnecessary array copy by using the segment directly
            param.Value = value;
        }
        else
        {
            param.Value = DBNull.Value;
        }

        var parameters = new
        {
            Id = key,
            ExpiresAtTime = expiresAtTime,
            SlidingExpirationInSeconds = options.SlidingExpiration?.TotalSeconds,
            AbsoluteExpiration = absoluteExpiration
        };

        // Execute with custom parameter handling
        using var cmd = new NpgsqlCommand(SqlCommands.SetCacheSql, connection);
        cmd.Parameters.AddWithValue("@Id", parameters.Id);
        cmd.Parameters.Add(param);
        cmd.Parameters.AddWithValue("@ExpiresAtTime", parameters.ExpiresAtTime);
        cmd.Parameters.AddWithValue("@SlidingExpirationInSeconds", parameters.SlidingExpirationInSeconds ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@AbsoluteExpiration", parameters.AbsoluteExpiration ?? (object)DBNull.Value);

        connection.Open();
        cmd.ExecuteNonQuery();
    }

    public async Task SetCacheItemAsync(string key, ArraySegment<byte> value, DistributedCacheEntryOptions options, CancellationToken cancellationToken)
    {
        if (_readOnlyMode)
            return;

        var utcNow = SystemClock.UtcNow;

        var absoluteExpiration = GetAbsoluteExpiration(utcNow, options);
        ValidateOptions(options.SlidingExpiration, absoluteExpiration);

        await using var connection = ConnectionFactory();

        var expiresAtTime = options.SlidingExpiration == null
            ? absoluteExpiration!.Value
            : utcNow.Add(options.SlidingExpiration.Value);

        // Use the ArraySegment directly with custom Npgsql parameter handling
        var param = new NpgsqlParameter("@Value", NpgsqlTypes.NpgsqlDbType.Bytea);
        if (value.Array != null)
        {
            // Avoid unnecessary array copy by using the segment directly
            param.Value = value;
        }
        else
        {
            param.Value = DBNull.Value;
        }

        var parameters = new
        {
            Id = key,
            ExpiresAtTime = expiresAtTime,
            SlidingExpirationInSeconds = options.SlidingExpiration?.TotalSeconds,
            AbsoluteExpiration = absoluteExpiration
        };

        // Execute with custom parameter handling
        await using var cmd = new NpgsqlCommand(SqlCommands.SetCacheSql, connection);
        cmd.Parameters.AddWithValue("@Id", parameters.Id);
        cmd.Parameters.Add(param);
        cmd.Parameters.AddWithValue("@ExpiresAtTime", parameters.ExpiresAtTime);
        cmd.Parameters.AddWithValue("@SlidingExpirationInSeconds", parameters.SlidingExpirationInSeconds ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@AbsoluteExpiration", parameters.AbsoluteExpiration ?? (object)DBNull.Value);

        await connection.OpenAsync(cancellationToken);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private byte[] GetCacheItem(string key, bool includeValue)
    {
        var utcNow = SystemClock.UtcNow;
        byte[] value = null;

        using var connection = ConnectionFactory();
        connection.Open();

        if (!_readOnlyMode && (_updateOnGetCacheItem || !includeValue))
        {
            var updateCacheItem = new CommandDefinition(
                SqlCommands.UpdateCacheItemSql,
                new ItemIdUtcNow { Id = key, UtcNow = utcNow },
                commandTimeout: _commandTimeout);
            connection.Execute(updateCacheItem);
        }

        if (includeValue)
        {
            var getCacheItem = new CommandDefinition(
                SqlCommands.GetCacheItemSql,
                new ItemIdUtcNow { Id = key, UtcNow = utcNow },
                commandTimeout: _commandTimeout);
            value = connection.QueryFirstOrDefault<byte[]>(getCacheItem);
        }

        return value;
    }

    private async Task<byte[]> GetCacheItemAsync(string key, bool includeValue, CancellationToken cancellationToken)
    {
        var utcNow = SystemClock.UtcNow;
        byte[] value = null;

        await using var connection = ConnectionFactory();
        await connection.OpenAsync(cancellationToken);

        if (!_readOnlyMode && (_updateOnGetCacheItem || !includeValue))
        {
            var updateCacheItem = new CommandDefinition(
                SqlCommands.UpdateCacheItemSql,
                new ItemIdUtcNow { Id = key, UtcNow = utcNow },
                cancellationToken: cancellationToken,
                commandTimeout: _commandTimeout);
            await connection.ExecuteAsync(updateCacheItem);
        }

        if (includeValue)
        {
            var getCacheItem = new CommandDefinition(
                SqlCommands.GetCacheItemSql,
                new ItemIdUtcNow { Id = key, UtcNow = utcNow },
                cancellationToken: cancellationToken,
                commandTimeout: _commandTimeout);
            value = await connection.QueryFirstOrDefaultAsync<byte[]>(getCacheItem);
        }

        return value;
    }

    private DateTimeOffset? GetAbsoluteExpiration(DateTimeOffset utcNow, DistributedCacheEntryOptions options)
    {
        // calculate absolute expiration
        DateTimeOffset? absoluteExpiration = null;
        if (options.AbsoluteExpirationRelativeToNow.HasValue)
        {
            absoluteExpiration = utcNow.Add(options.AbsoluteExpirationRelativeToNow.Value);
        }
        else if (options.AbsoluteExpiration.HasValue)
        {
            if (options.AbsoluteExpiration.Value.ToUniversalTime() <= utcNow.ToUniversalTime())
            {
                throw new InvalidOperationException("The absolute expiration value must be in the future.");
            }

            absoluteExpiration = options.AbsoluteExpiration.Value;
        }
        return absoluteExpiration;
    }

    private void ValidateOptions(TimeSpan? slidingExpiration, DateTimeOffset? absoluteExpiration)
    {
        if (!slidingExpiration.HasValue && !absoluteExpiration.HasValue)
        {
            throw new InvalidOperationException("Either absolute or sliding expiration needs to be provided.");
        }
    }
}
