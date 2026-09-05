import { fireEvent, render, screen } from '@testing-library/vue'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it } from 'vitest'
import type { Subscription } from '../app/types/subscription'
import { useSubscriptionStore } from '../app/stores/subscriptionStore'
import { buildDashboardSummary } from '../app/utils/subscriptionMath'
import DashboardView from '../views/DashboardView.vue'

function toIsoDate(offsetDays: number): string {
  const date = new Date()
  date.setDate(date.getDate() + offsetDays)
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

const subscriptions: Subscription[] = [
  {
    id: 'sub-1',
    name: 'Netflix',
    vendor: 'Netflix',
    logoUrl: null,
    categoryId: 'cat-streaming',
    categoryName: 'streaming',
    price: 17.99,
    cycle: 'monthly',
    nextPaymentDate: toIsoDate(8),
    paymentMethod: 'Visa',
    status: 'active',
    autoRenew: true,
    startedAt: '2024-01-01',
    cancelledAt: null,
  },
  {
    id: 'sub-2',
    name: 'Spotify',
    vendor: 'Spotify',
    logoUrl: null,
    categoryId: 'cat-streaming',
    categoryName: 'streaming',
    price: 10.99,
    cycle: 'monthly',
    nextPaymentDate: toIsoDate(14),
    paymentMethod: 'PayPal',
    status: 'active',
    autoRenew: true,
    startedAt: '2024-02-01',
    cancelledAt: null,
  },
]

describe('DashboardView', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    const store = useSubscriptionStore()
    store.$patch({
      subscriptions,
      summary: buildDashboardSummary(subscriptions),
      error: null,
    })
  })

  it('filters dashboard subscriptions by search query', async () => {
    render(DashboardView)

    await fireEvent.update(screen.getByRole('textbox', { name: /abo suchen/i }), 'netflix')

    expect(screen.getByText('Netflix')).toBeInTheDocument()
    expect(screen.queryByText('Spotify')).not.toBeInTheDocument()
  })

  it('shows empty dashboard sections when search has no match', async () => {
    render(DashboardView)

    await fireEvent.update(screen.getByRole('textbox', { name: /abo suchen/i }), 'xyz')

    expect(screen.getByText('Keine aktiven Abonnements.')).toBeInTheDocument()
    expect(screen.getByText('Keine Zahlungen in den nächsten 30 Tagen.')).toBeInTheDocument()
  })
})
