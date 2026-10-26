import { getUser } from '@/lib/session'

/**
 * Builds a Steam CDN URL for an app/achievement icon hash when the value is not already absolute.
 * DB stores 40-char hashes; absolute URLs are still accepted for backwards compatibility.
 */
export function steamAppIconUrl(appId, hashOrUrl) {
    if (!hashOrUrl) return ''
    if (/^https?:\/\//i.test(hashOrUrl)) return hashOrUrl
    if (!appId) return ''
    const hash = String(hashOrUrl).replace(/\.(jpe?g)$/i, '')
    return `https://media.steampowered.com/steamcommunity/public/images/apps/${appId}/${hash}.jpg`
}

/** Reconstructs the store header image from a Steam app id. */
export function steamHeaderImageUrl(appId) {
    if (!appId) return ''
    return `https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/${appId}/header.jpg`
}

export function getSessionSteamId() {
    return getUser()?.steamId?.trim() || ''
}
