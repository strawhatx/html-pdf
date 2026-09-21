#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
COMPOSE_FILE="$ROOT_DIR/docker-compose.local.yml"
RENDERER_LOG="${TMPDIR:-/tmp}/html-pdf-renderer.log"

# Stop the background renderer when the API exits or the script is interrupted.
cleanup() {
  if [[ -n "${RENDERER_PID:-}" ]] && kill -0 "$RENDERER_PID" 2>/dev/null; then
    echo "Stopping renderer..."
    kill "$RENDERER_PID" 2>/dev/null || true
    wait "$RENDERER_PID" 2>/dev/null || true
  fi
}
trap cleanup EXIT INT TERM

cd "$ROOT_DIR"

# Check required local development tools before changing service state.
if ! docker info >/dev/null 2>&1; then
  echo "Docker Desktop is not running. Start Docker Desktop and try again."
  exit 1
fi

if ! command -v dotnet >/dev/null 2>&1; then
  echo ".NET SDK is required but was not found."
  exit 1
fi

if ! command -v docker >/dev/null 2>&1; then
  echo "Docker is required but was not found."
  exit 1
fi

# Start the infrastructure used by the API and renderer.
echo "Starting PostgreSQL and Redis..."
docker compose -f "$COMPOSE_FILE" up -d

# Do not run migrations until PostgreSQL accepts connections.
echo "Waiting for PostgreSQL..."
until docker compose -f "$COMPOSE_FILE" exec -T postgres pg_isready -U pdfapi -d pdfapi >/dev/null 2>&1; do
  sleep 1
done

# Redis is the renderer's job transport, so wait for it as well.
echo "Waiting for Redis..."
until [[ "$(docker compose -f "$COMPOSE_FILE" exec -T redis redis-cli ping 2>/dev/null | tr -d '\r')" == "PONG" ]]; do
  sleep 1
done

# Bring the local database schema up to the latest migration.
echo "Applying database migrations..."
if command -v dotnet-ef >/dev/null 2>&1; then
  dotnet ef database update -p src/data -s src/api
elif [[ -x "$HOME/.dotnet/tools/dotnet-ef" ]]; then
  "$HOME/.dotnet/tools/dotnet-ef" database update -p src/data -s src/api
else
  echo "dotnet-ef is required. Install it with: dotnet tool install --global dotnet-ef --version 10.0.12"
  exit 1
fi

# Run the worker in the background while keeping the API attached to this terminal.
echo "Starting renderer..."
dotnet run --project src/renderer/renderer.csproj >"$RENDERER_LOG" 2>&1 &
RENDERER_PID=$!
echo "Renderer started (PID $RENDERER_PID). Logs: $RENDERER_LOG"

echo "Starting API at http://localhost:5104..."
dotnet run --project src/api/api.csproj --urls http://localhost:5104
