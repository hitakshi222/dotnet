#!/bin/sh
set -eu

: "${DB_HOST:?DB_HOST must be set}"
: "${DB_PORT:?DB_PORT must be set}"
: "${DB_NAME:?DB_NAME must be set}"
: "${DB_USER:?DB_USER must be set}"
: "${DB_PASSWORD:?DB_PASSWORD must be set}"

export PGPASSWORD="$DB_PASSWORD"
export PGSSLMODE="${DB_SSL_MODE:-require}"

until pg_isready -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" >/dev/null 2>&1; do
  sleep 2
done

schema_exists="$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -tAc \
  "SELECT to_regclass('public.universities') IS NOT NULL")"

if [ "$schema_exists" != "t" ]; then
  psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" \
    --set ON_ERROR_STOP=1 --single-transaction --file=/app/database/schema.sql
  psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" \
    --set ON_ERROR_STOP=1 --single-transaction --file=/app/database/seed.sql
fi

exec dotnet api.dll
