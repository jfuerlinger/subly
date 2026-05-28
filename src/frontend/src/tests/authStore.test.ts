import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useAuthStore } from '../app/stores/authStore'
import * as authApi from '../app/api/authApi'

describe('authStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.restoreAllMocks()
    window.localStorage.clear()
  })

  it('logs in and persists token', async () => {
    vi.spyOn(authApi, 'login').mockResolvedValue({
      accessToken: 'jwt-token',
      expiresAtUtc: '2026-05-28T12:00:00Z',
      user: {
        id: 'user-1',
        firstName: 'Max',
        lastName: 'Muster',
        email: 'max@example.com',
      },
    })

    const store = useAuthStore()
    await store.loginUser({ email: 'max@example.com', password: 'Secure123!' })

    expect(store.isAuthenticated).toBe(true)
    expect(window.localStorage.getItem('subly:auth:accessToken')).toBe('jwt-token')
  })

  it('clears session on logout', async () => {
    vi.spyOn(authApi, 'register').mockResolvedValue({
      accessToken: 'jwt-token',
      expiresAtUtc: '2026-05-28T12:00:00Z',
      user: {
        id: 'user-1',
        firstName: 'Max',
        lastName: 'Muster',
        email: 'max@example.com',
      },
    })

    const store = useAuthStore()
    await store.registerUser({
      firstName: 'Max',
      lastName: 'Muster',
      email: 'max@example.com',
      password: 'Secure123!',
    })
    store.logout()

    expect(store.isAuthenticated).toBe(false)
    expect(window.localStorage.getItem('subly:auth:accessToken')).toBeNull()
  })
})
