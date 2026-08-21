import { getJson } from './http'

export interface Player {
  steamId?: string | null
  personaName?: string | null
  profileUrl?: string | null
  avatar?: string | null
  avatarFull?: string | null
  communityVisibilityState?: number | null
  personaState?: number | null
}

export function getPlayer(steamId: string) {
  return getJson<Player>(`/api/steam/players/${encodeURIComponent(steamId)}`)
}
