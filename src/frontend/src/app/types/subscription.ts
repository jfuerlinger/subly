export type BillingCycle = 'monthly' | 'yearly'

export type SubscriptionStatus = 'active' | 'paused' | 'cancelled'

export interface Subscription {
  id: string
  name: string
  vendor: string
  category: string
  price: number
  cycle: BillingCycle
  nextPaymentDate: string
  paymentMethod: string
  status: SubscriptionStatus
  autoRenew: boolean
  startedAt: string
}

export interface NewSubscriptionRequest {
  name: string
  vendor: string
  category: string
  price: number
  cycle: BillingCycle
  nextPaymentDate: string
  paymentMethod: string
}

export interface DashboardSummary {
  monthlyTotal: number
  yearlyTotal: number
  activeSubscriptionsCount: number
  upcomingPaymentsTotal30Days: number
  upcomingPaymentsCount30Days: number
}
