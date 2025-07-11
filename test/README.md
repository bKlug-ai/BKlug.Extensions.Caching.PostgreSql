# Tests for BKlug.Extensions.Caching.PostgreSql

This project contains integration tests for the PostgreSQL distributed cache library.

## Prerequisites
- Docker or a local PostgreSQL instance
- .NET 8 SDK or higher
- The [pg_cron](https://github.com/citusdata/pg_cron) extension must be installed in the test database
  - The official `postgres:15` or higher image already includes pg_cron by default

## Configuration
You can customize the test database connection using environment variables:
- `PGCACHE_TEST_DB` (default: cache_test)
- `PGCACHE_TEST_USER` (default: postgres)
- `PGCACHE_TEST_PASS` (default: postgres)
- `PGCACHE_TEST_HOST` (default: localhost)
- `PGCACHE_TEST_PORT` (default: 5432)
- `PGCACHE_TEST_TABLE` (default: cache_items_test)

The test database will be created automatically if it doesn't exist.

## Running the tests
### Windows
```powershell
./run-db-tests.ps1
```
### Linux/macOS
```sh
chmod +x run-db-tests.sh
./run-db-tests.sh
```

## Cleanup
The test fixture removes the table and schema after tests.
The script removes the Docker container when finished.
