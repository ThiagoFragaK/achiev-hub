const SESSION_KEY = 'achiev-hub-session'

export function isAuthenticated(): boolean {
  return localStorage.getItem(SESSION_KEY) === '1'
}

export function setAuthenticated(value: boolean): void {
  if (value) {
    localStorage.setItem(SESSION_KEY, '1')
  } else {
    localStorage.removeItem(SESSION_KEY)
  }
}
