import { describe, expect, it } from 'vitest'
import { buildDashboardSummary, toMonthlyAmount, toYearlyAmount } from '../app/utils/subscriptionMath'
import type { Subscription } from '../app/types/subscription'

const sample: Subscription = {
  id: '1',
  name: 'Prime',
  vendor: 'Amazon',
  category: 'membership',
  price: 120,
  cycle: 'yearly',
  nextPaymentDate: '2026-05-20',
  paymentMethod: 'Visa',
  status: 'active',
  autoRenew: true,
  startedAt: '2025-01-01',
}

describe('subscriptionMath', () => {
  it('calculates monthly amount from yearly cycle', () => {
    expect(toMonthlyAmount(sample)).toBe(10)
  })

  it('calculates yearly amount from monthly cycle', () => {
    expect(toYearlyAmount({ ...sample, cycle: 'monthly', price: 10 })).toBe(120)
  })

  it('builds dashboard summary from active subscriptions only', () => {
    const summary = buildDashboardSummary(
      [
        sample,
        { ...sample, id: '2', cycle: 'monthly', price: 15, nextPaymentDate: '2026-06-10' },
        { ...sample, id: '3', status: 'paused', price: 50, cycle: 'monthly' },
      ],
      new Date('2026-05-16T10:00:00Z'),
    )

    expect(summary.activeSubscriptionsCount).toBe(2)
    expect(summary.monthlyTotal).toBe(25)
    expect(summary.yearlyTotal).toBe(300)
    expect(summary.upcomingPaymentsCount30Days).toBe(2)
    expect(summary.upcomingPaymentsTotal30Days).toBe(135)
  })
})
