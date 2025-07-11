namespace BKlug.Extensions.Caching.PostgreSql;

internal static class Columns
{
    public static class Names
    {
        public const string CacheItemId = "id";
        public const string CacheItemValue = "value";
        public const string ExpiresAtTime = "expires_at_time";
        public const string SlidingExpirationInSeconds = "sliding_expiration_seconds";
        public const string AbsoluteExpiration = "absolute_expiration";
    }
}
