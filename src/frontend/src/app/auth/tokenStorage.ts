const accessTokenKey = 'subly:auth:accessToken'
const accessTokenExpiryKey = 'subly:auth:accessTokenExpiry'
const authenticatedUserKey = 'subly:auth:user'

export function getAccessToken(): string | null {
  return hasValidAccessToken() ? window.sessionStorage.getItem(accessTokenKey) : null
}

export function setAccessToken(token: string): void {
  window.sessionStorage.setItem(accessTokenKey, token)
}

export function clearAccessToken(): void {
  window.sessionStorage.removeItem(accessTokenKey)
}

export function getAccessTokenExpiry(): Date | null {
  const value = window.sessionStorage.getItem(accessTokenExpiryKey)
  if (!value) {
    return null
  }

  const parsed = new Date(value)
  return Number.isNaN(parsed.getTime()) ? null : parsed
}

export function setAccessTokenExpiry(expiresAtUtc: string): void {
  window.sessionStorage.setItem(accessTokenExpiryKey, expiresAtUtc)
}

export function clearAccessTokenExpiry(): void {
  window.sessionStorage.removeItem(accessTokenExpiryKey)
}

export function hasValidAccessToken(): boolean {
  const token = window.sessionStorage.getItem(accessTokenKey)
  const expiresAt = getAccessTokenExpiry()
  if (!token || !expiresAt) {
    clearAccessToken()
    clearAccessTokenExpiry()
    return false
  }

  if (expiresAt.getTime() <= Date.now()) {
    clearAccessToken()
    clearAccessTokenExpiry()
    return false
  }

  return true
}

export interface StoredAuthenticatedUser {
  id: string
  firstName: string
  lastName: string
  email: string
}

export function getStoredAuthenticatedUser(): StoredAuthenticatedUser | null {
  const storedValue = window.localStorage.getItem(authenticatedUserKey)
  if (!storedValue) {
    return null
  }

  try {
    const parsed = JSON.parse(storedValue)
    if (
      typeof parsed?.id === 'string' &&
      typeof parsed?.firstName === 'string' &&
      typeof parsed?.lastName === 'string' &&
      typeof parsed?.email === 'string'
    ) {
      return parsed as StoredAuthenticatedUser
    }
  } catch {
    return null
  }

  return null
}

export function setStoredAuthenticatedUser(user: StoredAuthenticatedUser): void {
  window.localStorage.setItem(authenticatedUserKey, JSON.stringify(user))
}

export function clearStoredAuthenticatedUser(): void {
  window.localStorage.removeItem(authenticatedUserKey)
}
