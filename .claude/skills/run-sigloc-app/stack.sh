#!/usr/bin/env bash
# Start / stop / inspect the local Sigloc stack (Windows + Git Bash):
#   Postgres 16 in Docker (:5432) -> Sigloc.Api (:5100) -> Vite UI (:5173).
#
# The API is ALWAYS given a local connection string. The developer's user secrets point
# ConnectionStrings:DefaultConnection at the hosted Neon database, and Development startup
# runs migrations + the seeder, so a plain `dotnet run` would write to the shared DB.
#
# usage: stack.sh up | down | status
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../../.." && pwd)"
LOGS="${SIGLOC_LOGS:-$(node -p 'require("os").tmpdir()')/sigloc}"
LOCAL_DB="Host=localhost;Port=5432;Database=sigloc;Username=sigloc;Password=sigloc;"

# Any HTTP answer (even 401/404) means the server is listening.
listening() { [ "$(curl -s -o /dev/null -w '%{http_code}' "$1" || true)" != "000" ]; }

wait_for() {
  local url=$1 secs=$2 log=$3
  for _ in $(seq "$secs"); do
    listening "$url" && return 0
    sleep 1
  done
  echo "!! $url did not come up in ${secs}s; last log lines ($log):" >&2
  tail -20 "$log" >&2
  return 1
}

# No lsof/fuser in Git Bash: ask Windows who owns the listening port.
kill_port() {
  powershell.exe -NoProfile -Command "Get-NetTCPConnection -LocalPort $1 -State Listen -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id \$_.OwningProcess -Force }"
}

up() {
  mkdir -p "$LOGS"

  (cd "$ROOT/sigloc-api" && docker compose up -d)
  until docker exec sigloc-postgres pg_isready -U sigloc -d sigloc >/dev/null 2>&1; do sleep 1; done
  echo "postgres: ready"

  if listening http://localhost:5100/api/products; then
    echo "api: already running on :5100 (run 'down' first to pick up code changes)"
  else
    (
      cd "$ROOT/sigloc-api/Sigloc.Api"
      export ConnectionStrings__DefaultConnection="$LOCAL_DB" \
        ASPNETCORE_ENVIRONMENT=Development \
        Monitoring__MockMode=true Monitoring__MockLatitude=-23.5505 Monitoring__MockLongitude=-46.6333
      nohup dotnet run --no-launch-profile --urls http://localhost:5100 > "$LOGS/api.log" 2>&1 &
    )
    wait_for http://localhost:5100/api/products 240 "$LOGS/api.log"
    echo "api: http://localhost:5100 (log: $LOGS/api.log)"
  fi

  if listening http://localhost:5173/; then
    echo "ui: already running on :5173"
  else
    (
      cd "$ROOT/sigloc-ui"
      VITE_API_BASE_URL=http://localhost:5100 nohup npx vite --port 5173 --strictPort > "$LOGS/ui.log" 2>&1 &
    )
    wait_for http://localhost:5173/ 60 "$LOGS/ui.log"
    echo "ui: http://localhost:5173 (log: $LOGS/ui.log)"
  fi

  status
}

down() {
  kill_port 5100
  kill_port 5173
  (cd "$ROOT/sigloc-api" && docker compose stop)
  echo "stopped api, ui and postgres (data kept in the sigloc-pgdata volume)"
}

status() {
  listening http://localhost:5100/api/products && echo "api: up" || echo "api: down"
  listening http://localhost:5173/ && echo "ui: up" || echo "ui: down"
  # Proof the API talks to the local container and not Neon: it holds connections here.
  local conns
  conns=$(docker exec sigloc-postgres psql -U sigloc -d sigloc -tAc \
    "select count(*) from pg_stat_activity where datname='sigloc' and pid <> pg_backend_pid()" 2>/dev/null || echo "?")
  echo "postgres: $conns connection(s) from the app"
}

case "${1:-}" in
  up) up ;;
  down) down ;;
  status) status ;;
  *) echo "usage: $0 up|down|status" >&2; exit 2 ;;
esac
