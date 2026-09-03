# ===============================================================================
# SMART CAMPUS PUMUB - SEED SCRIPT RUNNER (PowerShell for Windows / macOS / Linux)
# ===============================================================================

param (
    [string]$Server = "localhost,1433",
    [string]$Database = "SmartCampusDb",
    [string]$User = "sa",
    [string]$Password = "Linn@81220015228",
    [string]$DockerContainer = "mssql_server"
)

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$SqlFile = Join-Path $ScriptDir "seed_master_data.sql"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  Smart Campus PUMUB - Database Seeder Runner (PowerShell)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "Server:          $Server"
Write-Host "Database:        $Database"
Write-Host "SQL File:        $SqlFile"
Write-Host "=========================================================="

if (-not (Test-Path $SqlFile)) {
    Write-Error "SQL File not found at: $SqlFile"
    exit 1
}

# 1. Try Docker if container is running
try {
    $dockerRunning = docker ps --format '{{.Names}}' 2>$null | Select-String "^$DockerContainer$"
    if ($dockerRunning) {
        Write-Host "Found active Docker container '$DockerContainer'. Executing seed script via Docker..." -ForegroundColor Green
        Get-Content $SqlFile -Raw | docker exec -i $DockerContainer /opt/mssql-tools18/bin/sqlcmd -S localhost -U $User -P $Password -C -d $Database
        Write-Host ""
        Write-Host "Seeding completed successfully via Docker!" -ForegroundColor Green
        exit 0
    }
} catch {
    # Docker not running or not installed
}

# 2. Try sqlcmd CLI
$sqlcmdCmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
if ($sqlcmdCmd) {
    Write-Host "Running seed via local sqlcmd..." -ForegroundColor Green
    sqlcmd -S $Server -U $User -P $Password -d $Database -C -i $SqlFile
    Write-Host ""
    Write-Host "Seeding completed successfully via sqlcmd!" -ForegroundColor Green
    exit 0
}

# 3. Try Invoke-Sqlcmd (SqlServer Module)
$invokeSqlcmd = Get-Command Invoke-Sqlcmd -ErrorAction SilentlyContinue
if ($invokeSqlcmd) {
    Write-Host "Running seed via Invoke-Sqlcmd..." -ForegroundColor Green
    Invoke-Sqlcmd -ServerInstance $Server -Database $Database -Username $User -Password $Password -InputFile $SqlFile -TrustServerCertificate
    Write-Host ""
    Write-Host "Seeding completed successfully via Invoke-Sqlcmd!" -ForegroundColor Green
    exit 0
}

Write-Host ""
Write-Host "Neither Docker container '$DockerContainer' nor 'sqlcmd' CLI tool was detected." -ForegroundColor Yellow
Write-Host "You can run 'seed_master_data.sql' manually via:" -ForegroundColor Yellow
Write-Host "1. SSMS / Azure Data Studio -> Open seed_master_data.sql and Execute"
Write-Host "2. VS Code MSSQL Extension -> Open seed_master_data.sql and Run Query"
Write-Host "3. Docker Command:"
Write-Host "   Get-Content .\seed_master_data.sql | docker exec -i $DockerContainer /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P '$Password' -C -d $Database"
