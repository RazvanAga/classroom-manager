# Starts the whole local stack for manual testing:
#   1. Postgres (docker compose), waits until healthy
#   2. the API   -> http://localhost:5080   (own window)
#   3. the frontend -> http://localhost:3000 (own window)
# then opens the browser once the frontend responds.
#
# Run from the repo root:  ./dev.ps1
# Stop: close the two spawned windows (Ctrl+C in each). The database keeps running;
# stop it with  ./dev.ps1 -StopDb  (or  docker compose -f docker-compose.dev.yml down).

param(
    [switch]$StopDb  # tear down the Postgres container and exit
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$compose = Join-Path $root "docker-compose.dev.yml"

if ($StopDb) {
    Write-Host "Stopping the database..." -ForegroundColor Yellow
    docker compose -f $compose down
    return
}

# --- Docker must be running ---
docker info *> $null
if ($LASTEXITCODE -ne 0) {
    Write-Host "Docker isn't running. Start Docker Desktop and try again." -ForegroundColor Red
    return
}

# --- 1. Database ---
Write-Host "Starting Postgres..." -ForegroundColor Cyan
docker compose -f $compose up -d | Out-Null

Write-Host "Waiting for the database to be healthy..." -ForegroundColor Cyan
for ($i = 0; $i -lt 30; $i++) {
    $status = (docker inspect --format '{{.State.Health.Status}}' classroom-dev-db 2>$null)
    if ($status -eq "healthy") { break }
    Start-Sleep -Seconds 2
}
if ($status -ne "healthy") {
    Write-Host "Database did not become healthy in time. Check 'docker logs classroom-dev-db'." -ForegroundColor Red
    return
}
Write-Host "Database is healthy." -ForegroundColor Green

# --- 2. Frontend dependencies (first run only) ---
if (-not (Test-Path (Join-Path $root "frontend/node_modules"))) {
    Write-Host "Installing frontend dependencies (first run, this may take a minute)..." -ForegroundColor Cyan
    Push-Location (Join-Path $root "frontend")
    npm install
    Pop-Location
}

# --- 3. Launch API and frontend, each in its own window ---
Write-Host "Launching the API (http://localhost:5080)..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$root'; dotnet run --project backend/src/Classroom.Api"

Write-Host "Launching the frontend (http://localhost:3000)..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$root/frontend'; npm run dev"

# --- 4. Wait for the frontend, then open the browser ---
Write-Host "Waiting for the app to come up..." -ForegroundColor Cyan
$ready = $false
for ($i = 0; $i -lt 40; $i++) {
    try {
        Invoke-WebRequest -Uri "http://localhost:3000" -UseBasicParsing -TimeoutSec 2 *> $null
        $ready = $true
        break
    } catch {
        Start-Sleep -Seconds 1
    }
}

if ($ready) {
    Write-Host "Opening http://localhost:3000" -ForegroundColor Green
    Start-Process "http://localhost:3000"
} else {
    Write-Host "Frontend isn't responding yet. Once it finishes compiling, open http://localhost:3000 manually." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Sign in with:  teacher@classroom.local  /  Passw0rd!" -ForegroundColor Green
Write-Host "API docs (Scalar): http://localhost:5080/scalar/v1"
Write-Host "Two windows opened (API + frontend). Close them or Ctrl+C to stop. Run './dev.ps1 -StopDb' to stop the database."
