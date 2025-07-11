# BKlug.Extensions.Caching.PostgreSql

[![NuGet](https://img.shields.io/nuget/v/BKlug.Extensions.Caching.PostgreSql.svg)](https://www.nuget.org/packages/BKlug.Extensions.Caching.PostgreSql)
[![Build & Test](https://github.com/bKlug-ai/BKlug.Extensions.Caching.PostgreSql/actions/workflows/ci.yml/badge.svg)](https://github.com/bKlug-ai/BKlug.Extensions.Caching.PostgreSql/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![NuGet Downloads](https://img.shields.io/nuget/dt/BKlug.Extensions.Caching.PostgreSql.svg)](https://www.nuget.org/packages/BKlug.Extensions.Caching.PostgreSql)

High-performance distributed cache for .NET using PostgreSQL with UNLOGGED tables and [pg_cron](https://github.com/citusdata/pg_cron).

---

## What is Distributed Caching?
A distributed cache is a cache shared by multiple app servers, typically maintained as an external service. It improves performance and scalability for ASP.NET Core apps, especially in cloud or server farm environments. Distributed caches:
- Keep data consistent across servers
- Survive server restarts and deployments
- Do not use local memory

ASP.NET Core provides the `IDistributedCache` interface, which this library implements for PostgreSQL.

## Why PostgreSQL?
- Ideal for teams already using PostgreSQL as infrastructure
- Provides high-performance caching without depending on Redis or SQL Server
- Uses UNLOGGED tables for maximum speed
- Automatic cleanup of expired items via pg_cron (no .NET background thread)
- Support for connection pooling, robustness, and customization

## Features
- Full `IDistributedCache` implementation
- PostgreSQL UNLOGGED tables for performance
- Expiration and cleanup via pg_cron
- Connection pooling and robust error handling
- Customizable schema, table, and cron schedule
- Ready for cloud-native and microservices

## Installation
```shell
dotnet add package BKlug.Extensions.Caching.PostgreSql
```

## Usage
### 1. Service Registration

#### Simple configuration
```csharp
services.AddDistributedPostgreSqlCache(options =>
{
    options.ConnectionString = "Host=localhost;Database=cache;Username=postgres;Password=yourpassword";
});
```

#### Advanced configuration
```csharp
services.AddDistributedPostgreSqlCache(options =>
{
    options.ConnectionString = "Host=localhost;Database=cache;Username=postgres;Password=yourpassword";
    options.SchemaName = "cache";
    options.TableName = "cache_items";
    options.InitializeSchema = true;
    options.CronSchedule = "*/5 * * * *"; // every 5 minutes
    options.MinPoolSize = 2;
    options.MaxPoolSize = 50;
    options.ConnectionLifetime = 600;
    options.CommandTimeout = 60;
    options.DefaultSlidingExpiration = TimeSpan.FromMinutes(30);
    options.UpdateOnGetCacheItem = false;
    options.ReadOnlyMode = false;
});
```

### 2. Using the Standard IDistributedCache Interface

```csharp
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

public class StandardCacheSample
{
    private readonly IDistributedCache _cache;
    
    public StandardCacheSample(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task BasicExampleAsync()
    {
        // Store a string directly
        await _cache.SetStringAsync("greeting", "Hello World", new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        });
        
        // Retrieve the string
        string greeting = await _cache.GetStringAsync("greeting");
        
        // Store binary data
        byte[] binaryData = new byte[] { 1, 2, 3, 4, 5 };
        await _cache.SetAsync("binary-data", binaryData);
        
        // Retrieve binary data
        byte[]? retrievedData = await _cache.GetAsync("binary-data");
    }
    
    public async Task CacheObjectAsync()
    {
        var user = new User { Id = 42, Name = "Alice" };
        
        // Manually serialize object
        string json = JsonSerializer.Serialize(user);
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
        
        // Store serialized object
        await _cache.SetAsync("user:42", bytes, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        });
        
        // Retrieve and deserialize
        byte[]? resultBytes = await _cache.GetAsync("user:42");
        if (resultBytes != null)
        {
            string jsonResult = System.Text.Encoding.UTF8.GetString(resultBytes);
            User? retrievedUser = JsonSerializer.Deserialize<User>(jsonResult);
            Console.WriteLine($"Retrieved user: {retrievedUser?.Name}");
        }
        
        // Refresh sliding expiration
        await _cache.RefreshAsync("user:42");
        
        // Remove from cache
        await _cache.RemoveAsync("user:42");
    }
    
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
```

### 3. Using Enhanced IDistributedCache Extensions

The enhanced extensions provide a simpler way to work with typed objects in the cache:

```csharp
using Microsoft.Extensions.Caching.Distributed;

public class EnhancedCacheSample
{
    private readonly IDistributedCache _cache;
    
    public EnhancedCacheSample(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task TypedObjectCachingAsync()
    {
        var user = new User { Id = 42, Name = "Alice", Email = "alice@example.com" };
        
        // Store typed object directly - no manual serialization needed
        await _cache.SetAsync("user:42", user, TimeSpan.FromHours(1));
        
        // Retrieve typed object - no manual deserialization needed
        User? retrievedUser = await _cache.GetAsync<User>("user:42");
        if (retrievedUser != null)
        {
            Console.WriteLine($"User: {retrievedUser.Name}, Email: {retrievedUser.Email}");
        }
        
        // Try to get with pattern matching
        if (await _cache.TryGetValueAsync<User>("user:42") is (true, var cachedUser))
        {
            Console.WriteLine($"Found user: {cachedUser?.Name}");
        }
        
        // Sync versions are also available
        _cache.Set("user:43", new User { Id = 43, Name = "Bob" }, 
            new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(30) });
            
        if (_cache.TryGetValue<User>("user:43", out User? bob))
        {
            Console.WriteLine($"Found Bob: {bob.Name}");
        }
    }
    
    public void GetOrCreateExample()
    {
        // Get cached value or create new one if not exists
        User user = _cache.GetOrCreate<User>("user:44", () => 
        {
            Console.WriteLine("Cache miss - creating new user");
            return new User { Id = 44, Name = "Charlie" };
        });
        
        // With expiration options
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        };
        
        User carol = _cache.GetOrCreate<User>("user:45", () => new User { Id = 45, Name = "Carol" }, options);
    }
    
    public async Task GetOrCreateAsyncExample()
    {
        // Async version with factory function
        User dave = await _cache.GetOrCreateAsync<User>("user:46", async () => 
        {
            await Task.Delay(10); // Simulate async work
            return new User { Id = 46, Name = "Dave" };
        });
        
        // With custom expiration
        User eve = await _cache.GetOrCreateAsync<User>(
            "user:47", 
            async () => 
            {
                await Task.Delay(10);
                return new User { Id = 47, Name = "Eve" };
            },
            new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(15) }
        );
    }
    
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
```

### 4. Practical Examples

```csharp
// Example: Caching API responses
public class WeatherService
{
    private readonly IDistributedCache _cache;
    private readonly HttpClient _httpClient;
    
    public WeatherService(IDistributedCache cache, HttpClient httpClient)
    {
        _cache = cache;
        _httpClient = httpClient;
    }
    
    public async Task<WeatherData> GetWeatherDataAsync(string city)
    {
        string cacheKey = $"weather:{city.ToLower()}";
        
        // Try to get from cache first
        return await _cache.GetOrCreateAsync<WeatherData>(cacheKey, async () =>
        {
            // Cache miss - fetch from API
            var response = await _httpClient.GetAsync($"https://api.example.com/weather?city={city}");
            response.EnsureSuccessStatusCode();
            var weatherData = await response.Content.ReadFromJsonAsync<WeatherData>();
            return weatherData ?? new WeatherData(); 
        }, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        });
    }
}

// Example: Caching database query results
public class ProductRepository
{
    private readonly IDistributedCache _cache;
    private readonly DbContext _dbContext;
    
    public ProductRepository(IDistributedCache cache, DbContext dbContext)
    {
        _cache = cache;
        _dbContext = dbContext;
    }
    
    public async Task<List<Product>> GetFeaturedProductsAsync()
    {
        string cacheKey = "featured-products";
        
        return await _cache.GetOrCreateAsync<List<Product>>(cacheKey, async () =>
        {
            // Expensive database query
            return await _dbContext.Products
                .Where(p => p.IsFeatured)
                .Include(p => p.Category)
                .ToListAsync();
        }, new DistributedCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(10),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
        });
    }
    
    public void InvalidateFeaturedProductsCache()
    {
        _cache.Remove("featured-products");
    }
}
```

### 5. Manual Schema Initialization

If you can't or don't want to use automatic schema initialization (for example, when your application user doesn't have permission to create schemas, tables or use pg_cron), you can set `InitializeSchema = false` and run the following script as a database administrator.

Adjust the schema name, table name, and cron schedule as needed:

```sql
-- Create schema and table
CREATE SCHEMA IF NOT EXISTS cache;
    
CREATE UNLOGGED TABLE IF NOT EXISTS cache.cache_items
(
    id TEXT NOT NULL PRIMARY KEY,
    value BYTEA,
    expires_at_time TIMESTAMPTZ,
    sliding_expiration_seconds DOUBLE PRECISION,
    absolute_expiration TIMESTAMPTZ
)
WITH (
    autovacuum_vacuum_scale_factor = 0.01,
    autovacuum_analyze_scale_factor = 0.005
);

CREATE INDEX IF NOT EXISTS idx_cache_items_expires
    ON cache.cache_items(expires_at_time)
    WHERE expires_at_time IS NOT NULL;

-- Create function to delete expired items
CREATE OR REPLACE FUNCTION cache.delete_expired_cache_items() 
RETURNS void LANGUAGE sql AS $$
    DELETE FROM cache.cache_items
    WHERE expires_at_time <= NOW();
$$;

-- Schedule the cleanup job (requires pg_cron extension)
-- Make sure pg_cron extension is installed first: CREATE EXTENSION IF NOT EXISTS pg_cron;
SELECT cron.schedule(
    'cache_delete_expired',
    '*/1 * * * *',  -- Run every minute (cron format: minute hour day month weekday)
    $$SELECT cache.delete_expired_cache_items()$$
);
```

Then configure your cache service without schema initialization:

```csharp
services.AddDistributedPostgreSqlCache(options =>
{
    options.ConnectionString = "Host=localhost;Database=cache;Username=postgres;Password=yourpassword";
    options.InitializeSchema = false; // Skip schema initialization
});
```

### 6. All Options (`PostgreSqlCacheOptions`)
| Option                   | Type           | Default                | Description |
|--------------------------|----------------|------------------------|-------------|
| ConnectionString         | string         | -                      | PostgreSQL connection string |
| DataSourceFactory        | Func<NpgsqlDataSource> | -            | Custom data source factory |
| SchemaName               | string         | "cache"               | Schema name |
| TableName                | string         | "cache_items"         | Table name |
| InitializeSchema         | bool           | true                   | Auto-create schema/table |
| CronSchedule             | string         | "*/1 * * * *"          | pg_cron schedule |
| MinPoolSize              | int            | 1                      | Min pool size |
| MaxPoolSize              | int            | 100                    | Max pool size |
| ConnectionLifetime       | int            | 300                    | Pool connection lifetime (s) |
| CommandTimeout           | int            | 30                     | Command timeout (s) |
| DefaultSlidingExpiration | TimeSpan       | 20 min                 | Default sliding expiration |
| UpdateOnGetCacheItem     | bool           | true                   | Refresh sliding expiration on get |
| ReadOnlyMode             | bool           | false                  | Read-only mode |

## Requirements
- PostgreSQL 13+ (15+ recommended for native pg_cron)
- [pg_cron](https://github.com/citusdata/pg_cron) extension enabled (the test script ensures this)

## Integration with ASP.NET Core
This library plugs directly into the ASP.NET Core dependency injection system. You can use it for:
- Session state
- Output caching
- Any custom caching scenario

All standard `IDistributedCache` methods are supported:
- `Get`, `GetAsync`
- `Set`, `SetAsync`
- `Refresh`, `RefreshAsync`
- `Remove`, `RemoveAsync`

### Enhanced Extensions
This library includes enhanced extension methods for `IDistributedCache` that provide a more convenient API:

| Method | Description |
|--------|-------------|
| `Get<T>(key)` | Gets a cached value and deserializes to type T |
| `GetAsync<T>(key)` | Asynchronously gets a cached value and deserializes to type T |
| `TryGetValue<T>(key, out T value)` | Tries to get a cached value and deserialize to type T |
| `TryGetValueAsync<T>(key)` | Asynchronously tries to get a cached value and deserialize to type T |
| `Set<T>(key, value)` | Serializes and caches a value of type T |
| `Set<T>(key, value, expiration)` | Serializes and caches a value with expiration |
| `SetAsync<T>(key, value)` | Asynchronously serializes and caches a value |
| `GetOrCreate<T>(key, factory)` | Gets a cached value or creates using factory function |
| `GetOrCreateAsync<T>(key, factory)` | Asynchronously gets a cached value or creates using async factory |

## Recommendations
- Use a dedicated PostgreSQL database for cache if possible, to avoid impact on business data
- Benchmark your application: for extremely high-performance workloads, consider Redis, but for many scenarios PostgreSQL is sufficient and more practical
- Always configure pg_cron to ensure automatic cleanup of expired items
- Use UNLOGGED tables for maximum performance, but be aware that data may be lost in a server crash

## License
MIT

## Contributing
See [CONTRIBUTING.md](CONTRIBUTING.md)

## For Contributors and Maintainers

### Development Workflow
1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Run the tests (`dotnet test`)
5. Commit your changes (`git commit -m 'Add some amazing feature'`)
6. Push to the branch (`git push origin feature/amazing-feature`)
7. Open a Pull Request

### Publishing to NuGet
This package uses GitHub Actions for continuous integration and delivery:

1. **Automatic Builds**: Every push to `main` branch triggers a build and test run.
2. **Version Updates**: To release a new version:
   - Update the `Version` in `src/BKlug.Extensions.Caching.PostgreSql.csproj`
   - Update `CHANGELOG.md` with details of changes
   - Create and push a new tag with the version number (e.g., `v1.0.1`)
   - The GitHub Action will automatically create a draft release with the packages
   - Review the draft release and publish it
   - After publishing, the package will be automatically pushed to NuGet

### Debugging Symbols
Symbol packages (.snupkg) are published alongside the main package to enable better debugging experience for consumers of this library.

## Security
See [SECURITY.md](SECURITY.md)

---

> **References:**
> - [Distributed caching in ASP.NET Core (Microsoft Docs)](https://learn.microsoft.com/aspnet/core/performance/caching/distributed)
> - [IDistributedCache interface](https://learn.microsoft.com/dotnet/api/microsoft.extensions.caching.distributed.idistributedcache)
> - [pg_cron for PostgreSQL](https://github.com/citusdata/pg_cron)

## Inspiration

This project was inspired by the following similar projects:
- [Microsoft.Extensions.Caching.SqlServer](https://www.nuget.org/packages/Microsoft.Extensions.Caching.SqlServer) - Official Microsoft SQL Server distributed cache implementation
- [Extensions.Caching.PostgreSQL](https://github.com/wullemsb/Extensions.Caching.PostgreSQL)
- [community-extensions-cache-postgres](https://github.com/leonibr/community-extensions-cache-postgres)

These projects provided valuable insights and approaches that informed the design and implementation of this library.
