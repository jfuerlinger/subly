import { apiClient } from './client'

export interface CategoryDto {
  id: string
  name: string
}

export async function fetchCategories(): Promise<CategoryDto[]> {
  const response = await apiClient.get<CategoryDto[]>('/categories')
  return response.data
}

export async function createCategory(name: string): Promise<CategoryDto> {
  const response = await apiClient.post<CategoryDto>('/categories', { name })
  return response.data
}
