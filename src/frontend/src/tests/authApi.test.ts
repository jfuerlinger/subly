import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { AxiosResponse } from 'axios'
import { apiClient } from '../app/api/client'
import { login, register } from '../app/api/authApi'
import type { AuthResponse } from '../app/types/auth'

describe('authApi', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
  })

  it('registers a user', async () => {
    const payload: AuthResponse = {
      accessToken: 'token',
      expiresAtUtc: '2026-05-28T12:00:00Z',
      user: {
        id: 'user-1',
        firstName: 'Max',
        lastName: 'Muster',
        email: 'max@example.com',
      },
    }
    vi.spyOn(apiClient, 'post').mockResolvedValue({ data: payload } as AxiosResponse<AuthResponse>)

    const result = await register({
      firstName: 'Max',
      lastName: 'Muster',
      email: 'max@example.com',
      password: 'Secure123!',
    })

    expect(result.accessToken).toBe('token')
  })

  it('logs in a user', async () => {
    const payload: AuthResponse = {
      accessToken: 'token',
      expiresAtUtc: '2026-05-28T12:00:00Z',
      user: {
        id: 'user-1',
        firstName: 'Max',
        lastName: 'Muster',
        email: 'max@example.com',
      },
    }
    vi.spyOn(apiClient, 'post').mockResolvedValue({ data: payload } as AxiosResponse<AuthResponse>)

    const result = await login({
      email: 'max@example.com',
      password: 'Secure123!',
    })

    expect(result.user.email).toBe('max@example.com')
  })
})
