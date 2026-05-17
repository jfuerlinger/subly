import type { SubscriptionStatus } from '../types/subscription'

export interface SubscriptionStatusMeta {
  value: SubscriptionStatus
  label: string
  tone: 'success' | 'warning' | 'danger'
  icon: 'active' | 'paused' | 'cancelled'
}

const activeStatusMeta: SubscriptionStatusMeta = {
  value: 'active',
  label: 'Aktiv',
  tone: 'success',
  icon: 'active',
}

const pausedStatusMeta: SubscriptionStatusMeta = {
  value: 'paused',
  label: 'Pausiert',
  tone: 'warning',
  icon: 'paused',
}

const cancelledStatusMeta: SubscriptionStatusMeta = {
  value: 'cancelled',
  label: 'Gekündigt',
  tone: 'danger',
  icon: 'cancelled',
}

export const subscriptionStatusOptions: readonly SubscriptionStatusMeta[] = [
  activeStatusMeta,
  pausedStatusMeta,
  cancelledStatusMeta,
]

const subscriptionStatusMetaByValue: Record<SubscriptionStatus, SubscriptionStatusMeta> = {
  active: activeStatusMeta,
  paused: pausedStatusMeta,
  cancelled: cancelledStatusMeta,
}

export function getSubscriptionStatusMeta(status: SubscriptionStatus): SubscriptionStatusMeta {
  return subscriptionStatusMetaByValue[status]
}
