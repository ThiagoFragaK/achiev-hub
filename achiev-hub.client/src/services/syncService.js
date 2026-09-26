import { getJson, toQuery } from './http'

export function getSyncStatus() {
    return getJson('/api/steam/sync/status')
}

export function getProvisioningStatus(steamId) {
    return getJson(`/api/register/status${toQuery({ steamId })}`)
}
