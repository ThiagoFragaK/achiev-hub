const TOKEN_KEY = 'achiev-hub-token'
const USER_KEY = 'achiev-hub-user'

export function getToken() {
    return localStorage.getItem(TOKEN_KEY)
}

export function getUser() {
    const raw = localStorage.getItem(USER_KEY)
    if (!raw) return null
    try {
        return JSON.parse(raw)
    } catch {
        return null
    }
}

export function isAuthenticated() {
    return !!getToken()
}

export function setSession({ accessToken, user }) {
    localStorage.setItem(TOKEN_KEY, accessToken)
    if (user) {
        localStorage.setItem(USER_KEY, JSON.stringify(user))
    }
}

export function mergeUser(partial) {
    const next = { ...(getUser() || {}), ...partial }
    localStorage.setItem(USER_KEY, JSON.stringify(next))
    return next
}

export function clearSession() {
    localStorage.removeItem(TOKEN_KEY)
    localStorage.removeItem(USER_KEY)
    localStorage.removeItem('achiev-hub-session')
}
