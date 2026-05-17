import { apiClient } from './client'

export async function deleteAllData(): Promise<void> {
  await apiClient.delete('/admin/data')
}

export async function seedData(): Promise<void> {
  await apiClient.post('/admin/seed')
}
