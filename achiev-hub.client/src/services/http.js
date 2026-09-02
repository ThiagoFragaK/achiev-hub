import { clearSession, getToken } from '@/lib/session'
import router from '@/router'

export class HttpError extends Error {
    constructor(message, status, body) {
        super(message)
        this.name = 'HttpError'
        this.status = status
        this.body = body
    }
}

async function request(url, options = {}) {
    const headers = new Headers(options.headers || {})
    if (!headers.has('Content-Type') && options.body) {
        headers.set('Content-Type', 'application/json')
    }

    const token = getToken()
    if (token) {
        headers.set('Authorization', `Bearer ${token}`)
    }

    const response = await fetch(url, { ...options, headers })

    if (response.status === 401 && !url.includes('/api/login')) {
        clearSession()
        if (router.currentRoute.value.name !== 'login') {
            router.push({ name: 'login', query: { redirect: router.currentRoute.value.fullPath } })
        }
    }

    if (!response.ok) {
        const text = await response.text()
        let body
        try {
            body = text ? JSON.parse(text) : null
        } catch {
            body = text
        }

        throw new HttpError(
            body?.message || `Request failed with status ${response.status}`,
            response.status,
            body
        )
    }

    if (response.status === 204) {
        return null
    }

    return response.json()
}

export async function getJson(url) {
    return request(url)
}

export async function postJson(url, body) {
    return request(url, {
        method: 'POST',
        body: JSON.stringify(body)
    })
}

export function toQuery(params) {
    const search = new URLSearchParams()

    Object.entries(params).forEach(([key, value]) => {
        if (value !== undefined && value !== null && value !== '') {
            search.set(key, String(value))
        }
    })

    const query = search.toString()
    return query ? `?${query}` : ''
}
