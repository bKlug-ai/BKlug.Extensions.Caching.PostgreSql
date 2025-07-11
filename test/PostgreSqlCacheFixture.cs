using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace BKlug.Extensions.Caching.PostgreSql.Tests
{
    /// <summary>
    /// Fixture para criar o banco de dados de teste e configurar o cache distribuído para os testes.
    /// </summary>
    public class PostgreSqlCacheFixture : IDisposable
    {
        public string ConnectionString { get; }
        public string TableName { get; }
        public string SchemaName { get; } = "cache";
        public IDistributedCache Cache { get; }

        public PostgreSqlCacheFixture()
        {
            // Permitir configuração via variáveis de ambiente
            var dbName = Environment.GetEnvironmentVariable("PGCACHE_TEST_DB") ?? "cache_test";
            var user = Environment.GetEnvironmentVariable("PGCACHE_TEST_USER") ?? "postgres";
            var pass = Environment.GetEnvironmentVariable("PGCACHE_TEST_PASS") ?? "postgres";
            var host = Environment.GetEnvironmentVariable("PGCACHE_TEST_HOST") ?? "localhost";
            var port = Environment.GetEnvironmentVariable("PGCACHE_TEST_PORT") ?? "5432";
            ConnectionString = $"Host={host};Port={port};Database={dbName};Username={user};Password={pass}";
            TableName = Environment.GetEnvironmentVariable("PGCACHE_TEST_TABLE") ?? "cache_items_test";

            EnsureDatabaseExists(dbName, host, port, user, pass);

            // Configurar os serviços de cache para os testes
            var services = new ServiceCollection();
            services.AddDistributedPostgreSqlCache(options =>
            {
                options.ConnectionString = ConnectionString;
                options.SchemaName = SchemaName;
                options.TableName = TableName;
                options.InitializeSchema = true; // DatabaseOperations já cuidará da criação da infraestrutura
            });

            var provider = services.BuildServiceProvider();

            Cache = provider.GetRequiredService<IDistributedCache>();

            // Limpar dados antigos, caso existam
            CleanupOldData().GetAwaiter().GetResult();
        }

        private void EnsureDatabaseExists(string dbName, string host, string port, string user, string pass)
        {
            // Conecta no banco padrão (postgres) e cria o banco de teste se não existir
            var adminConnStr = $"Host={host};Port={port};Database=postgres;Username={user};Password={pass}";
            using var conn = new NpgsqlConnection(adminConnStr);
            conn.Open();
            using var cmd = new NpgsqlCommand($"SELECT 1 FROM pg_database WHERE datname = '{dbName}'", conn);
            var exists = cmd.ExecuteScalar();
            if (exists == null)
            {
                using var createCmd = new NpgsqlCommand($"CREATE DATABASE \"{dbName}\"", conn);
                createCmd.ExecuteNonQuery();
            }
        }

        private async Task CleanupOldData()
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand($@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = '{SchemaName}' AND tablename = '{TableName}') THEN
                        EXECUTE 'TRUNCATE {SchemaName}.{TableName}';
                    END IF;
                END $$;
            ", conn);
            await cmd.ExecuteNonQueryAsync();
        }

        public void Dispose()
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand($"DROP TABLE IF EXISTS {SchemaName}.{TableName}; DROP SCHEMA IF EXISTS {SchemaName} CASCADE;", conn);
            cmd.ExecuteNonQuery();
        }
    }
}
