import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { AxiosResponse } from 'axios'
import { apiClient } from '../app/api/client'
import { createCategory, fetchCategories, type CategoryDto } from '../app/api/categoriesApi'

describe('categoriesApi', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
  })

  it('fetches categories', async () => {
    const payload: CategoryDto[] = [
      { id: '1', name: 'streaming' },
      { id: '2', name: 'software' },
    ]
    vi.spyOn(apiClient, 'get').mockResolvedValue({ data: payload } as AxiosResponse<CategoryDto[]>)

    const result = await fetchCategories()

    expect(result).toEqual(payload)
    expect(apiClient.get).toHaveBeenCalledWith('/categories')
  })

  it('creates a new category', async () => {
    const created: CategoryDto = { id: 'new-id', name: 'gaming' }
    vi.spyOn(apiClient, 'post').mockResolvedValue({ data: created } as AxiosResponse<CategoryDto>)

    const result = await createCategory('gaming')

    expect(result.name).toBe('gaming')
    expect(apiClient.post).toHaveBeenCalledWith('/categories', { name: 'gaming' })
  })
})
