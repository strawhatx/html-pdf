# HTML PDF

Local HTML-to-PDF service built with .NET, Playwright/Chromium, PostgreSQL, and Redis.

## Project Layout

```text
src/
├── api/       ASP.NET Core API host
├── core/      Shared jobs, contracts, and errors
├── data/      EF Core and PostgreSQL persistence
├── queue/     Redis queue implementation
└── renderer/  Playwright rendering worker
tests/         Automated tests
docker-compose.local.yml  Local PostgreSQL and Redis
```

## Prerequisites

- .NET 10 SDK
- Docker Desktop
- Node.js/npm, used to install the Playwright Chromium binary

Verify the tools:

```bash
dotnet --version
docker --version
docker compose version
node --version
```

## Local Setup

Run these commands from the repository root.

### 1. Start PostgreSQL and Redis

```bash
docker compose -f docker-compose.local.yml up -d
docker compose -f docker-compose.local.yml ps
```

The local services expose PostgreSQL at `localhost:5432` and Redis at `localhost:6379`.

The Compose database uses:

```text
Database: pdfapi
Username: pdfapi
Password: localdev
```

### 2. Configure API database secrets

User secrets override `appsettings.json`. Set them once for local development:

```bash
dotnet user-secrets set DbConnection:Reader localhost --project src/api
dotnet user-secrets set DbConnection:Writer localhost --project src/api
dotnet user-secrets set DbConnection:Port 5432 --project src/api
dotnet user-secrets set DbConnection:Database pdfapi --project src/api
dotnet user-secrets set DbConnection:Username pdfapi --project src/api
dotnet user-secrets set DbConnection:Password localdev --project src/api
dotnet user-secrets set DbConnection:IamToken localdev --project src/api
```

The `IamToken` value is used by the existing configuration resolver as the password fallback. For AWS Aurora, replace these local values with the reader/writer endpoints and IAM token instead.

### 3. Apply EF Core migrations

Install the EF CLI if it is not already available:

```bash
dotnet tool install --global dotnet-ef --version 10.0.12
```

Apply the schema:

```bash
dotnet ef database update -p src/data -s src/api
```

### 4. Install Playwright Chromium

The renderer uses the Chromium revision expected by the .NET Playwright package:

```bash
npx --yes playwright@1.62.0 install chromium
```

### 5. Build the projects

```bash
dotnet build src/api/api.csproj
dotnet build src/renderer/renderer.csproj
```

## Run Locally

The easiest way to start the complete local stack is:

```bash
./start-local.sh
```

This starts PostgreSQL and Redis, waits for them to accept connections, applies EF migrations, starts the renderer, and then starts the API at `http://localhost:5104`. Press `Ctrl+C` to stop the API and renderer. The renderer log is written to a temporary file and its path is printed by the script.

Start the API in one terminal:

```bash
dotnet run --project src/api/api.csproj
```

The default HTTP URL is `http://localhost:5104`.

Start the renderer in a second terminal:

```bash
dotnet run --project src/renderer/renderer.csproj
```

The renderer connects to Redis, consumes jobs, runs Chromium, stores job status in PostgreSQL, and periodically requeues jobs that exceed the Redis visibility timeout.

Check that the API is responding:

```bash
curl http://localhost:5104/health
```

Expected response:

```json
{"status":"ok"}
```

To use port `5080` instead:

```bash
dotnet run --project src/api/api.csproj --urls http://localhost:5080
```

## Break-Token Diagnostics

`BreakTokenDiagnostics` is a manual console harness for running one HTML fixture through Chromium and inspecting the resulting PDF. It intentionally has no database, Redis, or hosted worker dependencies.

Build it and install its Playwright browser once:

```bash
dotnet build src/BreakTokenDiagnostics/BreakTokenDiagnostics.csproj
npx --yes playwright@1.62.0 install chromium
```

Run the included smoke fixture:

```bash
mkdir -p results
dotnet run --project src/BreakTokenDiagnostics -- src/BreakTokenDiagnostics/fixtures/hello-world.html --out results/hello-world.pdf
```

Run a directory of benchmark fixtures after adding the HTML files:

```bash
mkdir -p results
for f in fixtures/*.html; do
	dotnet run --project src/BreakTokenDiagnostics -- "$f" --out "results/$(basename "$f" .html).pdf"
done
```

## Stop Local Services

Stop the .NET processes with `Ctrl+C`, then stop the containers:

```bash
docker compose -f docker-compose.local.yml down
```

To remove the local PostgreSQL data volume as well:

```bash
docker compose -f docker-compose.local.yml down -v
```

## Useful Commands

List migrations:

```bash
dotnet ef migrations list -p src/data -s src/api
```

Run tests:

```bash
dotnet test
```

Inspect service logs:

```bash
docker compose -f docker-compose.local.yml logs -f postgres redis
```
