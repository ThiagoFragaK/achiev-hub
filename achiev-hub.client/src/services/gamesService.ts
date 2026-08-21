import { getJson, toQuery } from './http'

export function getRecentGames(steamId: string, page = 1, pageSize = 7) {
  return getJson(`/api/steam/games/recent${toQuery({ steamId, page, pageSize })}`)
}

export function getLibrary(steamId: string, page = 1, pageSize = 25) {
  return getJson(`/api/steam/games${toQuery({ steamId, page, pageSize })}`)
}

export function getGameDetails(appId: string | number) {
  return getJson(`/api/steam/games/${encodeURIComponent(String(appId))}`)
}

export function getAchievements(steamId: string, appId: string | number, page = 1, pageSize = 25) {
  return getJson(
    `/api/steam/games/${encodeURIComponent(String(appId))}/achievements${toQuery({ steamId, page, pageSize })}`,
  )
}
