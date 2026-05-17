import { describe, expect, it } from 'vitest'
import {
  buildCalendarPayments,
  buildDashboardSummary,
  buildPaymentForecast,
  buildPaymentMethodBreakdown,
  buildSpendingTrend,
  getPaymentDatesInRange,
  toDateKey,
  toMonthlyAmount,
  toYearlyAmount,
} from '../app/utils/subscriptionMath'
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
  cancelledAt: null,
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

describe('buildSpendingTrend', () => {
  it('includes only active subscriptions that started before each month end', () => {
    const today = new Date('2026-05-16')
    const subs: Subscription[] = [
      { ...sample, id: '1', startedAt: '2025-01-01', cycle: 'monthly', price: 10 },
      { ...sample, id: '2', startedAt: '2026-04-01', cycle: 'monthly', price: 20 },
      { ...sample, id: '3', startedAt: '2026-06-01', cycle: 'monthly', price: 50 },
    ]
    const trend = buildSpendingTrend(subs, 3, today)

    // 3 months ending at current: Mar, Apr, May 2026
    expect(trend).toHaveLength(3)
    // March: only sub 1 started (sub 2 starts Apr, sub 3 starts Jun)
    expect(trend[0].total).toBe(10)
    // April: sub 1 + sub 2
    expect(trend[1].total).toBe(30)
    // May: still sub 1 + sub 2 (sub 3 starts Jun)
    expect(trend[2].total).toBe(30)
  })

  it('excludes paused subscriptions', () => {
    const today = new Date('2026-05-16')
    const subs: Subscription[] = [
      { ...sample, id: '1', startedAt: '2025-01-01', status: 'active', cycle: 'monthly', price: 10 },
      { ...sample, id: '2', startedAt: '2025-01-01', status: 'paused', cycle: 'monthly', price: 50 },
    ]
    const trend = buildSpendingTrend(subs, 1, today)
    expect(trend[0].total).toBe(10)
  })

  it('keeps cancelled subscriptions in past months until their cancellation date', () => {
    const today = new Date('2026-05-16')
    const subs: Subscription[] = [
      { ...sample, id: '1', startedAt: '2026-01-01', status: 'cancelled', cancelledAt: '2026-04-15', cycle: 'monthly', price: 30 },
    ]

    const trend = buildSpendingTrend(subs, 3, today)

    // Mar: active, Apr: cancellation month still counted, May: no longer active.
    expect(trend[0].total).toBe(30)
    expect(trend[1].total).toBe(30)
    expect(trend[2].total).toBe(0)
  })
})

describe('buildPaymentForecast', () => {
  it('includes monthly subscriptions in every month starting from nextPaymentDate', () => {
    const today = new Date('2026-05-16')
    const subs: Subscription[] = [
      { ...sample, id: '1', cycle: 'monthly', price: 15, nextPaymentDate: '2026-05-20' },
      { ...sample, id: '2', cycle: 'monthly', price: 10, nextPaymentDate: '2026-07-01' },
    ]
    const forecast = buildPaymentForecast(subs, 3, today)

    // May (month 0): sub 1 started May, sub 2 starts Jul → only sub 1
    expect(forecast[0].total).toBe(15)
    // June: sub 1 pays (Jun >= May), sub 2 not yet
    expect(forecast[1].total).toBe(15)
    // July: both pay
    expect(forecast[2].total).toBe(25)
  })

  it('includes yearly subscriptions only in the month of their payment', () => {
    const today = new Date('2026-05-16')
    const subs: Subscription[] = [
      { ...sample, id: '1', cycle: 'yearly', price: 120, nextPaymentDate: '2026-06-15' },
    ]
    const forecast = buildPaymentForecast(subs, 3, today)

    expect(forecast[0].total).toBe(0)  // May – no yearly payment
    expect(forecast[1].total).toBe(120) // June – yearly payment
    expect(forecast[2].total).toBe(0)  // July
  })

  it('stops forecasting monthly payments after cancellation date', () => {
    const today = new Date('2026-05-16')
    const subs: Subscription[] = [
      {
        ...sample,
        id: '1',
        cycle: 'monthly',
        price: 20,
        nextPaymentDate: '2026-05-20',
        status: 'cancelled',
        cancelledAt: '2026-06-10',
      },
    ]

    const forecast = buildPaymentForecast(subs, 3, today)

    // May payment stays, June payment already after cancellation day, July also excluded.
    expect(forecast[0].total).toBe(20)
    expect(forecast[1].total).toBe(0)
    expect(forecast[2].total).toBe(0)
  })
})

describe('buildPaymentMethodBreakdown', () => {
  it('groups active subscriptions by payment method and sums monthly costs', () => {
    const subs: Subscription[] = [
      { ...sample, id: '1', paymentMethod: 'Visa', cycle: 'monthly', price: 10 },
      { ...sample, id: '2', paymentMethod: 'Visa', cycle: 'monthly', price: 20 },
      { ...sample, id: '3', paymentMethod: 'PayPal', cycle: 'monthly', price: 5 },
      { ...sample, id: '4', paymentMethod: 'Visa', status: 'paused', cycle: 'monthly', price: 100 },
    ]
    const result = buildPaymentMethodBreakdown(subs)

    expect(result).toHaveLength(2)
    expect(result[0]).toMatchObject({ method: 'Visa', total: 30, count: 2 })
    expect(result[1]).toMatchObject({ method: 'PayPal', total: 5, count: 1 })
  })
})

