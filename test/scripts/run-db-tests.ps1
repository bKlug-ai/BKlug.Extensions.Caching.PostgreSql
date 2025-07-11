param(
    [string]$DbName = "cache_test",
    [string]$User = "postgres",
    [string]$Pass = "postgres",
    [string]$DbHost = "localhost",
    [string]$Port = "5432",
    [string]$Table = "cache_items_test"
)

# Start PostgreSQL Docker container
Write-Host "Starting PostgreSQL Docker container..."
docker run --name pgcache-test -e POSTGRES_DB=$DbName -e POSTGRES_USER=$User -e POSTGRES_PASSWORD=$Pass -p $Port:5432 -d postgres:16

Start-Sleep -Seconds 10

# Ensure pg_cron is installed (postgres:15+ already includes pg_cron)
docker exec pgcache-test psql -U $User -d $DbName -c "CREATE EXTENSION IF NOT EXISTS pg_cron;"

# Export environment variables for test execution
$env:PGCACHE_TEST_DB = $DbName
$env:PGCACHE_TEST_USER = $User
$env:PGCACHE_TEST_PASS = $Pass
$env:PGCACHE_TEST_HOST = $DbHost
$env:PGCACHE_TEST_PORT = $Port
$env:PGCACHE_TEST_TABLE = $Table

# Run tests
# The test project path is relative to this script location
# If you move this script, update the path accordingly
Write-Host "Running tests..."
dotnet test ..\BKlug.Extensions.Caching.PostgreSql.Tests.csproj

# Clean up Docker container
Write-Host "Removing Docker container..."
docker rm -f pgcache-test
