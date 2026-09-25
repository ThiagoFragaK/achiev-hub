import { getJson } from './http'

export function getPlayer() {
    return getJson('/api/steam/players')
}
