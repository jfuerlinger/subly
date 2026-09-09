import { apiClient } from './client'

export interface NotificationSettingsDto {
  leadDays: number
  emailEnabled: boolean
}

export async function fetchNotificationSettings(): Promise<NotificationSettingsDto> {
  const response = await apiClient.get<NotificationSettingsDto>('/notification-settings')
  return response.data
}

export async function updateNotificationSettings(
  leadDays: number,
  emailEnabled: boolean,
): Promise<NotificationSettingsDto> {
  const response = await apiClient.put<NotificationSettingsDto>('/notification-settings', {
    leadDays,
    emailEnabled,
  })
  return response.data
}
