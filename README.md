# AchievHub

Track Steam games and achievements, store a personal library, and set completion goals.

The backend talks to the Steam Web API and persists users, games, achievements, and goals in PostgreSQL. The Vue client is a SPA served through the ASP.NET Core host. Heavy Steam sync runs in the separate **steam-sync** microservice via RabbitMQ.

## Stack

| Layer | Tech |
| --- | --- |
| API | ASP.NET Core 10 |
| Sync worker | steam-sync (.NET 10 + MassTransit) |
| UI | Vue 3 + Vite |
| Database | PostgreSQL + EF Core |
| Queue | RabbitMQ |
| Steam | Web API + Store app details |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) 20.19+ or 22.12+
- PostgreSQL (default local database name: `achievhub`)
- RabbitMQ (for sync job publishing)
- A [Steam Web API key](https://steamcommunity.com/dev/apikey)

## Setup

1. Clone the repo and restore the frontend:

   ```bash
   cd achiev-hub.client
   npm install
   ```

2. Ensure PostgreSQL is running (or `docker compose up postgres rabbitmq`). Migrations and seed users apply automatically when the API starts. To apply migrations manually from `achiev-hub.Server`:

   ```bash
   cd achiev-hub.Server
   dotnet ef database update
   ```

   The default connection string in `appsettings.json` is for local DX only:

   `Host=localhost;Port=6110;Database=achievhub;Username=postgres;Password=postgres`

   Override it for anything beyond local Development (env / Compose / local JSON).

3. Configure secrets. Committed `appsettings*.json` leave `SteamApi:ApiKey` and `Jwt:Key` empty; the API will not start without them.

   Prefer [user secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) for local Development:

   ```bash
   cd achiev-hub.Server
   dotnet user-secrets set "SteamApi:ApiKey" "YOUR_KEY"
   dotnet user-secrets set "Jwt:Key" "YOUR_LOCAL_JWT_KEY_AT_LEAST_32_CHARS_LONG"
   ```

   Or copy `appsettings.Development.local.json.example` to `appsettings.Development.local.json` (gitignored) and fill in values.

   If a Steam API key was ever committed to this repo, rotate it in the [Steam Web API key](https://steamcommunity.com/dev/apikey) portal.

## Auth

Login uses Steam ID + password and returns a JWT Bearer token.

Seed users (Development only, password `achiev456`; created once, not reset on restart):

| Steam ID | Email | Role |
| --- | --- | --- |
| `76561198000000001` | `user@example.com` | user |
| `76561198000000002` | `admin@example.com` | admin |

| Method | Path | Description |
| --- | --- | --- |
| `POST` | `/api/login` | `{ steamId, password }` → access token |
| `POST` | `/api/guest` | `{ steamId }` → guest access token |
| `POST` | `/api/logout` | Revoke current token (requires Bearer) |

Protected APIs require `Authorization: Bearer <token>`. Steam library/profile reads use the token `steam_id` claim (do not pass another player's Steam ID).

## Run

From Visual Studio, start the **achiev-hub.Server** project (HTTPS profile). The SPA proxy starts Vite automatically. Also run **steam-sync** worker so queued jobs process.

From the CLI:

```bash
cd achiev-hub.Server
dotnet run
```

- API: `https://localhost:7254` (or `http://localhost:5067`)
- Vue dev server (via SPA proxy): `https://localhost:9100`
- OpenAPI document in Development: `/openapi/v1.json`

## API

Steam (identity from JWT `steam_id`):

| Method | Path | Description |
| --- | --- | --- |
| `GET` | `/api/steam/players` | Player profile for the current token |
| `GET` | `/api/steam/games` | Owned library (paged) |
| `GET` | `/api/steam/games/recent` | Recently played (paged) |
| `GET` | `/api/steam/games/{appId}` | Store / game details |
| `GET` | `/api/steam/games/{appId}/achievements` | Schema + player unlocks (paged) |
| `POST` | `/api/steam/games/sync` | Publish full library sync to RabbitMQ (**202**) |
| `POST` | `/api/steam/games/{appId}/sync-achievements` | Publish per-game achievement sync (**202**) |
| `GET` | `/api/steam/sync/status` | Sync progress, avg %, coverage, updating / partial flags + `sync` block |
| `GET` | `/api/users/{id}/sync-status` | Dedicated sync status DTO (`user_sync_status`) |
| `POST` | `/api/register/validate-steam` | Validate Steam ID before OTP |
| `GET` | `/api/register/status?steamId=` | Registration / sync readiness (anonymous) |

Stats:

| Method | Path | Description |
| --- | --- | --- |
| `GET` | `/api/users/stats` | Current user stats (`sync` block; Steam live fallback while pending) |
| `GET` | `/api/users/stats/games/{appId}` | Per-game stats |

## Progressive sync (login & library)

Login and registration **never wait** for achievement crawls. The API stores a lightweight Steam snapshot (owned + recent games) at register, then publishes RabbitMQ jobs to **steam-sync**.

### Job types

| Trigger | Job | Priority |
| --- | --- | --- |
| Register | `user_sync` + `scope=initial` (recent + top 50 by playtime) | high |
| Login (never fully synced) | `user_sync` + `scope=initial` | high |
| Login (`last_full_sync` &gt; 14 days) | `full_library_resync` | low |
| Login (play since last partial) | `recent_activity_only` | high |
| Games page if stale (&gt; 14 days) | `recent_activity_only` (max 1 auto/day) | medium |
| Manual “Sync now” | `full_library_resync` (max 2/day) | high |

Duplicate publishes are blocked while `user_sync_status.status = Syncing` or `locked_until` is in the future.

### Sync status values

`pending` → `syncing` → `partial` (initial pass done) → `complete` (full crawl) / `failed`

Auth and data responses include a `sync` block:

```json
{
  "status": "partial",
  "lastFullSync": null,
  "lastPartialSync": "2026-09-29T08:00:00Z",
  "gamesSynced": 540,
  "gamesTotal": 1200,
  "progressPercent": 45
}
```

While status is `pending`/`syncing` in the first 24h and the DB shell is empty, library/stats may return `source: "steam_live"` and `fallback: true`.

### Frontend sync UX

- **App shell banner**: while `isUpdating`, show `Syncing X/Y games`. While `partial`, show a softer “Library partially imported” message.
- **Incomplete stats**: treat `sync.status !== "complete"` as incomplete (badge / muted charts).
- **Pending games**: library items with `importPending: true` should appear greyed / “pending import”.
- **Live fallback**: if a games/stats response has `fallback: true`, show “Importing your library… showing live Steam data”.
- Poll `GET /api/steam/sync/status` about every 5s while logged in (already done in `AppShell`).

## Project layout

```
achiev-hub.Server/     ASP.NET Core API, EF Core, Steam reads + sync enqueue
achiev-hub.client/     Vue 3 + Vite SPA
../steam-sync/         Sync worker (MassTransit consumer)
achiev-hub.slnx        Solution
```

Server layout in short: `Controllers` → `Services` → `Repositories` / `ApplicationDbContext`. Live Steam HTTP for guests/validation stays in `SteamRepository`. Bulk sync lives in steam-sync.

## Docker

Compose runs `postgres`, `rabbitmq`, `api`, and `steam-sync-worker` (2 replicas). Sync jobs go to RabbitMQ (`steam_sync_jobs`). Build context is the parent `MyApps` folder (API references `SteamSync.Shared`).

1. Copy the env template and set secrets:

   ```bash
   cp .env.example .env
   ```

   Required in `.env`:

   - `STEAM_API_KEY` — Steam Web API key
   - `JWT_KEY` — signing key, at least 32 characters
   - `POSTGRES_PASSWORD` — Postgres password (defaults to `postgres` if omitted)

2. Start the stack (from `achiev-hub`):

   ```bash
   docker compose up --build
   ```

- App: `http://localhost:8080`
- RabbitMQ Management UI: `http://localhost:15672` (guest/guest)
- Postgres: `localhost:6110`

EF Core migrations run automatically on startup. Seed users are Development-only and are not created under Compose Production.

```bash
docker compose up postgres rabbitmq
```

### Railway

Deploy the **API** plus a separate **steam-sync** worker, Postgres, and RabbitMQ. Configure `RabbitMQ__*`, connection strings, `SteamApi__ApiKey`, and `Jwt__Key`. See [steam-sync/README.md](../steam-sync/README.md).
