import { defineStore } from 'pinia'
import { ref, watch } from 'vue'

export type ThemeMode = 'light' | 'dark' | 'system'

const STORAGE_KEY = 'subly-theme'

function getSystemTheme(): 'light' | 'dark' {
  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
}

function applyTheme(mode: ThemeMode) {
  const resolved = mode === 'system' ? getSystemTheme() : mode
  document.documentElement.setAttribute('data-theme', resolved)
}

export const useThemeStore = defineStore('theme', () => {
  const saved = localStorage.getItem(STORAGE_KEY) as ThemeMode | null
  const theme = ref<ThemeMode>(saved ?? 'system')

  applyTheme(theme.value)

  // Keep in sync when system preference changes
  const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)')
  mediaQuery.addEventListener('change', () => {
    if (theme.value === 'system') applyTheme('system')
  })

  watch(theme, (newMode) => {
    localStorage.setItem(STORAGE_KEY, newMode)
    applyTheme(newMode)
  })

  function setTheme(mode: ThemeMode) {
    theme.value = mode
  }

  return { theme, setTheme }
})