describe('toDateKey', () => {
  it('formats a date as YYYY-MM-DD using local time', () => {
    expect(toDateKey(new Date(2026, 4, 7))).toBe('2026-05-07') // month is 0-based
    expect(toDateKey(new Date(2026, 11, 31))).toBe('2026-12-31')
  })
})

describe('getPaymentDatesInRange', () => {
  const base: Subscription = {
    ...sample,
    id: 'sub1',
    cycle: 'monthly',
    price: 10,
    startedAt: '2026-01-01',
    nextPaymentDate: '2026-05-15',
    cancelledAt: null,
    status: 'active',
  }

  it('returns monthly payment dates within the range', () => {
    const start = new Date(2026, 4, 1)  // 2026-05-01
    const end = new Date(2026, 6, 31)   // 2026-07-31
    const dates = getPaymentDatesInRange(base, start, end)
    expect(dates.map(toDateKey)).toEqual(['2026-05-15', '2026-06-15', '2026-07-15'])
  })

  it('returns past monthly payment dates using startedAt as lower bound', () => {
    const start = new Date(2026, 1, 1)  // 2026-02-01
    const end = new Date(2026, 3, 30)   // 2026-04-30
    const dates = getPaymentDatesInRange(base, start, end)
    expect(dates.map(toDateKey)).toEqual(['2026-02-15', '2026-03-15', '2026-04-15'])
  })

  it('does not return dates before startedAt', () => {
    const sub = { ...base, startedAt: '2026-03-01' }
    const start = new Date(2026, 0, 1)  // 2026-01-01
    const end = new Date(2026, 4, 31)   // 2026-05-31
    const dates = getPaymentDatesInRange(sub, start, end)
    // Jan and Feb are before startedAt (2026-03-01), so only Mar, Apr, May
    expect(dates.map(toDateKey)).toEqual(['2026-03-15', '2026-04-15', '2026-05-15'])
  })

  it('excludes dates after cancelledAt', () => {
    const sub = { ...base, status: 'cancelled' as const, cancelledAt: '2026-06-10' }
    const start = new Date(2026, 4, 1)
    const end = new Date(2026, 6, 31)
    const dates = getPaymentDatesInRange(sub, start, end)
    // June 15 is after cancelledAt June 10, July 15 is after too → only May
    expect(dates.map(toDateKey)).toEqual(['2026-05-15'])
  })

  it('returns empty array for paused subscriptions', () => {
    const sub = { ...base, status: 'paused' as const }
    const dates = getPaymentDatesInRange(sub, new Date(2026, 4, 1), new Date(2026, 6, 31))
    expect(dates).toHaveLength(0)
  })

  it('handles months with fewer days than the payment day (clamps to last day)', () => {
    const sub = { ...base, nextPaymentDate: '2026-05-31' }
    const start = new Date(2026, 1, 1)  // 2026-02
    const end = new Date(2026, 1, 28)
    const dates = getPaymentDatesInRange(sub, start, end)
    // Feb has 28 days in 2026 → payment clamped to 28th
    expect(dates.map(toDateKey)).toEqual(['2026-02-28'])
  })

  it('returns yearly payment dates in range', () => {
    const sub = { ...base, cycle: 'yearly' as const, nextPaymentDate: '2026-05-15' }
    const start = new Date(2025, 0, 1)
    const end = new Date(2027, 11, 31)
    const dates = getPaymentDatesInRange(sub, start, end)
    // startedAt is 2026-01-01, so 2025-05-15 is before startedAt → skip; 2026 and 2027 are in range
    expect(dates.map(toDateKey)).toEqual(['2026-05-15', '2027-05-15'])
  })
})

describe('buildCalendarPayments', () => {
  it('groups multiple subscriptions by date key', () => {
    const sub1: Subscription = { ...sample, id: '1', cycle: 'monthly', nextPaymentDate: '2026-05-15', startedAt: '2026-01-01', cancelledAt: null, status: 'active', price: 10 }
    const sub2: Subscription = { ...sample, id: '2', cycle: 'monthly', nextPaymentDate: '2026-05-15', startedAt: '2026-01-01', cancelledAt: null, status: 'active', price: 20 }
    const sub3: Subscription = { ...sample, id: '3', cycle: 'monthly', nextPaymentDate: '2026-05-20', startedAt: '2026-01-01', cancelledAt: null, status: 'active', price: 30 }

    const start = new Date(2026, 4, 1)
    const end = new Date(2026, 4, 31)
    const result = buildCalendarPayments([sub1, sub2, sub3], start, end)

    expect(result.size).toBe(2)
    expect(result.get('2026-05-15')).toHaveLength(2)
    expect(result.get('2026-05-20')).toHaveLength(1)
    expect(result.get('2026-05-20')![0].amount).toBe(30)
  })

  it('returns an empty map when there are no active subscriptions', () => {
    const sub: Subscription = { ...sample, id: '1', status: 'paused' }
    const result = buildCalendarPayments([sub], new Date(2026, 4, 1), new Date(2026, 4, 31))
    expect(result.size).toBe(0)
  })
})
