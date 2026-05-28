import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { nextTick } from 'vue'
import { useProfileStore } from '../app/stores/profileStore'

describe('profileStore', () => {
  let localStorageMock: Record<string, string>

  beforeEach(() => {
    setActivePinia(createPinia())
    vi.restoreAllMocks()

    localStorageMock = {}
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(
      (key: string) => localStorageMock[key] ?? null,
    )
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(
      (key: string, value: string) => { localStorageMock[key] = value },
    )
  })

  it('defaults to empty strings when localStorage is empty', () => {
    const store = useProfileStore()

    expect(store.firstName).toBe('')
    expect(store.lastName).toBe('')
  })

  it('loads stored names from localStorage on init', () => {
    localStorageMock['subly-profile-first-name'] = 'Max'
    localStorageMock['subly-profile-last-name'] = 'Mustermann'

    const store = useProfileStore()

    expect(store.firstName).toBe('Max')
    expect(store.lastName).toBe('Mustermann')
  })

  it('displays fallback text when no name is set', () => {
    const store = useProfileStore()

    expect(store.displayName).toBe('Mein Profil')
  })

  it('displays first name only when last name is missing', () => {
    localStorageMock['subly-profile-first-name'] = 'Max'
    const store = useProfileStore()

    expect(store.displayName).toBe('Max')
  })

  it('displays abbreviated last name when both names are set', () => {
    localStorageMock['subly-profile-first-name'] = 'Max'
    localStorageMock['subly-profile-last-name'] = 'Mustermann'
    const store = useProfileStore()

    expect(store.displayName).toBe('Max M.')
  })

  it('returns "?" as initials when no name is set', () => {
    const store = useProfileStore()

    expect(store.initials).toBe('?')
  })

  it('returns uppercase initials from first and last name', () => {
    localStorageMock['subly-profile-first-name'] = 'max'
    localStorageMock['subly-profile-last-name'] = 'mustermann'
    const store = useProfileStore()

    expect(store.initials).toBe('MM')
  })

  it('returns single initial when only first name is set', () => {
    localStorageMock['subly-profile-first-name'] = 'Max'
    const store = useProfileStore()

    expect(store.initials).toBe('M')
  })

  it('persists first name to localStorage when setName is called', async () => {
    const store = useProfileStore()

    store.setName('Anna', 'Schmidt')
    await nextTick()

    expect(localStorageMock['subly-profile-first-name']).toBe('Anna')
  })

  it('persists last name to localStorage when setName is called', async () => {
    const store = useProfileStore()

    store.setName('Anna', 'Schmidt')
    await nextTick()

    expect(localStorageMock['subly-profile-last-name']).toBe('Schmidt')
  })

  it('updates displayName reactively after setName', async () => {
    const store = useProfileStore()
    expect(store.displayName).toBe('Mein Profil')

    store.setName('Anna', 'Schmidt')
    await nextTick()

    expect(store.displayName).toBe('Anna S.')
  })

  it('updates initials reactively after setName', async () => {
    const store = useProfileStore()

    store.setName('Anna', 'Schmidt')
    await nextTick()

    expect(store.initials).toBe('AS')
  })
})
