using System;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;
using Npgsql;

namespace BKlug.Extensions.Caching.PostgreSql;

/// <summary>
/// PostgreSql distributed cache options.
/// </summary>
public class PostgreSqlCacheOptions : IOptions<PostgreSqlCacheOptions>
{
    /// <summary>
    /// The factory to create a NpgsqlDataSource instance.
    /// Either <see cref="DataSourceFactory"/> or <see cref="ConnectionString"/> should be set.
    /// </summary>
    public Func<NpgsqlDataSource> DataSourceFactory { get; set; }

    /// <summary>
    /// The connection string to the database.
    /// If <see cref="DataSourceFactory"/> not set, <see cref="ConnectionString"/> would be used to connect to the database.
    /// </summary>
    public string ConnectionString { get; set; }

    /// <summary>
    /// An abstraction to represent the clock of a machine in order to enable unit testing.
    /// </summary>
    public ISystemClock SystemClock { get; set; } = new SystemClock();

    /// <summary>
    /// The schema name of the table.
    /// </summary>
    public string SchemaName { get; set; } = "cache";

    /// <summary>
    /// Name of the table where the cache items are stored.
    /// </summary>
    public string TableName { get; set; } = "cache_items";

    /// <summary>
    /// If set to true, the infrastructure (schema, table, function, and cron job) will be initialized.
    /// </summary>
    public bool InitializeSchema { get; set; } = true;

    /// <summary>
    /// Cron schedule for the pg_cron job that removes expired items.
    /// Default is every minute.
    /// </summary>
    public string CronSchedule { get; set; } = "*/1 * * * *";

    /// <summary>
    /// The minimum connection pool size for better performance under load.
    /// </summary>
    public int MinPoolSize { get; set; } = 1;

    /// <summary>
    /// The maximum connection pool size to prevent resource exhaustion.
    /// </summary>
    public int MaxPoolSize { get; set; } = 100;

    /// <summary>
    /// Connection lifetime in seconds. When a connection is returned to the pool
    /// and its lifetime is exceeded, the connection is destroyed instead of being put in the pool.
    /// This is useful when the database server has a policy of closing connections after a certain time.
    /// </summary>
    public int ConnectionLifetime { get; set; } = 300; // 5 minutes

    /// <summary>
    /// Command timeout in seconds. Sets the time to wait while trying to execute a command
    /// before terminating the attempt and generating an error.
    /// </summary>
    public int CommandTimeout { get; set; } = 30;

    /// <summary>
    /// The default sliding expiration set for a cache entry if neither Absolute or SlidingExpiration has been set explicitly.
    /// By default, its 20 minutes.
    /// </summary>
    public TimeSpan DefaultSlidingExpiration { get; set; } = TimeSpan.FromMinutes(20);

    /// <summary>
    /// If set to false no update of ExpiresAtTime will be performed when getting a cache item (i.e., IDistributedCache.Get or GetAsync)
    /// Default value is true. 
    /// ATTENTION: When is set to false the user of the distributed cache must call the IDistributedCache.Refresh to update slide expiration.
    ///   For example, if you are using ASPNET Core Sessions, ASPNET Core will call IDistributedCache.Refresh for you at the end of the request if 
    ///   needed (i.e., there wasn't any changes to the session but it still needs to be refreshed).
    /// </summary>
    public bool UpdateOnGetCacheItem { get; set; } = true;

    /// <summary>
    /// If set to true, no updates at all will be saved to the database, values will only be read.
    /// ATTENTION: this will disable any sliding expiration as well as cache clean-up.
    /// </summary>
    public bool ReadOnlyMode { get; set; } = false;

    /// <summary>
    /// Gets a valid connection string with proper pooling settings applied
    /// </summary>
    internal string GetConnectionStringWithPooling()
    {
        if (string.IsNullOrEmpty(ConnectionString))
        {
            return null;
        }

        var builder = new NpgsqlConnectionStringBuilder(ConnectionString)
        {
            MinPoolSize = MinPoolSize,
            MaxPoolSize = MaxPoolSize,
            ConnectionLifetime = ConnectionLifetime,
            CommandTimeout = CommandTimeout,
            Pooling = true
        };

        return builder.ToString();
    }

    PostgreSqlCacheOptions IOptions<PostgreSqlCacheOptions>.Value => this;
}
