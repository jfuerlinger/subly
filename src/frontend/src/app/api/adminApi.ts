import { apiClient } from './client'

export interface DatabaseResetResult {
  steps: string[]
  completedAtUtc: string
}

export async function deleteAllData(): Promise<void> {
  await apiClient.delete('/admin/data')
}

export async function seedData(): Promise<void> {
  await apiClient.post('/admin/seed')
}

export async function resetDatabase(): Promise<DatabaseResetResult> {
  const response = await apiClient.post<DatabaseResetResult>('/admin/reset-database')
  return response.data
}
