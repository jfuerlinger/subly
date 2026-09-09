import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useNotificationSettingsStore } from '../app/stores/notificationSettingsStore'
import * as notificationSettingsApi from '../app/api/notificationSettingsApi'

describe('useNotificationSettingsStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.restoreAllMocks()
  })

  it('initializes with defaults', () => {
    const store = useNotificationSettingsStore()

    expect(store.leadDays).toBe(3)
    expect(store.emailEnabled).toBe(false)
    expect(store.loading).toBe(false)
    expect(store.error).toBeNull()
  })

  it('loads settings on initialize', async () => {
    vi.spyOn(notificationSettingsApi, 'fetchNotificationSettings').mockResolvedValue({ leadDays: 5, emailEnabled: true })
    const store = useNotificationSettingsStore()

    await store.initialize()

    expect(store.leadDays).toBe(5)
    expect(store.emailEnabled).toBe(true)
    expect(store.error).toBeNull()
  })

  it('sets error when initialize fails', async () => {
    vi.spyOn(notificationSettingsApi, 'fetchNotificationSettings').mockRejectedValue(new Error('Network error'))
    const store = useNotificationSettingsStore()

    await store.initialize()

    expect(store.error).toBeTruthy()
  })

  it('saves settings and updates state', async () => {
    const update = vi.spyOn(notificationSettingsApi, 'updateNotificationSettings').mockResolvedValue({ leadDays: 10, emailEnabled: true })
    const store = useNotificationSettingsStore()

    await store.save(10, true)

    expect(update).toHaveBeenCalledWith(10, true)
    expect(store.leadDays).toBe(10)
    expect(store.emailEnabled).toBe(true)
  })
})
