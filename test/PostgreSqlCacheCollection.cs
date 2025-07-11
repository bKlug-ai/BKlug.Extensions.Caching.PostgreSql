using Xunit;

namespace BKlug.Extensions.Caching.PostgreSql.Tests
{
    [CollectionDefinition("PostgreSqlCache collection")]
    public class PostgreSqlCacheCollection : ICollectionFixture<PostgreSqlCacheFixture>
    {
        // Intentionally left blank. This class only serves as collection definition for xUnit.
    }
}
