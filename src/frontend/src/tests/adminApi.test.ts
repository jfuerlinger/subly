import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { AxiosResponse } from 'axios'
import { apiClient } from '../app/api/client'
import { deleteAllData, resetDatabase, seedData } from '../app/api/adminApi'

describe('adminApi', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
  })

  it('deleteAllData calls DELETE /admin/data', async () => {
    vi.spyOn(apiClient, 'delete').mockResolvedValue({} as AxiosResponse<void>)

    await deleteAllData()

    expect(apiClient.delete).toHaveBeenCalledWith('/admin/data')
  })

  it('seedData calls POST /admin/seed', async () => {
    vi.spyOn(apiClient, 'post').mockResolvedValue({} as AxiosResponse<void>)

    await seedData()

    expect(apiClient.post).toHaveBeenCalledWith('/admin/seed')
  })

  it('resetDatabase calls POST /admin/reset-database', async () => {
    vi.spyOn(apiClient, 'post').mockResolvedValue({
      data: {
        steps: [],
        completedAtUtc: '2026-01-01T00:00:00Z',
      },
    } as AxiosResponse)

    await resetDatabase()

    expect(apiClient.post).toHaveBeenCalledWith('/admin/reset-database')
  })
})
