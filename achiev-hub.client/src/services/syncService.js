import { getJson, toQuery } from './http'

export function getProvisioningStatus(steamId) {
    return getJson(`/api/register/status${toQuery({ steamId })}`)
}
