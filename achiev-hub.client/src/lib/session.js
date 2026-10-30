import { reactive } from 'vue'

const USER_KEY = 'achiev-hub-user'

function readStoredUser() {
    const raw = localStorage.getItem(USER_KEY)
    if (!raw) return null
    try {
        return JSON.parse(raw)
    } catch {
        return null
    }
}

const state = reactive({
    user: readStoredUser()
})

export function useSession() {
    return state
}

/** @deprecated Token lives in an HttpOnly cookie; kept for API compatibility. */
export function getToken() {
    return null
}

export function getUser() {
    return state.user ?? readStoredUser()
}

export function isAuthenticated() {
    return !!getUser()
}

export function setSession({ accessToken, user }) {
    // accessToken is intentionally ignored — auth uses HttpOnly cookie only.
    void accessToken

    if (user) {
        state.user = user
        localStorage.setItem(USER_KEY, JSON.stringify(user))
    }
}

export function mergeUser(partial) {
    const next = { ...(getUser() || {}), ...partial }
    state.user = next
    localStorage.setItem(USER_KEY, JSON.stringify(next))
    return next
}

export function clearSession() {
    state.user = null
    localStorage.removeItem(USER_KEY)
    localStorage.removeItem('achiev-hub-token')
    localStorage.removeItem('achiev-hub-session')
}
