import { postJson, getJson, toQuery } from '@/services/http'
import { clearSession, getUser, setSession } from '@/lib/session'
import { getPlayer } from '@/services/playersService'

async function attachSteamProfile(user) {
    if (!user?.steamId?.trim()) {
        return user
    }

    try {
        const player = await getPlayer()
        return {
            ...user,
            personaName: player?.personaName || user.personaName,
            avatar: player?.avatar || user.avatar
        }
    } catch {
        return user
    }
}

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

    const user = await attachSteamProfile(data.user)
    setSession({
        accessToken: data.accessToken,
        user
    })

    return { ...data, user }
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

    const user = await attachSteamProfile(data.user)
    setSession({
        accessToken: data.accessToken,
        user
    })

    return { ...data, user }
}

export async function validateSteam(steamId) {
    return postJson('/api/register/validate-steam', { steamId })
}

export async function sendVerification(email, steamId) {
    return postJson('/api/register/send-verification', { email, steamId })
}

export async function confirmCode(email, code) {
    return postJson('/api/register/confirm-code', { email, code })
}

export async function register({ steamId, email, password, emailVerifiedToken }) {
    return postJson('/api/register', { steamId, email, password, emailVerifiedToken })
}

export async function getProvisioningStatus(steamId) {
    return getJson(`/api/register/status${toQuery({ steamId })}`)
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
