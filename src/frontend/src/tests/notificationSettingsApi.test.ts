import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { AxiosResponse } from 'axios'
import { apiClient } from '../app/api/client'
import {
  fetchNotificationSettings,
  updateNotificationSettings,
  type NotificationSettingsDto,
} from '../app/api/notificationSettingsApi'

describe('notificationSettingsApi', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
  })

  it('fetches notification settings', async () => {
    const payload: NotificationSettingsDto = { leadDays: 3, emailEnabled: false }
    vi.spyOn(apiClient, 'get').mockResolvedValue({ data: payload } as AxiosResponse<NotificationSettingsDto>)

    const result = await fetchNotificationSettings()

    expect(result).toEqual(payload)
    expect(apiClient.get).toHaveBeenCalledWith('/notification-settings')
  })

  it('updates notification settings', async () => {
    const updated: NotificationSettingsDto = { leadDays: 7, emailEnabled: true }
    vi.spyOn(apiClient, 'put').mockResolvedValue({ data: updated } as AxiosResponse<NotificationSettingsDto>)

    const result = await updateNotificationSettings(7, true)

    expect(result).toEqual(updated)
    expect(apiClient.put).toHaveBeenCalledWith('/notification-settings', { leadDays: 7, emailEnabled: true })
  })
})
