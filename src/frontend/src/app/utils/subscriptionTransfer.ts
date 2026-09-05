import type {
  BillingCycle,
  NewSubscriptionRequest,
  Subscription,
  SubscriptionStatus,
} from '../types/subscription'

const exportFormat = 'subly-subscriptions'
const exportVersion = 1
const isoDatePattern = /^\d{4}-\d{2}-\d{2}$/

type JsonRecord = Record<string, unknown>

// The exported/imported JSON represents a category by its (portable) name rather than the
// internal categoryId, which isn't meaningful across installs/accounts.
export interface SubscriptionExportItem extends Omit<Subscription, 'categoryId' | 'categoryName'> {
  category: string
}

export interface SubscriptionExportPayload {
  format: typeof exportFormat
  version: typeof exportVersion
  exportedAt: string
  subscriptions: SubscriptionExportItem[]
}

export interface ImportSubscriptionItem extends Omit<NewSubscriptionRequest, 'categoryId'> {
  status: SubscriptionStatus
  category: string
}

export function buildSubscriptionExportPayload(
  subscriptions: Subscription[],
): SubscriptionExportPayload {
  return {
    format: exportFormat,
    version: exportVersion,
    exportedAt: new Date().toISOString(),
    subscriptions: subscriptions.map(({ categoryId: _categoryId, categoryName, ...rest }) => ({
      ...rest,
      category: categoryName,
    })),
  }
}

export function parseSubscriptionImportPayload(content: string): ImportSubscriptionItem[] {
  let parsed: unknown

  try {
    parsed = JSON.parse(content)
  } catch {
    throw new Error('Die JSON-Datei konnte nicht gelesen werden.')
  }

  const rawSubscriptions = extractSubscriptions(parsed)
  if (!Array.isArray(rawSubscriptions)) {
    throw new Error('Die JSON-Datei enthält kein gültiges Feld "subscriptions".')
  }

  return rawSubscriptions.map((item, index) => parseImportItem(item, index))
}

function extractSubscriptions(parsed: unknown): unknown {
  if (Array.isArray(parsed)) {
    return parsed
  }

  if (isJsonRecord(parsed)) {
    if (parsed.format !== exportFormat) {
      throw new Error(`Ungültiges JSON-Format. Erwartet wird "${exportFormat}".`)
    }

    if (parsed.version !== exportVersion) {
      throw new Error(`Ungültige JSON-Version. Unterstützt wird Version ${exportVersion}.`)
    }

    return parsed.subscriptions
  }

  return null
}

function parseImportItem(item: unknown, index: number): ImportSubscriptionItem {
  if (!isJsonRecord(item)) {
    throw new Error(`Ungültiger Eintrag bei Position ${index + 1}.`)
  }

  const cancelledAt = readNullableDate(item.cancelledAt, 'cancelledAt', index)

  return {
    name: readRequiredString(item.name, 'name', index),
    vendor: readRequiredString(item.vendor, 'vendor', index),
    logoUrl: readNullableString(item.logoUrl, 'logoUrl', index),
    category: readRequiredString(item.category, 'category', index),
    price: readPositiveNumber(item.price, 'price', index),
    cycle: readBillingCycle(item.cycle, index),
    nextPaymentDate: readDate(item.nextPaymentDate, 'nextPaymentDate', index),
    paymentMethod: readRequiredString(item.paymentMethod, 'paymentMethod', index),
    startedAt: readDate(item.startedAt, 'startedAt', index),
    cancelledAt,
    status: readStatus(item.status, cancelledAt, index),
  }
}

function readRequiredString(value: unknown, field: string, index: number): string {
  if (typeof value !== 'string' || value.trim().length === 0) {
    throw new Error(`Feld "${field}" fehlt oder ist ungültig (Eintrag ${index + 1}).`)
  }

  return value.trim()
}

function readNullableString(value: unknown, field: string, index: number): string | null {
  if (value === null || value === undefined) {
    return null
  }

  if (typeof value !== 'string') {
    throw new Error(`Feld "${field}" muss ein String sein (Eintrag ${index + 1}).`)
  }

  const trimmed = value.trim()
  return trimmed === '' ? null : trimmed
}

function readPositiveNumber(value: unknown, field: string, index: number): number {
  if (typeof value !== 'number' || !Number.isFinite(value) || value <= 0) {
    throw new Error(`Feld "${field}" muss eine Zahl größer 0 sein (Eintrag ${index + 1}).`)
  }

  return value
}

function readDate(value: unknown, field: string, index: number): string {
  if (typeof value !== 'string' || !isValidIsoCalendarDate(value)) {
    throw new Error(`Feld "${field}" muss im Format YYYY-MM-DD sein (Eintrag ${index + 1}).`)
  }

  return value
}

function readNullableDate(value: unknown, field: string, index: number): string | null {
  if (value === null || value === undefined) {
    return null
  }

  return readDate(value, field, index)
}

function readBillingCycle(value: unknown, index: number): BillingCycle {
  if (value === 'monthly' || value === 'yearly') {
    return value
  }

  throw new Error(`Feld "cycle" muss "monthly" oder "yearly" sein (Eintrag ${index + 1}).`)
}

function readStatus(
  value: unknown,
  cancelledAt: string | null,
  index: number,
): SubscriptionStatus {
  if (value === undefined || value === null || value === '') {
    return cancelledAt ? 'cancelled' : 'active'
  }

  if (value === 'active' || value === 'paused' || value === 'cancelled') {
    return value
  }

  throw new Error(
    `Feld "status" muss "active", "paused" oder "cancelled" sein (Eintrag ${index + 1}).`,
  )
}

function isJsonRecord(value: unknown): value is JsonRecord {
  return typeof value === 'object' && value !== null
}

function isValidIsoCalendarDate(value: string): boolean {
  if (!isoDatePattern.test(value)) {
    return false
  }

  const [yearString, monthString, dayString] = value.split('-')
  const year = Number(yearString)
  const month = Number(monthString)
  const day = Number(dayString)

  const parsedDate = new Date(Date.UTC(year, month - 1, day))
  return parsedDate.getUTCFullYear() === year
    && parsedDate.getUTCMonth() === month - 1
    && parsedDate.getUTCDate() === day
}
