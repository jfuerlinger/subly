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
 * Returns the cumulative monthly cost for each of the last `monthCount` months.
 * Uses `startedAt` and `cancelledAt` to determine if a subscription was active
 * in the respective month.
 */
export function buildSpendingTrend(
  subscriptions: Subscription[],
  monthCount = 12,
  today = new Date(),
): SpendingTrendPoint[] {
  return Array.from({ length: monthCount }, (_, i) => {
    const offset = monthCount - 1 - i
    const d = new Date(today.getFullYear(), today.getMonth() - offset, 1)
    const monthStart = new Date(d.getFullYear(), d.getMonth(), 1)
    const monthEnd = new Date(d.getFullYear(), d.getMonth() + 1, 0)
    const monthKey = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`
    const label =
      d.getMonth() === 0
        ? d.toLocaleString('de-DE', { month: 'short', year: '2-digit' })
        : d.toLocaleString('de-DE', { month: 'short' })

    const total = round(
      subscriptions
        .filter((s) => {
          if (s.status === 'paused') {
            return false
          }

          if (toDate(s.startedAt) > monthEnd) {
            return false
          }

          if (!s.cancelledAt) {
            return true
          }

          return toDate(s.cancelledAt) >= monthStart
        })
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
  const relevantSubscriptions = subscriptions.filter((s) => s.status !== 'paused')

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
    for (const s of relevantSubscriptions) {
      const cancelledAt = s.cancelledAt ? toDate(s.cancelledAt) : null
      if (cancelledAt && cancelledAt < monthStart) {
        continue
      }

      if (s.cycle === 'monthly') {
        // Monthly subscriptions pay every month starting from the month of nextPaymentDate.
        const nextD = toDate(s.nextPaymentDate)
        const nextMonthStart = new Date(nextD.getFullYear(), nextD.getMonth(), 1)
        if (monthStart >= nextMonthStart) {
          const paymentDate = paymentDateInMonth(nextD, monthStart)
          if (paymentDate >= nextD && (!cancelledAt || paymentDate <= cancelledAt)) {
            total += s.price
          }
        }
      } else {
        // Yearly subscriptions pay only when the annual date falls in this month.
        const next = toDate(s.nextPaymentDate)
        const nextPlusYear = new Date(next.getFullYear() + 1, next.getMonth(), next.getDate())
        let paymentDate: Date | null = null

        if (next >= monthStart && next <= monthEnd) {
          paymentDate = next
        } else if (nextPlusYear >= monthStart && nextPlusYear <= monthEnd) {
          paymentDate = nextPlusYear
        }

        if (paymentDate && (!cancelledAt || paymentDate <= cancelledAt)) {
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

// ─── Calendar helpers ──────────────────────────────────────────────────────

export interface CalendarPaymentEntry {
  subscription: Subscription
  date: Date
  amount: number
}

/** Converts a local Date to an ISO date string key ("YYYY-MM-DD"). */
export function toDateKey(date: Date): string {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`
}

/**
 * Returns all dates within [rangeStart, rangeEnd] on which a subscription has a payment.
 * Paused subscriptions are excluded. Cancelled subscriptions are included up to their
 * cancellation date. The payment day-of-month is derived from nextPaymentDate; the
 * subscription's startedAt is used as the earliest possible payment date.
 */
export function getPaymentDatesInRange(
  subscription: Subscription,
  rangeStart: Date,
  rangeEnd: Date,
): Date[] {
  if (subscription.status === 'paused') return []

  const startedAt = toDate(subscription.startedAt)
  const cancelledAt = subscription.cancelledAt ? toDate(subscription.cancelledAt) : null
  const nextPayment = toDate(subscription.nextPaymentDate)
  const effectiveEnd = cancelledAt ?? rangeEnd

  const dates: Date[] = []

  if (subscription.cycle === 'monthly') {
    const cursor = new Date(rangeStart.getFullYear(), rangeStart.getMonth(), 1)
    while (cursor <= rangeEnd) {
      const year = cursor.getFullYear()
      const month = cursor.getMonth()
      const daysInMonth = new Date(year, month + 1, 0).getDate()
      const day = Math.min(nextPayment.getDate(), daysInMonth)
      const paymentDate = new Date(year, month, day)

      if (
        paymentDate >= rangeStart &&
        paymentDate <= rangeEnd &&
        paymentDate >= startedAt &&
        paymentDate <= effectiveEnd
      ) {
        dates.push(paymentDate)
      }

      cursor.setMonth(cursor.getMonth() + 1)
    }
  } else {
    // Yearly: same month/day each year — check all years that overlap the range.
    const startYear = Math.min(startedAt.getFullYear(), rangeStart.getFullYear()) - 1
    const endYear = rangeEnd.getFullYear() + 1
    for (let year = startYear; year <= endYear; year++) {
      const candidate = new Date(year, nextPayment.getMonth(), nextPayment.getDate())
      if (
        candidate >= rangeStart &&
        candidate <= rangeEnd &&
        candidate >= startedAt &&
        candidate <= effectiveEnd
      ) {
        dates.push(candidate)
      }
    }
  }

  return dates
}

/**
 * Groups all subscription payment dates within [rangeStart, rangeEnd] by date key.
 * Returns a Map<"YYYY-MM-DD", CalendarPaymentEntry[]>.
 */
export function buildCalendarPayments(
  subscriptions: Subscription[],
  rangeStart: Date,
  rangeEnd: Date,
): Map<string, CalendarPaymentEntry[]> {
  const map = new Map<string, CalendarPaymentEntry[]>()
  for (const sub of subscriptions) {
    for (const date of getPaymentDatesInRange(sub, rangeStart, rangeEnd)) {
      const key = toDateKey(date)
      const entries = map.get(key) ?? []
      entries.push({ subscription: sub, date, amount: sub.price })
      map.set(key, entries)
    }
  }
  return map
}

// ─── Internal helpers ──────────────────────────────────────────────────────

function round(value: number): number {
  return Math.round((value + Number.EPSILON) * 100) / 100
}

function toDate(value: string): Date {
  return new Date(`${value}T00:00:00`)
}

function paymentDateInMonth(nextPaymentDate: Date, monthStart: Date): Date {
  const year = monthStart.getFullYear()
  const month = monthStart.getMonth()
  const day = Math.min(nextPaymentDate.getDate(), new Date(year, month + 1, 0).getDate())
  return new Date(year, month, day)
}
