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

export function getGamesPercentageBands() {
    return getJson('/api/users/stats/games-percentage-bands')
}
