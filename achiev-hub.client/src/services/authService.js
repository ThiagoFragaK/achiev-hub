import { postJson } from '@/services/http'
import { clearSession, setSession } from '@/lib/session'

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

export async function logout() {
    try {
        await postJson('/api/logout', {})
    } catch {
        // Always clear local session even if revoke fails.
    } finally {
        clearSession()
    }
}
