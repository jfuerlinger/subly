import { apiClient } from './client'
import type { DashboardSummary, NewSubscriptionRequest, Subscription, SubscriptionStatus } from '../types/subscription'

export async function fetchSubscriptions(): Promise<Subscription[]> {
  const response = await apiClient.get<Subscription[]>('/subscriptions')
  return response.data
}

export async function fetchDashboardSummary(): Promise<DashboardSummary> {
  const response = await apiClient.get<DashboardSummary>('/dashboard/summary')
  return response.data
}

export async function createSubscription(request: NewSubscriptionRequest): Promise<Subscription> {
  const response = await apiClient.post<Subscription>('/subscriptions', request)
  return response.data
}

export async function updateSubscriptionStatus(
  id: string,
  status: SubscriptionStatus,
  cancelledAt?: string | null,
): Promise<Subscription> {
  const response = await apiClient.patch<Subscription>(`/subscriptions/${id}/status`, { status, cancelledAt })
  return response.data
}

export async function deleteSubscription(id: string): Promise<void> {
  await apiClient.delete(`/subscriptions/${id}`)
}
