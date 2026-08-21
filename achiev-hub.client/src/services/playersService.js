import { getJson } from './http'

export function getPlayer(steamId) {
    return getJson(`/api/steam/players/${encodeURIComponent(steamId)}`)
}
