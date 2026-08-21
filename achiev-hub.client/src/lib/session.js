const SESSION_KEY = 'achiev-hub-session'

export function isAuthenticated() {
    return localStorage.getItem(SESSION_KEY) === '1'
}

export function setAuthenticated(value) {
    if (value) {
        localStorage.setItem(SESSION_KEY, '1')
    } else {
        localStorage.removeItem(SESSION_KEY)
    }
}
