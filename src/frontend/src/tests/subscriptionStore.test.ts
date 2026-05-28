import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import type { DashboardSummary, NewSubscriptionRequest, Subscription, UpdateSubscriptionRequest } from '../app/types/subscription'
import { useSubscriptionStore } from '../app/stores/subscriptionStore'
import * as api from '../app/api/subscriptionsApi'

const baseSubscription: Subscription = {
  id: 'sub-1',
  name: 'Netflix',
  vendor: 'Netflix',
  logoUrl: null,
  category: 'streaming',
  price: 17.99,
  cycle: 'monthly',
  nextPaymentDate: '2026-05-20',
  paymentMethod: 'Visa',
  status: 'active',
  autoRenew: true,
  startedAt: '2023-01-01',
  cancelledAt: null,
}

const dashboard: DashboardSummary = {
  monthlyTotal: 17.99,
  yearlyTotal: 215.88,
  activeSubscriptionsCount: 1,
  upcomingPaymentsTotal30Days: 17.99,
  upcomingPaymentsCount30Days: 1,
}

describe('subscriptionStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.restoreAllMocks()
  })

  it('loads subscriptions and summary', async () => {
    vi.spyOn(api, 'fetchSubscriptions').mockResolvedValue([baseSubscription])
    vi.spyOn(api, 'fetchDashboardSummary').mockResolvedValue(dashboard)
    const store = useSubscriptionStore()

    await store.initialize()

    expect(store.subscriptions).toHaveLength(1)
    expect(store.summary?.monthlyTotal).toBe(17.99)
  })

  it('creates and prepends new subscription', async () => {
    const store = useSubscriptionStore()
    const request: NewSubscriptionRequest = {
      name: 'ChatGPT Plus',
      vendor: 'OpenAI',
      logoUrl: null,
      category: 'software',
      price: 22,
      cycle: 'monthly',
      nextPaymentDate: '2026-05-18',
      paymentMethod: 'Visa',
      startedAt: '2026-04-01',
      cancelledAt: null,
    }
    const created: Subscription = {
      id: 'new-id',
      ...request,
      status: 'active',
      autoRenew: true,
      startedAt: '2026-05-16',
      cancelledAt: null,
    }
    vi.spyOn(api, 'createSubscription').mockResolvedValue(created)

    await store.create(request)

    expect(store.subscriptions[0].id).toBe('new-id')
  })

  it('updates status in local state', async () => {
    const store = useSubscriptionStore()
    store.$patch({ subscriptions: [baseSubscription] })
    const updated: Subscription = { ...baseSubscription, status: 'paused' }
    vi.spyOn(api, 'updateSubscriptionStatus').mockResolvedValue(updated)

    await store.updateStatus(baseSubscription.id, 'paused')

    expect(store.subscriptions[0].status).toBe('paused')
  })

  it('updates subscription details in local state', async () => {
    const store = useSubscriptionStore()
    store.$patch({ subscriptions: [baseSubscription] })
    const request: UpdateSubscriptionRequest = {
      name: 'Netflix Premium',
      vendor: 'Netflix Inc.',
      logoUrl: null,
      category: 'streaming',
      price: 19.99,
      cycle: 'monthly',
      nextPaymentDate: '2026-06-01',
      paymentMethod: 'Visa',
      startedAt: '2023-01-01',
      cancelledAt: null,
    }
    const updated: Subscription = { ...baseSubscription, ...request }
    vi.spyOn(api, 'updateSubscription').mockResolvedValue(updated)

    await store.update(baseSubscription.id, request)

    expect(store.subscriptions[0].name).toBe('Netflix Premium')
    expect(store.subscriptions[0].price).toBe(19.99)
  })

  it('deletes subscription from local state', async () => {
    const store = useSubscriptionStore()
    store.$patch({ subscriptions: [baseSubscription] })
    vi.spyOn(api, 'deleteSubscription').mockResolvedValue()

    await store.remove(baseSubscription.id)

    expect(store.subscriptions).toHaveLength(0)
  })
})
