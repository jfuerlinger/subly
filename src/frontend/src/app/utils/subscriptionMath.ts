import type { DashboardSummary, Subscription } from '../types/subscription'

export function toMonthlyAmount(subscription: Subscription): number {
  return subscription.cycle === 'yearly' ? subscription.price / 12 : subscription.price
}

export function toYearlyAmount(subscription: Subscription): number {
  return subscription.cycle === 'yearly' ? subscription.price : subscription.price * 12
}

export function buildDashboardSummary(subscriptions: Subscription[], today = new Date()): DashboardSummary {
  const active = subscriptions.filter((subscription) => subscription.status === 'active')
  const endDate = new Date(today)
  endDate.setDate(endDate.getDate() + 30)

  const upcoming = active.filter((subscription) => {
    const paymentDate = new Date(subscription.nextPaymentDate)
    return paymentDate >= today && paymentDate <= endDate
  })

  return {
    monthlyTotal: round(active.reduce((sum, current) => sum + toMonthlyAmount(current), 0)),
    yearlyTotal: round(active.reduce((sum, current) => sum + toYearlyAmount(current), 0)),
    activeSubscriptionsCount: active.length,
    upcomingPaymentsTotal30Days: round(upcoming.reduce((sum, current) => sum + current.price, 0)),
    upcomingPaymentsCount30Days: upcoming.length,
  }
}

// ─── Analytics helpers ─────────────────────────────────────────────────────

export interface SpendingTrendPoint {
  label: string
  monthKey: string
  total: number
}

/**
 * Returns the cumulative monthly cost of all currently-active subscriptions
 * for each of the last `monthCount` months. Uses `startedAt` to determine
 * when a subscription first contributed to the running total.
 */
export function buildSpendingTrend(
  subscriptions: Subscription[],
  monthCount = 12,
  today = new Date(),
): SpendingTrendPoint[] {
  return Array.from({ length: monthCount }, (_, i) => {
    const offset = monthCount - 1 - i
    const d = new Date(today.getFullYear(), today.getMonth() - offset, 1)
    const monthEnd = new Date(d.getFullYear(), d.getMonth() + 1, 0)
    const monthKey = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`
    const label =
      d.getMonth() === 0
        ? d.toLocaleString('de-DE', { month: 'short', year: '2-digit' })
        : d.toLocaleString('de-DE', { month: 'short' })

    const total = round(
      subscriptions
        .filter((s) => s.status === 'active' && new Date(s.startedAt) <= monthEnd)
        .reduce((sum, s) => sum + toMonthlyAmount(s), 0),
    )

    return { label, monthKey, total }
  })
}

export interface ForecastPoint {
  label: string
  monthKey: string
  total: number
}

/**
 * Returns the expected total payments for each of the next `monthCount` months.
 * Monthly subscriptions contribute every month from `nextPaymentDate` onwards.
 * Yearly subscriptions contribute only in the month their annual payment falls.
 */
export function buildPaymentForecast(
  subscriptions: Subscription[],
  monthCount = 12,
  today = new Date(),
): ForecastPoint[] {
  const active = subscriptions.filter((s) => s.status === 'active')

  return Array.from({ length: monthCount }, (_, i) => {
    const d = new Date(today.getFullYear(), today.getMonth() + i, 1)
    const monthStart = new Date(d.getFullYear(), d.getMonth(), 1)
    const monthEnd = new Date(d.getFullYear(), d.getMonth() + 1, 0)
    const monthKey = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`
    const label =
      d.getMonth() === 0
        ? d.toLocaleString('de-DE', { month: 'short', year: '2-digit' })
        : d.toLocaleString('de-DE', { month: 'short' })

    let total = 0
    for (const s of active) {
      if (s.cycle === 'monthly') {
        // Monthly subscriptions pay every month starting from the month of nextPaymentDate.
        const nextD = new Date(s.nextPaymentDate)
        const nextMonthStart = new Date(nextD.getFullYear(), nextD.getMonth(), 1)
        if (monthStart >= nextMonthStart) {
          total += s.price
        }
      } else {
        // Yearly subscriptions pay only when the annual date falls in this month.
        const next = new Date(s.nextPaymentDate)
        const nextPlusYear = new Date(next.getFullYear() + 1, next.getMonth(), next.getDate())
        if (
          (next >= monthStart && next <= monthEnd) ||
          (nextPlusYear >= monthStart && nextPlusYear <= monthEnd)
        ) {
          total += s.price
        }
      }
    }

    return { label, monthKey, total: round(total) }
  })
}

export interface PaymentMethodBreakdownItem {
  method: string
  total: number
  count: number
}

/** Groups active subscriptions by payment method and sums their monthly costs. */
export function buildPaymentMethodBreakdown(subscriptions: Subscription[]): PaymentMethodBreakdownItem[] {
  const active = subscriptions.filter((s) => s.status === 'active')
  const map = new Map<string, { total: number; count: number }>()

  for (const s of active) {
    const key = s.paymentMethod?.trim() || 'Unbekannt'
    const existing = map.get(key) ?? { total: 0, count: 0 }
    map.set(key, { total: existing.total + toMonthlyAmount(s), count: existing.count + 1 })
  }

  return Array.from(map.entries())
    .map(([method, data]) => ({ method, total: round(data.total), count: data.count }))
    .sort((a, b) => b.total - a.total)
}

function round(value: number): number {
  return Math.round((value + Number.EPSILON) * 100) / 100
}
