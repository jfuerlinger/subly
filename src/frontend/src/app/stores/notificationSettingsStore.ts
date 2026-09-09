import { ref } from 'vue'
import { defineStore } from 'pinia'
import { fetchNotificationSettings, updateNotificationSettings } from '../api/notificationSettingsApi'

export const useNotificationSettingsStore = defineStore('notificationSettings', () => {
  const leadDays = ref(3)
  const emailEnabled = ref(false)
  const loading = ref(false)
  const error = ref<string | null>(null)

  async function initialize(): Promise<void> {
    loading.value = true
    error.value = null
    try {
      const settings = await fetchNotificationSettings()
      leadDays.value = settings.leadDays
      emailEnabled.value = settings.emailEnabled
    } catch {
      error.value = 'Die Benachrichtigungseinstellungen konnten nicht geladen werden.'
    } finally {
      loading.value = false
    }
  }

  async function save(newLeadDays: number, newEmailEnabled: boolean): Promise<void> {
    const settings = await updateNotificationSettings(newLeadDays, newEmailEnabled)
    leadDays.value = settings.leadDays
    emailEnabled.value = settings.emailEnabled
  }

  return { leadDays, emailEnabled, loading, error, initialize, save }
})
