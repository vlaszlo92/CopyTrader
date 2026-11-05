#!/bin/bash
set -euo pipefail

run_sql() {
    /opt/mssql-tools18/bin/sqlcmd -C -b -I -S "${SQL_SERVER:-db}" -U sa -P "${MSSQL_SA_PASSWORD}" "$@"
}

until run_sql -Q "SELECT 1" > /dev/null 2>&1; do
    echo "Waiting for database..."
    sleep 3
done

run_sql -v APP_PASSWORD="${APP_PASSWORD}" -i /db/init.sql

TABLE_COUNT=$(run_sql -d CopyTrader -h -1 -W -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.tables" | tr -d '[:space:]')
if [ "${TABLE_COUNT}" = "0" ]; then
    run_sql -d CopyTrader -i /db/schema.sql
fi

run_sql -d CopyTrader -i /db/seed.sql
echo "Database initialization finished."
