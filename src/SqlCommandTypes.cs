using System;

namespace BKlug.Extensions.Caching.PostgreSql;

internal record ItemIdUtcNow
{
    public string Id { get; internal set; }

    // Not needed anymore as we use NOW() in SQL directly
    public DateTimeOffset UtcNow { get; internal set; }
};

internal record ItemIdOnly
{
    public string Id { get; internal set; }
}
