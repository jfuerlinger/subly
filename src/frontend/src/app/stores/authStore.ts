import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { AxiosError } from 'axios'
import type { AuthenticatedUser, LoginRequest, RegisterRequest } from '../types/auth'
import { devLogin, login, register } from '../api/authApi'
import { markOnboardingPending } from '../onboarding/onboardingStorage'
import {
  clearAccessToken,
  clearAccessTokenExpiry,
  clearStoredAuthenticatedUser,
  getAccessToken,
  getAccessTokenExpiry,
  getStoredAuthenticatedUser,
  hasValidAccessToken,
  setAccessToken,
  setAccessTokenExpiry,
  setStoredAuthenticatedUser,
} from '../auth/tokenStorage'

export const useAuthStore = defineStore('auth', () => {
  const accessToken = ref<string | null>(getAccessToken())
  const expiresAtUtc = ref<Date | null>(getAccessTokenExpiry())
  const user = ref<AuthenticatedUser | null>(getStoredAuthenticatedUser())
  const authError = ref<string | null>(null)
  const loading = ref(false)

  if (!hasValidAccessToken()) {
    accessToken.value = null
    expiresAtUtc.value = null
    user.value = null
    clearStoredAuthenticatedUser()
  }

  const isAuthenticated = computed(
    () => !!accessToken.value && !!expiresAtUtc.value && expiresAtUtc.value.getTime() > Date.now(),
  )

  async function registerUser(request: RegisterRequest): Promise<void> {
    loading.value = true
    authError.value = null

    try {
      const response = await register(request)
      setSession(response.accessToken, response.expiresAtUtc, response.user)
      markOnboardingPending(response.user.id)
    } catch (error) {
      authError.value = toErrorMessage(error, 'Registrierung fehlgeschlagen.')
    } finally {
      loading.value = false
    }
  }

  async function loginUser(request: LoginRequest): Promise<void> {
    loading.value = true
    authError.value = null

    try {
      const response = await login(request)
      setSession(response.accessToken, response.expiresAtUtc, response.user)
    } catch (error) {
      authError.value = toErrorMessage(error, 'Anmeldung fehlgeschlagen.')
    } finally {
      loading.value = false
    }
  }

  async function devLoginUser(): Promise<void> {
    loading.value = true
    authError.value = null

    try {
      const response = await devLogin()
      setSession(response.accessToken, response.expiresAtUtc, response.user)
    } catch (error) {
      authError.value = toErrorMessage(error, 'Dev-Login fehlgeschlagen.')
    } finally {
      loading.value = false
    }
  }

  function logout(): void {
    accessToken.value = null
    expiresAtUtc.value = null
    user.value = null
    authError.value = null
    clearAccessToken()
    clearAccessTokenExpiry()
    clearStoredAuthenticatedUser()
  }

  function setSession(token: string, expiresAt: string, authenticatedUser: AuthenticatedUser): void {
    const parsedExpiresAt = new Date(expiresAt)
    if (Number.isNaN(parsedExpiresAt.getTime())) {
      authError.value = 'Ungültiges Ablaufdatum im Login-Token.'
      logout()
      return
    }

    accessToken.value = token
    expiresAtUtc.value = parsedExpiresAt
    user.value = authenticatedUser
    setAccessToken(token)
    setAccessTokenExpiry(parsedExpiresAt.toISOString())
    setStoredAuthenticatedUser(authenticatedUser)
  }

  return {
    accessToken,
    user,
    authError,
    loading,
    isAuthenticated,
    registerUser,
    loginUser,
    devLoginUser,
    logout,
  }
})

function toErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof AxiosError) {
    const detail = error.response?.data?.detail
    if (typeof detail === 'string' && detail.length > 0) {
      return detail
    }
  }

  return fallback
}
