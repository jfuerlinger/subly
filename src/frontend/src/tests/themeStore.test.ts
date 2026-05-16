import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { nextTick } from 'vue'
import { useThemeStore } from '../app/stores/themeStore'

describe('themeStore', () => {
  let localStorageMock: Record<string, string>
  let mediaQueryListeners: Array<(event: MediaQueryListEvent) => void>
  let mockMediaQuery: { matches: boolean; media: string; addEventListener: any; removeEventListener: any }

  beforeEach(() => {
    setActivePinia(createPinia())
    vi.restoreAllMocks()

    // Mock localStorage
    localStorageMock = {}
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation((key: string) => localStorageMock[key] ?? null)
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation((key: string, value: string) => {
      localStorageMock[key] = value
    })

    // Mock matchMedia
    mediaQueryListeners = []
    mockMediaQuery = {
      matches: false,
      media: '(prefers-color-scheme: dark)',
      addEventListener: vi.fn((_, listener) => {
        mediaQueryListeners.push(listener as (event: MediaQueryListEvent) => void)
      }),
      removeEventListener: vi.fn(),
    }

    Object.defineProperty(window, 'matchMedia', {
      writable: true,
      value: vi.fn().mockReturnValue(mockMediaQuery),
    })

    // Mock document.documentElement.setAttribute
    vi.spyOn(document.documentElement, 'setAttribute')
  })

  it('defaults to system mode when localStorage is empty', () => {
    const store = useThemeStore()

    expect(store.theme).toBe('system')
  })

  it('loads theme from localStorage when valid', () => {
    localStorageMock['subly-theme'] = 'dark'

    const store = useThemeStore()

    expect(store.theme).toBe('dark')
  })

  it('falls back to system when localStorage contains invalid value', () => {
    localStorageMock['subly-theme'] = 'invalid-mode'

    const store = useThemeStore()

    expect(store.theme).toBe('system')
  })

  it('applies data-theme attribute on initialization with light mode', () => {
    localStorageMock['subly-theme'] = 'light'

    useThemeStore()

    expect(document.documentElement.setAttribute).toHaveBeenCalledWith('data-theme', 'light')
  })

  it('applies data-theme attribute on initialization with dark mode', () => {
    localStorageMock['subly-theme'] = 'dark'

    useThemeStore()

    expect(document.documentElement.setAttribute).toHaveBeenCalledWith('data-theme', 'dark')
  })

  it('applies data-theme based on system preference when mode is system', () => {
    mockMediaQuery.matches = true
    localStorageMock['subly-theme'] = 'system'

    useThemeStore()

    expect(document.documentElement.setAttribute).toHaveBeenCalledWith('data-theme', 'dark')
  })

  it('persists theme to localStorage when setTheme is called', async () => {
    const store = useThemeStore()

    store.setTheme('dark')
    await nextTick()

    expect(localStorageMock['subly-theme']).toBe('dark')
  })

  it('applies data-theme attribute when setTheme is called', async () => {
    const store = useThemeStore()
    vi.clearAllMocks()

    store.setTheme('light')
    await nextTick()

    expect(document.documentElement.setAttribute).toHaveBeenCalledWith('data-theme', 'light')
  })

  it('reacts to system preference changes when mode is system', () => {
    localStorageMock['subly-theme'] = 'system'
    mockMediaQuery.matches = false

    const store = useThemeStore()
    expect(store.theme).toBe('system')

    vi.clearAllMocks()

    // Simulate system preference change to dark
    mockMediaQuery.matches = true
    const event = new Event('change') as MediaQueryListEvent
    mediaQueryListeners.forEach((listener) => listener(event))

    expect(document.documentElement.setAttribute).toHaveBeenCalledWith('data-theme', 'dark')
  })

  it('does not react to system preference changes when mode is not system', () => {
    localStorageMock['subly-theme'] = 'light'
    mockMediaQuery.matches = false

    const store = useThemeStore()
    expect(store.theme).toBe('light')

    vi.clearAllMocks()

    // Simulate system preference change
    mockMediaQuery.matches = true
    const event = new Event('change') as MediaQueryListEvent
    mediaQueryListeners.forEach((listener) => listener(event))

    // Should not apply theme again since mode is 'light', not 'system'
    expect(document.documentElement.setAttribute).not.toHaveBeenCalled()
  })

  it('updates to system mode and applies current system preference', async () => {
    mockMediaQuery.matches = true
    const store = useThemeStore()
    store.setTheme('light')
    await nextTick()

    vi.clearAllMocks()

    store.setTheme('system')
    await nextTick()

    expect(localStorageMock['subly-theme']).toBe('system')
    expect(document.documentElement.setAttribute).toHaveBeenCalledWith('data-theme', 'dark')
  })
})
