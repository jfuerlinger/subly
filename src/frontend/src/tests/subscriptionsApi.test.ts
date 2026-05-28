import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { AxiosResponse } from 'axios'
import { apiClient } from '../app/api/client'
import {
  createSubscription,
  deleteSubscription,
  fetchDashboardSummary,
  fetchLogoSuggestions,
  fetchSubscriptions,
  updateSubscriptionStatus,
} from '../app/api/subscriptionsApi'
import type { DashboardSummary, NewSubscriptionRequest, Subscription } from '../app/types/subscription'

describe('subscriptionsApi', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
  })

  it('fetches subscriptions', async () => {
    const payload: Subscription[] = []
    vi.spyOn(apiClient, 'get').mockResolvedValue({ data: payload } as AxiosResponse<Subscription[]>)

    const result = await fetchSubscriptions()

    expect(result).toEqual(payload)
  })

  it('creates subscription', async () => {
    const request: NewSubscriptionRequest = {
      name: 'ChatGPT Plus',
      vendor: 'OpenAI',
      logoUrl: null,
      category: 'software',
      price: 22,
      cycle: 'monthly',
      nextPaymentDate: '2026-05-20',
      paymentMethod: 'Visa',
      startedAt: '2026-04-01',
      cancelledAt: null,
    }
    const responsePayload = { id: 'new-id', ...request, status: 'active', autoRenew: true, startedAt: '2026-05-16' } as Subscription
    vi.spyOn(apiClient, 'post').mockResolvedValue({ data: responsePayload } as AxiosResponse<Subscription>)

    const result = await createSubscription(request)

    expect(result.id).toBe('new-id')
  })

  it('updates subscription status', async () => {
    const responsePayload = {
      id: 'sub-1',
      name: 'Notion',
      vendor: 'Notion',
      logoUrl: null,
      category: 'software',
      price: 10,
      cycle: 'monthly',
      nextPaymentDate: '2026-05-20',
      paymentMethod: 'PayPal',
      status: 'paused',
      autoRenew: true,
      startedAt: '2026-01-01',
      cancelledAt: null,
    } as Subscription
    vi.spyOn(apiClient, 'patch').mockResolvedValue({ data: responsePayload } as AxiosResponse<Subscription>)

    const result = await updateSubscriptionStatus('sub-1', 'paused')

    expect(result.status).toBe('paused')
  })

  it('fetches dashboard summary', async () => {
    const payload: DashboardSummary = {
      monthlyTotal: 50,
      yearlyTotal: 600,
      activeSubscriptionsCount: 4,
      upcomingPaymentsTotal30Days: 70,
      upcomingPaymentsCount30Days: 2,
    }
    vi.spyOn(apiClient, 'get').mockResolvedValue({ data: payload } as AxiosResponse<DashboardSummary>)

    const result = await fetchDashboardSummary()

    expect(result.monthlyTotal).toBe(50)
  })

  it('deletes subscription', async () => {
    vi.spyOn(apiClient, 'delete').mockResolvedValue({} as AxiosResponse<void>)

    await expect(deleteSubscription('sub-1')).resolves.toBeUndefined()
  })

  it('fetches logo suggestions', async () => {
    vi.spyOn(apiClient, 'get').mockResolvedValue({
      data: [{ provider: 'Clearbit', domain: 'netflix.com', logoUrl: 'https://logo.clearbit.com/netflix.com' }],
    } as AxiosResponse)

    const result = await fetchLogoSuggestions('Netflix')

    expect(result).toHaveLength(1)
    expect(result[0].domain).toBe('netflix.com')
  })
})
