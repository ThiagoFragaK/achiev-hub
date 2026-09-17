import { getUser } from '@/lib/session'

/**
 * Builds a Steam CDN URL for an app icon hash when the value is not already absolute.
 * Achievement icons from schema are usually full URLs already.
 */
export function steamAppIconUrl(appId, hashOrUrl) {
    if (!hashOrUrl) return ''
    if (/^https?:\/\//i.test(hashOrUrl)) return hashOrUrl
    if (!appId) return ''
    return `https://media.steampowered.com/steamcommunity/public/images/apps/${appId}/${hashOrUrl}.jpg`
}

export function getSessionSteamId() {
    return getUser()?.steamId?.trim() || ''
}
