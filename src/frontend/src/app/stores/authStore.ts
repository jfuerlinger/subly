import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { AxiosError } from 'axios'
import type { AuthenticatedUser, LoginRequest, RegisterRequest } from '../types/auth'
import { login, register } from '../api/authApi'
import {
  clearAccessToken,
  clearStoredAuthenticatedUser,
  getAccessToken,
  getStoredAuthenticatedUser,
  setAccessToken,
  setStoredAuthenticatedUser,
} from '../auth/tokenStorage'

export const useAuthStore = defineStore('auth', () => {
  const accessToken = ref<string | null>(getAccessToken())
  const user = ref<AuthenticatedUser | null>(getStoredAuthenticatedUser())
  const authError = ref<string | null>(null)
  const loading = ref(false)

  const isAuthenticated = computed(() => !!accessToken.value)

  async function registerUser(request: RegisterRequest): Promise<void> {
    loading.value = true
    authError.value = null

    try {
      const response = await register(request)
      setSession(response.accessToken, response.user)
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
      setSession(response.accessToken, response.user)
    } catch (error) {
      authError.value = toErrorMessage(error, 'Anmeldung fehlgeschlagen.')
    } finally {
      loading.value = false
    }
  }

  function logout(): void {
    accessToken.value = null
    user.value = null
    authError.value = null
    clearAccessToken()
    clearStoredAuthenticatedUser()
  }

  function setSession(token: string, authenticatedUser: AuthenticatedUser): void {
    accessToken.value = token
    user.value = authenticatedUser
    setAccessToken(token)
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
