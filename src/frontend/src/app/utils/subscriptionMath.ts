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

function round(value: number): number {
  return Math.round((value + Number.EPSILON) * 100) / 100
}
