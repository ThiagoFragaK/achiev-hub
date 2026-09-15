const SPECIAL = /[!@#$%^&*(),.?":{}|<>]/

export function validatePassword(password) {
    if (!password || password.length < 12) {
        return 'Password must be at least 12 characters.'
    }
    if (!/[A-Z]/.test(password)) {
        return 'Password must include at least one uppercase letter.'
    }
    if (!/[0-9]/.test(password)) {
        return 'Password must include at least one number.'
    }
    if (!SPECIAL.test(password)) {
        return 'Password must include at least one special character.'
    }
    return ''
}