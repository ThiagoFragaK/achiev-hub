import { reactive } from 'vue'

const TOKEN_KEY = 'achiev-hub-token'
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
    token: localStorage.getItem(TOKEN_KEY),
    user: readStoredUser()
})

export function useSession() {
    return state
}

export function getToken() {
    return state.token || localStorage.getItem(TOKEN_KEY)
}

export function getUser() {
    return state.user ?? readStoredUser()
}

export function isAuthenticated() {
    return !!getToken()
}

export function setSession({ accessToken, user }) {
    state.token = accessToken || null
    if (accessToken) {
        localStorage.setItem(TOKEN_KEY, accessToken)
    } else {
        localStorage.removeItem(TOKEN_KEY)
    }

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
    state.token = null
    state.user = null
    localStorage.removeItem(TOKEN_KEY)
    localStorage.removeItem(USER_KEY)
    localStorage.removeItem('achiev-hub-session')
}
