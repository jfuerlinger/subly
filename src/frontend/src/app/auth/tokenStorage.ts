const accessTokenKey = 'subly:auth:accessToken'
const authenticatedUserKey = 'subly:auth:user'

export function getAccessToken(): string | null {
  return window.localStorage.getItem(accessTokenKey)
}

export function setAccessToken(token: string): void {
  window.localStorage.setItem(accessTokenKey, token)
}

export function clearAccessToken(): void {
  window.localStorage.removeItem(accessTokenKey)
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
