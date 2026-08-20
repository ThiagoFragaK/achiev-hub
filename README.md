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

2. Create the database and apply migrations from `achiev-hub.Server`:

   ```bash
   cd achiev-hub.Server
   dotnet ef database update
   ```

   The default connection string in `appsettings.json` is:

   `Host=localhost;Port=5432;Database=achievhub;Username=postgres;Password=postgres`

3. Put your Steam API key in [user secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) (do not commit it):

   ```bash
   cd achiev-hub.Server
   dotnet user-secrets set "SteamApi:ApiKey" "YOUR_KEY"
   ```

## Run

From Visual Studio, start the **achiev-hub.Server** project (HTTPS profile). The SPA proxy starts Vite automatically.

From the CLI:

```bash
cd achiev-hub.Server
dotnet run
```

- API: `https://localhost:7254` (or `http://localhost:5067`)
- Vue dev server (via SPA proxy): `https://localhost:53139`
- OpenAPI document in Development: `/openapi/v1.json`

## API

Steam (read-through to Valve; requires `steamId` where noted):

| Method | Path | Description |
| --- | --- | --- |
| `GET` | `/api/steam/players/{steamId}` | Player profile |
| `GET` | `/api/steam/games?steamId=` | Owned library (paged) |
| `GET` | `/api/steam/games/recent?steamId=` | Recently played (paged) |
| `GET` | `/api/steam/games/{appId}` | Store / game details |
| `GET` | `/api/steam/games/{appId}/achievements?steamId=` | Schema + player unlocks (paged) |

Persisted resources (CRUD):

- `/api/users`
- `/api/games`
- `/api/achievements`
- `/api/goals`
- `/api/users-games`
- `/api/users-achievements`
- `/api/goal-achievements`

The Vue app is still a scaffold; call these endpoints from a services layer as the UI is built out.

## Project layout

```
achiev-hub.Server/     ASP.NET Core API, EF Core, Steam integration
achiev-hub.client/     Vue 3 + Vite SPA
achiev-hub.slnx        Solution
```

Server layout in short: `Controllers` → `Services` → `Repositories` / `ApplicationDbContext`. Steam HTTP calls live in `SteamRepository`. Domain types are in `Entities`.

## Docker

`achiev-hub.Server/Dockerfile` builds the API (and the SPA as part of publish). You still need PostgreSQL and `SteamApi:ApiKey` (environment variable or another secrets source) at runtime.
