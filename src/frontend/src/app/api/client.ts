import axios from 'axios'
import {
  clearAccessToken,
  clearAccessTokenExpiry,
  clearStoredAuthenticatedUser,
  getAccessToken,
  hasValidAccessToken,
} from '../auth/tokenStorage'

const baseURL = import.meta.env.VITE_API_BASE_URL ?? '/api'

export const apiClient = axios.create({
  baseURL,
  timeout: 10_000,
  headers: {
    'Content-Type': 'application/json',
  },
})

apiClient.interceptors.request.use((config) => {
  const token = hasValidAccessToken() ? getAccessToken() : null

  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }

  return config
})

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error?.response?.status === 401) {
      clearAccessToken()
      clearAccessTokenExpiry()
      clearStoredAuthenticatedUser()
    }

    return Promise.reject(error)
  },
)
