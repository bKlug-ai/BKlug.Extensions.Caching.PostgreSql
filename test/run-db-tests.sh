#!/bin/bash
set -e

DB_NAME=${PGCACHE_TEST_DB:-cache_test}
USER=${PGCACHE_TEST_USER:-postgres}
PASS=${PGCACHE_TEST_PASS:-postgres}
DB_HOST=${PGCACHE_TEST_HOST:-localhost}
PORT=${PGCACHE_TEST_PORT:-5432}
TABLE=${PGCACHE_TEST_TABLE:-cache_items_test}

CONTAINER_NAME=pgcache-test

# Start PostgreSQL Docker container
if [ ! $(docker ps -q -f name=$CONTAINER_NAME) ]; then
  echo "Starting PostgreSQL Docker container..."
  docker run --name $CONTAINER_NAME -e POSTGRES_DB=$DB_NAME -e POSTGRES_USER=$USER -e POSTGRES_PASSWORD=$PASS -p $PORT:5432 -d postgres:16
  sleep 10
fi

# Ensure pg_cron is installed (postgres:15+ already includes pg_cron)
docker exec $CONTAINER_NAME psql -U $USER -d $DB_NAME -c "CREATE EXTENSION IF NOT EXISTS pg_cron;"

export PGCACHE_TEST_DB=$DB_NAME
export PGCACHE_TEST_USER=$USER
export PGCACHE_TEST_PASS=$PASS
export PGCACHE_TEST_HOST=$DB_HOST
export PGCACHE_TEST_PORT=$PORT
export PGCACHE_TEST_TABLE=$TABLE

# Run tests
echo "Running tests..."
dotnet test ../BKlug.Extensions.Caching.PostgreSql.Tests.csproj

# Clean up
echo "Removing Docker container..."
docker rm -f $CONTAINER_NAME
