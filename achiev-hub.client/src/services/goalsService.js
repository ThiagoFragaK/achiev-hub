import { deleteJson, getJson, postJson, putJson, toQuery } from './http'

export function getGoals(status) {
    return getJson(`/api/goals${toQuery({ status })}`)
}

export function getGoal(id) {
    return getJson(`/api/goals/${encodeURIComponent(String(id))}`)
}

export function getActiveGoalByGame(appId) {
    return getJson(`/api/goals/by-game/${encodeURIComponent(String(appId))}`)
}

export function getEligibleGames(q) {
    return getJson(`/api/goals/eligible-games${toQuery({ q })}`)
}

export function getCompletedSeries() {
    return getJson('/api/goals/completed-series')
}

export function getUnacknowledgedCompletedGoals() {
    return getJson('/api/goals/unacknowledged-completed')
}

export function acknowledgeGoals() {
    return postJson('/api/goals/acknowledge', {})
}

export function createGoal({ appId, percentageGoal, achievementIds = [] }) {
    return postJson('/api/goals', {
        appId,
        percentageGoal,
        achievementIds
    })
}

export function updateGoal(id, { percentageGoal }) {
    return putJson(`/api/goals/${encodeURIComponent(String(id))}`, { percentageGoal })
}

export function attachGoalAchievements(id, achievementIds) {
    return postJson(`/api/goals/${encodeURIComponent(String(id))}/achievements`, {
        achievementIds
    })
}

export function getGoalAchievements(id) {
    return getJson(`/api/goals/${encodeURIComponent(String(id))}/achievements`)
}

export function replaceGoalAchievements(id, { achievementIds, percentageGoal }) {
    return putJson(`/api/goals/${encodeURIComponent(String(id))}/achievements`, {
        achievementIds,
        percentageGoal
    })
}

export function abandonGoal(id) {
    return postJson(`/api/goals/${encodeURIComponent(String(id))}/abandon`, {})
}

export function reopenGoal(id) {
    return postJson(`/api/goals/${encodeURIComponent(String(id))}/reopen`, {})
}

export function deleteGoal(id) {
    return deleteJson(`/api/goals/${encodeURIComponent(String(id))}`)
}
