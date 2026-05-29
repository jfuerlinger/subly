import { describe, expect, it } from 'vitest'
import type { Subscription } from '../app/types/subscription'
import {
  buildSubscriptionExportPayload,
  parseSubscriptionImportPayload,
} from '../app/utils/subscriptionTransfer'

const sampleSubscription: Subscription = {
  id: 'sub-1',
  name: 'Netflix',
  vendor: 'Netflix',
  logoUrl: null,
  category: 'streaming',
  price: 17.99,
  cycle: 'monthly',
  nextPaymentDate: '2026-05-20',
  paymentMethod: 'Visa',
  status: 'active',
  autoRenew: true,
  startedAt: '2024-01-01',
  cancelledAt: null,
}

describe('subscriptionTransfer', () => {
  it('builds export payload with metadata', () => {
    const payload = buildSubscriptionExportPayload([sampleSubscription])

    expect(payload.format).toBe('subly-subscriptions')
    expect(payload.version).toBe(1)
    expect(payload.subscriptions).toHaveLength(1)
    expect(typeof payload.exportedAt).toBe('string')
  })

  it('parses an object payload with subscriptions', () => {
    const json = JSON.stringify({
      format: 'subly-subscriptions',
      version: 1,
      subscriptions: [
        {
          ...sampleSubscription,
          status: 'paused',
        },
      ],
    })

    const result = parseSubscriptionImportPayload(json)

    expect(result).toHaveLength(1)
    expect(result[0].status).toBe('paused')
    expect(result[0].name).toBe('Netflix')
  })

  it('parses array payloads and derives cancelled status from cancelledAt', () => {
    const json = JSON.stringify([
      {
        ...sampleSubscription,
        status: undefined,
        cancelledAt: '2026-05-01',
      },
    ])

    const result = parseSubscriptionImportPayload(json)

    expect(result[0].status).toBe('cancelled')
    expect(result[0].cancelledAt).toBe('2026-05-01')
  })

  it('parses nullable and missing logoUrl values as null', () => {
    const json = JSON.stringify([
      {
        ...sampleSubscription,
        logoUrl: null,
      },
      {
        ...sampleSubscription,
        logoUrl: '',
      },
      {
        ...sampleSubscription,
      },
    ])

    const result = parseSubscriptionImportPayload(json)

    expect(result[0].logoUrl).toBeNull()
    expect(result[1].logoUrl).toBeNull()
    expect(result[2].logoUrl).toBeNull()
  })

  it('throws when logoUrl is not a string', () => {
    const json = JSON.stringify([
      {
        ...sampleSubscription,
        logoUrl: 42,
      },
    ])

    expect(() => parseSubscriptionImportPayload(json)).toThrow(
      'Feld "logoUrl" muss ein String sein (Eintrag 1).',
    )
  })

  it('throws on invalid billing cycle', () => {
    const json = JSON.stringify([
      {
        ...sampleSubscription,
        cycle: 'weekly',
      },
    ])

    expect(() => parseSubscriptionImportPayload(json)).toThrow(
      'Feld "cycle" muss "monthly" oder "yearly" sein (Eintrag 1).',
    )
  })

  it('throws on unsupported object payload format', () => {
    const json = JSON.stringify({
      format: 'other-format',
      version: 1,
      subscriptions: [sampleSubscription],
    })

    expect(() => parseSubscriptionImportPayload(json)).toThrow(
      'Ungültiges JSON-Format. Erwartet wird "subly-subscriptions".',
    )
  })

  it('throws on unsupported object payload version', () => {
    const json = JSON.stringify({
      format: 'subly-subscriptions',
      version: 2,
      subscriptions: [sampleSubscription],
    })

    expect(() => parseSubscriptionImportPayload(json)).toThrow(
      'Ungültige JSON-Version. Unterstützt wird Version 1.',
    )
  })

  it('throws on invalid calendar date values', () => {
    const json = JSON.stringify({
      format: 'subly-subscriptions',
      version: 1,
      subscriptions: [
        {
          ...sampleSubscription,
          nextPaymentDate: '2026-99-99',
        },
      ],
    })

    expect(() => parseSubscriptionImportPayload(json)).toThrow(
      'Feld "nextPaymentDate" muss im Format YYYY-MM-DD sein (Eintrag 1).',
    )
  })
})
