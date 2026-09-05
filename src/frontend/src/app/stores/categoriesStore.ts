import { ref } from 'vue'
import { defineStore } from 'pinia'
import type { CategoryDto } from '../api/categoriesApi'
import { createCategory, deleteCategory, fetchCategories, renameCategory } from '../api/categoriesApi'

export const useCategoryStore = defineStore('categories', () => {
  const categories = ref<CategoryDto[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  async function initialize(): Promise<void> {
    loading.value = true
    error.value = null
    try {
      categories.value = await fetchCategories()
    } catch {
      error.value = 'Die Kategorien konnten nicht geladen werden.'
    } finally {
      loading.value = false
    }
  }

  async function create(name: string): Promise<void> {
    const created = await createCategory(name)
    categories.value = [...categories.value, created].sort((a, b) =>
      a.name.localeCompare(b.name),
    )
  }

  async function rename(id: string, newName: string): Promise<void> {
    const updated = await renameCategory(id, newName)
    categories.value = categories.value
      .map((c) => (c.id === id ? updated : c))
      .sort((a, b) => a.name.localeCompare(b.name))
  }

  async function remove(id: string, replacementCategoryId?: string): Promise<void> {
    await deleteCategory(id, replacementCategoryId)
    categories.value = categories.value.filter((category) => category.id !== id)
  }

  return { categories, loading, error, initialize, create, rename, remove }
})
