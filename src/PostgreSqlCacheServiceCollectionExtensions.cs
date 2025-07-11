/// <summary>
/// BKlug.Extensions.Caching.PostgreSql provides a high-performance distributed cache implementation 
/// using PostgreSQL. It follows the interface patterns of Microsoft.Extensions.Caching.SqlServer
/// but is optimized for PostgreSQL with:
///
/// 1. PostgreSQL-specific UNLOGGED tables for better performance
/// 2. pg_cron for database-side cleanup of expired items (no .NET background thread)
///
/// REQUIREMENTS:
/// - PostgreSQL 12 or higher
/// - pg_cron extension must be installed on the PostgreSQL server
/// - Database user must have permission to create tables, functions, and use pg_cron
/// </summary>

using System;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BKlug.Extensions.Caching.PostgreSql;

/// <summary>
/// Extension methods for setting up PostgreSql distributed cache services in an <see cref="IServiceCollection" />.
/// </summary>
public static class PostgreSqlCacheServiceCollectionExtensions
{
    /// <summary>
    /// Adds Community Microsoft PostgreSql distributed caching services to the specified <see cref="IServiceCollection" />
    /// without configuration. Use an implementation of <see cref="IConfigureOptions{PostgreSqlCacheOptions}"/> for configuration.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddDistributedPostgreSqlCache(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.AddOptions();
        AddPostgreSqlCacheServices(services);

        return services;
    }

    /// <summary>
    /// Adds Community Microsoft PostgreSql distributed caching services to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="setupAction">An <see cref="Action{PostgreSqlCacheOptions}"/> to configure the provided <see cref="PostgreSqlCacheOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddDistributedPostgreSqlCache(this IServiceCollection services, Action<PostgreSqlCacheOptions> setupAction)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (setupAction == null)
        {
            throw new ArgumentNullException(nameof(setupAction));
        }

        services.AddOptions();
        services.Configure(setupAction);
        AddPostgreSqlCacheServices(services);

        return services;
    }

    /// <summary>
    /// Adds Community Microsoft PostgreSql distributed caching services to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="setupAction">An <see cref="Action{IServiceProvider, PostgreSqlCacheOptions}"/> to configure the provided <see cref="PostgreSqlCacheOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddDistributedPostgreSqlCache(this IServiceCollection services, Action<IServiceProvider, PostgreSqlCacheOptions> setupAction)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (setupAction == null)
        {
            throw new ArgumentNullException(nameof(setupAction));
        }

        services.AddOptions();
        services.AddSingleton<IConfigureOptions<PostgreSqlCacheOptions>>(
            sp => new ConfigureOptions<PostgreSqlCacheOptions>(opt => setupAction(sp, opt)));
        AddPostgreSqlCacheServices(services);

        return services;
    }

    private static void AddPostgreSqlCacheServices(IServiceCollection services)
    {
        // Register DatabaseOperations with initialization logic
        services.AddSingleton<IDatabaseOperations>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<PostgreSqlCacheOptions>>();
            var logger = sp.GetService<ILogger<DatabaseOperations>>();
            var databaseOps = new DatabaseOperations(options, logger);

            // Initialize schema, table, function and cron job if configured
            if (options.Value.InitializeSchema)
            {
                try
                {
                    // Execute async code synchronously at startup to ensure schema is ready
                    databaseOps.CreateInfrastructureAsync().GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    // Log but don't fail startup - the app may be able to continue without cache
                    var optionsLogger = sp.GetService<ILogger<PostgreSqlCacheOptions>>();
                    optionsLogger?.LogError(ex, "Failed to initialize PostgreSQL cache infrastructure. Cache operations may fail.");
                }
            }

            return databaseOps;
        });

        services.AddSingleton<IDistributedCache, PostgreSqlCache>();
    }
}
