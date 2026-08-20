import { getJson, toQuery } from './http';

export function getRecentGames(steamId, page = 1, pageSize = 7) {
  return getJson(`/api/steam/games/recent${toQuery({ steamId, page, pageSize })}`);
}

export function getLibrary(steamId, page = 1, pageSize = 25) {
  return getJson(`/api/steam/games${toQuery({ steamId, page, pageSize })}`);
}

export function getGameDetails(appId) {
  return getJson(`/api/steam/games/${encodeURIComponent(appId)}`);
}

export function getAchievements(steamId, appId, page = 1, pageSize = 25) {
  return getJson(`/api/steam/games/${encodeURIComponent(appId)}/achievements${toQuery({ steamId, page, pageSize })}`);
}
