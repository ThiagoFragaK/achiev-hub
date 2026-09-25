import { getJson, postJson, toQuery } from './http'

export function getRecentGames(page = 1, pageSize = 7) {
    return getJson(`/api/steam/games/recent${toQuery({ page, pageSize })}`)
}

export function getLibrary(page = 1, pageSize = 15, filters = {}) {
    return getJson(`/api/steam/games${toQuery({
        page,
        pageSize,
        name: filters.name,
        minHours: filters.minHours,
        hasAchievements: filters.hasAchievements
    })}`)
}

export function getGameDetails(appId) {
    return getJson(`/api/steam/games/${encodeURIComponent(String(appId))}`)
}

export function getAchievements(appId, page = 1, pageSize = 7, filters = {}) {
    return getJson(
        `/api/steam/games/${encodeURIComponent(String(appId))}/achievements${toQuery({
            page,
            pageSize,
            name: filters.name,
            status: filters.status
        })}`
    )
}

export function syncLibrary() {
    return postJson('/api/steam/games/sync', {})
}

export function syncGameAchievements(appId) {
    return postJson(`/api/steam/games/${encodeURIComponent(String(appId))}/sync-achievements`, {})
}
