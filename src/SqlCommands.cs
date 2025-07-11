namespace BKlug.Extensions.Caching.PostgreSql;

internal class SqlCommands
{
    private readonly string _schemaName;
    private readonly string _tableName;
    private readonly string _cronSchedule;

    public SqlCommands(string schemaName, string tableName, string cronSchedule = "*/1 * * * *")
    {
        _schemaName = schemaName;
        _tableName = tableName;
        _cronSchedule = cronSchedule;
    }

    public string CreateSchemaAndTableSql =>
        $"""
        CREATE SCHEMA IF NOT EXISTS {_schemaName};
    
        CREATE UNLOGGED TABLE IF NOT EXISTS {_schemaName}.{_tableName}
        (
            {Columns.Names.CacheItemId} TEXT NOT NULL PRIMARY KEY,
            {Columns.Names.CacheItemValue} BYTEA,
            {Columns.Names.ExpiresAtTime} TIMESTAMPTZ,
            {Columns.Names.SlidingExpirationInSeconds} DOUBLE PRECISION,
            {Columns.Names.AbsoluteExpiration} TIMESTAMPTZ
        )
        WITH (
            autovacuum_vacuum_scale_factor = 0.01,
            autovacuum_analyze_scale_factor = 0.005
        );
    
        CREATE INDEX IF NOT EXISTS idx_{_tableName}_expires
            ON {_schemaName}.{_tableName}({Columns.Names.ExpiresAtTime})
            WHERE {Columns.Names.ExpiresAtTime} IS NOT NULL;
        """;

    public string GetCacheItemSql =>
        $"""
        SELECT {Columns.Names.CacheItemValue}
        FROM {_schemaName}.{_tableName}
        WHERE {Columns.Names.CacheItemId} = @Id AND ({Columns.Names.ExpiresAtTime} IS NULL OR {Columns.Names.ExpiresAtTime} > NOW())
        """;

    public string SetCacheSql =>
        $"""
        INSERT INTO {_schemaName}.{_tableName} ({Columns.Names.CacheItemId}, {Columns.Names.CacheItemValue}, {Columns.Names.ExpiresAtTime}, {Columns.Names.SlidingExpirationInSeconds}, {Columns.Names.AbsoluteExpiration})
            VALUES (@Id, @Value, @ExpiresAtTime, @SlidingExpirationInSeconds, @AbsoluteExpiration)
        ON CONFLICT({Columns.Names.CacheItemId}) DO
        UPDATE SET
            {Columns.Names.CacheItemValue} = EXCLUDED.{Columns.Names.CacheItemValue},
            {Columns.Names.ExpiresAtTime} = EXCLUDED.{Columns.Names.ExpiresAtTime},
            {Columns.Names.SlidingExpirationInSeconds} = EXCLUDED.{Columns.Names.SlidingExpirationInSeconds},
            {Columns.Names.AbsoluteExpiration} = EXCLUDED.{Columns.Names.AbsoluteExpiration}
        """;

    public string UpdateCacheItemSql =>
        $"""
        UPDATE {_schemaName}.{_tableName}
        SET {Columns.Names.ExpiresAtTime} = LEAST({Columns.Names.AbsoluteExpiration}, NOW() + {Columns.Names.SlidingExpirationInSeconds} * interval '1 second')
        WHERE {Columns.Names.CacheItemId} = @Id
            AND ({Columns.Names.ExpiresAtTime} IS NULL OR NOW() <= {Columns.Names.ExpiresAtTime})
            AND {Columns.Names.SlidingExpirationInSeconds} IS NOT NULL
            AND ({Columns.Names.AbsoluteExpiration} IS NULL OR {Columns.Names.AbsoluteExpiration} <> {Columns.Names.ExpiresAtTime})
        """;

    public string DeleteCacheItemSql =>
        $"""
        DELETE FROM {_schemaName}.{_tableName}
        WHERE {Columns.Names.CacheItemId} = @Id
        """;

    public string DeleteExpiredCacheSql =>
        $"""
        DELETE FROM {_schemaName}.{_tableName}
        WHERE {Columns.Names.ExpiresAtTime} <= NOW()
        """;

    public string CreateDeleteFunctionSql =>
        $"""
        CREATE OR REPLACE FUNCTION {_schemaName}.delete_expired_cache_items() 
        RETURNS void LANGUAGE sql AS $$
            DELETE FROM {_schemaName}.{_tableName}
            WHERE {Columns.Names.ExpiresAtTime} <= NOW();
        $$;
        """;

    public string ScheduleCronJobSql =>
        $"""
        SELECT cron.schedule(
            'cache_delete_expired',
            '{_cronSchedule}',
            $$SELECT {_schemaName}.delete_expired_cache_items()$$
        );
        """;
}