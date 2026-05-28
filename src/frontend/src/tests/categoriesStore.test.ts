import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useCategoryStore } from '../app/stores/categoriesStore'
import * as categoriesApi from '../app/api/categoriesApi'
import type { CategoryDto } from '../app/api/categoriesApi'

const cat1: CategoryDto = { id: '1', name: 'software' }
const cat2: CategoryDto = { id: '2', name: 'streaming' }

describe('useCategoryStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.restoreAllMocks()
  })

  it('initializes with empty state', () => {
    const store = useCategoryStore()
    expect(store.categories).toEqual([])
    expect(store.loading).toBe(false)
    expect(store.error).toBeNull()
  })

  it('loads categories on initialize', async () => {
    vi.spyOn(categoriesApi, 'fetchCategories').mockResolvedValue([cat1, cat2])
    const store = useCategoryStore()

    await store.initialize()

    expect(store.categories).toEqual([cat1, cat2])
    expect(store.loading).toBe(false)
    expect(store.error).toBeNull()
  })

  it('sets error when initialize fails', async () => {
    vi.spyOn(categoriesApi, 'fetchCategories').mockRejectedValue(new Error('Network error'))
    const store = useCategoryStore()

    await store.initialize()

    expect(store.error).toBeTruthy()
    expect(store.categories).toEqual([])
  })

  it('adds and sorts category on create', async () => {
    const newCat: CategoryDto = { id: '3', name: 'gaming' }
    vi.spyOn(categoriesApi, 'fetchCategories').mockResolvedValue([cat1, cat2])
    vi.spyOn(categoriesApi, 'createCategory').mockResolvedValue(newCat)
    const store = useCategoryStore()
    await store.initialize()

    await store.create('gaming')

    expect(store.categories.map((c) => c.name)).toEqual(['gaming', 'software', 'streaming'])
  })

  it('updates category name on rename', async () => {
    const renamed: CategoryDto = { id: '1', name: 'tools' }
    vi.spyOn(categoriesApi, 'fetchCategories').mockResolvedValue([cat1, cat2])
    vi.spyOn(categoriesApi, 'renameCategory').mockResolvedValue(renamed)
    const store = useCategoryStore()
    await store.initialize()

    await store.rename('1', 'tools')

    const names = store.categories.map((c) => c.name)
    expect(names).toContain('tools')
    expect(names).not.toContain('software')
  })
})
