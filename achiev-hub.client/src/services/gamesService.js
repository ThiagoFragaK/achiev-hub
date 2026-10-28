import { getJson, postJson, toQuery } from './http'

export function getRecentGames(steamId, page = 1, pageSize = 7) {
    return getJson(`/api/steam/games/recent${toQuery({ steamId, page, pageSize })}`)
}

export function getRecentAchievements(page = 1, pageSize = 10) {
    return getJson(`/api/steam/games/recent-achievements${toQuery({ page, pageSize })}`)
}

export function getLibrary(steamId, page = 1, pageSize = 15, filters = {}) {
    return getJson(`/api/steam/games${toQuery({
        steamId,
        page,
        pageSize,
        name: filters.name,
        minHours: filters.minHours,
        hasAchievements: filters.hasAchievements,
        sort: filters.sort
    })}`)
}

export function getGameDetails(appId) {
    return getJson(`/api/steam/games/${encodeURIComponent(String(appId))}`)
}

export function getAchievements(steamId, appId, page = 1, pageSize = 7, filters = {}) {
    return getJson(
        `/api/steam/games/${encodeURIComponent(String(appId))}/achievements${toQuery({
            steamId,
            page,
            pageSize,
            name: filters.name,
            status: filters.status,
            sort: filters.sort,
            global: filters.global && filters.global !== 'all' ? filters.global : undefined
        })}`
    )
}

export function syncLibrary() {
    return postJson('/api/steam/games/sync', {})
}

export function syncGameAchievements(appId) {
    return postJson(`/api/steam/games/${encodeURIComponent(String(appId))}/sync-achievements`, {})
}
