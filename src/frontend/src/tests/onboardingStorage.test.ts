import { beforeEach, describe, expect, it } from 'vitest'
import {
  clearOnboardingPending,
  isDemoDataSeeded,
  isOnboardingPending,
  isTourCompleted,
  markDemoDataSeeded,
  markOnboardingPending,
  markTourCompleted,
} from '../app/onboarding/onboardingStorage'

describe('onboardingStorage', () => {
  beforeEach(() => {
    window.localStorage.clear()
  })

  it('tracks pending onboarding per user', () => {
    markOnboardingPending('user-a')
    expect(isOnboardingPending('user-a')).toBe(true)

    clearOnboardingPending('user-a')
    expect(isOnboardingPending('user-a')).toBe(false)
  })

  it('stores demo and tour flags per user', () => {
    markDemoDataSeeded('user-b')
    markTourCompleted('user-b')

    expect(isDemoDataSeeded('user-b')).toBe(true)
    expect(isTourCompleted('user-b')).toBe(true)
    expect(isDemoDataSeeded('user-c')).toBe(false)
    expect(isTourCompleted('user-c')).toBe(false)
  })
})
