import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import type { DashboardSummary, NewSubscriptionRequest, Subscription, SubscriptionStatus } from '../types/subscription'
import {
  createSubscription,
  deleteSubscription,
  fetchDashboardSummary,
  fetchSubscriptions,
  updateSubscriptionStatus,
} from '../api/subscriptionsApi'
import { buildDashboardSummary } from '../utils/subscriptionMath'

export const useSubscriptionStore = defineStore('subscriptions', () => {
  const subscriptions = ref<Subscription[]>([])
  const summary = ref<DashboardSummary | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)

  const activeSubscriptions = computed(() =>
    subscriptions.value.filter((subscription) => subscription.status === 'active'),
  )

  async function initialize(): Promise<void> {
    loading.value = true
    error.value = null

    try {
      const [subscriptionData, summaryData] = await Promise.all([fetchSubscriptions(), fetchDashboardSummary()])
      subscriptions.value = subscriptionData
      summary.value = summaryData
    } catch {
      error.value = 'Die Daten konnten nicht geladen werden.'
      summary.value = buildDashboardSummary(subscriptions.value)
    } finally {
      loading.value = false
    }
  }

  async function create(request: NewSubscriptionRequest): Promise<void> {
    const created = await createSubscription(request)
    subscriptions.value = [created, ...subscriptions.value]
    await refreshSummary()
  }

  async function updateStatus(id: string, status: SubscriptionStatus): Promise<void> {
    const updated = await updateSubscriptionStatus(id, status)
    subscriptions.value = subscriptions.value.map((subscription) => (subscription.id === id ? updated : subscription))
    await refreshSummary()
  }

  async function remove(id: string): Promise<void> {
    await deleteSubscription(id)
    subscriptions.value = subscriptions.value.filter((subscription) => subscription.id !== id)
    await refreshSummary()
  }

  async function refreshSummary(): Promise<void> {
    try {
      summary.value = await fetchDashboardSummary()
    } catch {
      summary.value = buildDashboardSummary(subscriptions.value)
    }
  }

  return {
    subscriptions,
    summary,
    loading,
    error,
    activeSubscriptions,
    initialize,
    create,
    updateStatus,
    remove,
  }
})
