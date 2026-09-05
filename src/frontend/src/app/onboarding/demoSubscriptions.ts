import type { NewSubscriptionRequest } from '../types/subscription'

export function buildDemoSubscriptions(
  categoryIdByName: Map<string, string>,
  anchorDate = new Date(),
): NewSubscriptionRequest[] {
  return [
    {
      name: 'Netflix Standard',
      vendor: 'Netflix',
      logoUrl: null,
      categoryId: categoryIdByName.get('streaming')!,
      price: 17.99,
      cycle: 'monthly',
      nextPaymentDate: formatDate(addDays(anchorDate, 6)),
      paymentMethod: 'Visa •• 4421',
      startedAt: formatDate(addYears(anchorDate, -2)),
      cancelledAt: null,
    },
    {
      name: 'Spotify Family',
      vendor: 'Spotify',
      logoUrl: null,
      categoryId: categoryIdByName.get('streaming')!,
      price: 17.99,
      cycle: 'monthly',
      nextPaymentDate: formatDate(addDays(anchorDate, 2)),
      paymentMethod: 'PayPal',
      startedAt: formatDate(addYears(anchorDate, -3)),
      cancelledAt: null,
    },
    {
      name: 'ChatGPT Plus',
      vendor: 'OpenAI',
      logoUrl: null,
      categoryId: categoryIdByName.get('software')!,
      price: 22,
      cycle: 'monthly',
      nextPaymentDate: formatDate(addDays(anchorDate, 4)),
      paymentMethod: 'Visa •• 4421',
      startedAt: formatDate(addYears(anchorDate, -1)),
      cancelledAt: null,
    },
    {
      name: 'Amazon Prime',
      vendor: 'Amazon',
      logoUrl: null,
      categoryId: categoryIdByName.get('membership')!,
      price: 89.9,
      cycle: 'yearly',
      nextPaymentDate: formatDate(addMonths(anchorDate, 2)),
      paymentMethod: 'Mastercard •• 0044',
      startedAt: formatDate(addYears(anchorDate, -4)),
      cancelledAt: null,
    },
    {
      name: 'iCloud+ 200GB',
      vendor: 'Apple',
      logoUrl: null,
      categoryId: categoryIdByName.get('cloud')!,
      price: 2.99,
      cycle: 'monthly',
      nextPaymentDate: formatDate(addDays(anchorDate, 10)),
      paymentMethod: 'Apple Pay',
      startedAt: formatDate(addYears(anchorDate, -2)),
      cancelledAt: null,
    },
  ]
}

function addDays(value: Date, days: number): Date {
  const next = new Date(value)
  next.setDate(next.getDate() + days)
  return next
}

function addMonths(value: Date, months: number): Date {
  const next = new Date(value)
  next.setMonth(next.getMonth() + months)
  return next
}

function addYears(value: Date, years: number): Date {
  const next = new Date(value)
  next.setFullYear(next.getFullYear() + years)
  return next
}

function formatDate(value: Date): string {
  const year = value.getFullYear()
  const month = String(value.getMonth() + 1).padStart(2, '0')
  const day = String(value.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}
