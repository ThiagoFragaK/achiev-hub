import { postJson } from '@/services/http'
import { clearSession, getUser, setSession } from '@/lib/session'

export async function login(steamId, password) {
    const response = await postJson('/api/login', { steamId, password })
    const data = response?.data
    if (!data?.accessToken) {
        throw new Error(response?.message || 'Login failed')
    }

    setSession({
        accessToken: data.accessToken,
        user: data.user
    })

    return data
}

export async function continueAsGuest(steamId) {
    const response = await postJson('/api/guest', { steamId })
    const data = response?.data
    if (!data?.accessToken) {
        throw new Error(response?.message || 'Guest session failed')
    }

    setSession({
        accessToken: data.accessToken,
        user: data.user
    })

    return data
}

export async function sendVerification(email) {
    return postJson('/api/register/send-verification', { email })
}

export async function confirmCode(email, code) {
    return postJson('/api/register/confirm-code', { email, code })
}

export async function register({ steamId, email, password, emailVerifiedToken }) {
    return postJson('/api/register', { steamId, email, password, emailVerifiedToken })
}

export async function logout() {
    const user = getUser()
    try {
        if (user?.role !== 'guest') {
            await postJson('/api/logout', {})
        }
    } catch {
        // Always clear local session even if revoke fails.
    } finally {
        clearSession()
    }
}
