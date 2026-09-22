import { getJson } from './http'

export function getUserStats() {
    return getJson('/api/users/stats')
}

export function getGameProgress(appId) {
    return getJson(`/api/users/stats/games/${encodeURIComponent(String(appId))}`)
}
