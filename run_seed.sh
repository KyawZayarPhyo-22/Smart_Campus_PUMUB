#!/bin/bash
# ===============================================================================
# SMART CAMPUS PUMUB - SEED SCRIPT RUNNER (macOS / Linux / Docker)
# ===============================================================================

set -e

# Default Database Connection Configurations
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-1433}"
DB_USER="${DB_USER:-sa}"
DB_PASSWORD="${DB_PASSWORD:-Linn@81220015228}"
DB_NAME="${DB_NAME:-SmartCampusDb}"
DOCKER_CONTAINER="${DOCKER_CONTAINER:-mssql_server}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SQL_FILE="$SCRIPT_DIR/seed_master_data.sql"

echo "=========================================================="
echo "  Smart Campus PUMUB - Database Seeder Runner"
echo "=========================================================="
echo "Target Host:     $DB_HOST:$DB_PORT"
echo "Database:        $DB_NAME"
echo "SQL File:        $SQL_FILE"
echo "=========================================================="

if [ ! -f "$SQL_FILE" ]; then
    echo "Error: SQL file not found at $SQL_FILE"
    exit 1
fi

# Method 1: Try running via Docker container if running
if docker ps --format '{{.Names}}' | grep -q "^${DOCKER_CONTAINER}$"; then
    echo "Detected running Docker container '${DOCKER_CONTAINER}'. Running seed via Docker..."
    docker exec -i "$DOCKER_CONTAINER" /opt/mssql-tools18/bin/sqlcmd -S localhost -U "$DB_USER" -P "$DB_PASSWORD" -C -d "$DB_NAME" < "$SQL_FILE"
    echo ""
    echo "Seed execution via Docker completed successfully!"
    exit 0
fi

# Method 2: Try running via local sqlcmd tool
if command -v sqlcmd &> /dev/null; then
    echo "Running seed via local sqlcmd..."
    sqlcmd -S "$DB_HOST,$DB_PORT" -U "$DB_USER" -P "$DB_PASSWORD" -d "$DB_NAME" -C -i "$SQL_FILE"
    echo ""
    echo "Seed execution via local sqlcmd completed successfully!"
    exit 0
fi

# Method 3: If sqlcmd is in standard Homebrew or MSSQL paths on Mac
MSSQL_PATHS=(
    "/opt/homebrew/bin/sqlcmd"
    "/usr/local/bin/sqlcmd"
    "/opt/mssql-tools18/bin/sqlcmd"
    "/opt/mssql-tools/bin/sqlcmd"
    "$HOME/.dotnet/tools/sqlcmd"
)

for p in "${MSSQL_PATHS[@]}"; do
    if [ -f "$p" ]; then
        echo "Running seed via $p..."
        "$p" -S "$DB_HOST,$DB_PORT" -U "$DB_USER" -P "$DB_PASSWORD" -d "$DB_NAME" -C -i "$SQL_FILE"
        echo ""
        echo "Seed execution completed successfully!"
        exit 0
    fi
done

echo ""
echo "Notice: Neither running Docker container '${DOCKER_CONTAINER}' nor local 'sqlcmd' CLI tool was found."
echo "You can execute the SQL seed file manually using:"
echo "1. SQL Server Management Studio (SSMS) or Azure Data Studio -> Open 'seed_master_data.sql' and click Execute."
echo "2. VS Code MSSQL Extension -> Open 'seed_master_data.sql' and Run Query."
echo "3. Docker Command:"
echo "   docker exec -i mssql_server /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P '$DB_PASSWORD' -C -d SmartCampusDb < \"$SQL_FILE\""
echo "=========================================================="
