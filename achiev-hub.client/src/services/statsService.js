import { getJson } from './http'

export function getUserStats() {
    return getJson('/api/users/stats')
}

export function getGameProgress(appId) {
    return getJson(`/api/users/stats/games/${encodeURIComponent(String(appId))}`)
}

export function getAchievementsByMonth() {
    return getJson('/api/users/stats/achievements-by-month')
}

export function getAchievementsForMonth(year, month, page = 1, pageSize = 10, name = '') {
    const params = new URLSearchParams({
        page: String(page),
        pageSize: String(pageSize)
    })
    const trimmed = (name || '').trim()
    if (trimmed) {
        params.set('name', trimmed)
    }
    return getJson(
        `/api/users/stats/achievements-by-month/${encodeURIComponent(String(year))}/${encodeURIComponent(String(month))}?${params}`
    )
}

export function getGamesPercentageBands() {
    return getJson('/api/users/stats/games-percentage-bands')
}

export function getGamesPercentageBandGames() {
    return getJson('/api/users/stats/games-percentage-band-games')
}

export function getGamesForAverage() {
    return getJson('/api/users/stats/games-for-average')
}

export function getCompletedGamesByYear() {
    return getJson('/api/users/stats/completed-games-by-year')
}
