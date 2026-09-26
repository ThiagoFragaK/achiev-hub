# AchievHub

Track Steam games and achievements, store a personal library, and set completion goals.

The backend talks to the Steam Web API and persists users, games, achievements, and goals in PostgreSQL. The Vue client is a SPA served through the ASP.NET Core host.

## Stack

| Layer | Tech |
| --- | --- |
| API | ASP.NET Core 10 |
| UI | Vue 3 + Vite |
| Database | PostgreSQL + EF Core |
| Steam | Web API + Store app details |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) 20.19+ or 22.12+
- PostgreSQL (default local database name: `achievhub`)
- A [Steam Web API key](https://steamcommunity.com/dev/apikey)

## Setup

1. Clone the repo and restore the frontend:

   ```bash
   cd achiev-hub.client
   npm install
   ```

2. Ensure PostgreSQL is running (or `docker compose up postgres`). Migrations and seed users apply automatically when the API starts. To apply migrations manually from `achiev-hub.Server`:

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

From Visual Studio, start the **achiev-hub.Server** project (HTTPS profile). The SPA proxy starts Vite automatically.

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
| `POST` | `/api/steam/games/sync` | Enqueue full library sync (**202**) |
| `POST` | `/api/steam/games/{appId}/sync-achievements` | Enqueue per-game achievement sync (**202**) |
| `GET` | `/api/steam/sync/status` | Sync jobs, avg %, coverage, updating flag |
| `POST` | `/api/register/validate-steam` | Validate Steam ID before OTP |
| `GET` | `/api/register/status?steamId=` | Provisioning readiness (anonymous) |

Stats:

| Method | Path | Description |
| --- | --- | --- |
| `GET` | `/api/users/stats` | Current user stats |
| `GET` | `/api/users/stats/games/{appId}` | Per-game stats |

## Project layout

```
achiev-hub.Server/     ASP.NET Core API, EF Core, Steam integration
achiev-hub.client/     Vue 3 + Vite SPA
achiev-hub.slnx        Solution
```

Server layout in short: `Controllers` → `Services` → `Repositories` / `ApplicationDbContext`. Steam HTTP calls live in `SteamRepository`. Domain types are in `Entities`.

## Docker

`achiev-hub.Server/Dockerfile` builds one image used for both API and worker. Role is selected with `SyncWorker__AppRole`:

| Value | Behavior |
| --- | --- |
| `Api` | HTTP API only (enqueues sync jobs) |
| `Worker` | Background job processor + nightly maintenance; `/health` only |
| `All` | API + worker in one process (**Railway default**) |

Compose runs `postgres`, `api` (`AppRole=Api`), and `worker` (`AppRole=Worker`).

1. Copy the env template and set secrets:

   ```bash
   cp .env.example .env
   ```

   Required in `.env`:

   - `STEAM_API_KEY` — Steam Web API key
   - `JWT_KEY` — signing key, at least 32 characters
   - `POSTGRES_PASSWORD` — Postgres password (defaults to `postgres` if omitted)

2. Start the stack:

   ```bash
   docker compose up --build
   ```

- App: `http://localhost:8080` (`ASPNETCORE_ENVIRONMENT=Production`)
- Worker: same image, processes `sync_jobs` (no public UI port required)
- Postgres: `localhost:6110` (user/db: `postgres` / `achievhub`; password from `POSTGRES_PASSWORD`)

EF Core migrations run automatically on startup. Seed users are Development-only and are not created under Compose Production. For local development without the API container, you can run only the database:

```bash
docker compose up postgres
```

### Railway

Deploy **one** service with `SyncWorker__AppRole=All` (API + worker in-process) plus your Postgres plugin and `SteamApi__ApiKey` / `Jwt__Key`. To scale later, add a second service with the same image and `SyncWorker__AppRole=Worker`, and set the web service to `Api`.