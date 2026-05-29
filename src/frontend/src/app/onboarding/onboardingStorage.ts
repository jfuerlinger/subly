const onboardingPendingUsersKey = 'subly:onboarding:pendingUsers'
const onboardingDemoSeededUsersKey = 'subly:onboarding:demoSeededUsers'
const onboardingTourCompletedUsersKey = 'subly:onboarding:tourCompletedUsers'

export function markOnboardingPending(userId: string): void {
  updateUserSet(onboardingPendingUsersKey, userId, true)
}

export function isOnboardingPending(userId: string): boolean {
  return readUserSet(onboardingPendingUsersKey).has(userId)
}

export function clearOnboardingPending(userId: string): void {
  updateUserSet(onboardingPendingUsersKey, userId, false)
}

export function markDemoDataSeeded(userId: string): void {
  updateUserSet(onboardingDemoSeededUsersKey, userId, true)
}

export function isDemoDataSeeded(userId: string): boolean {
  return readUserSet(onboardingDemoSeededUsersKey).has(userId)
}

export function markTourCompleted(userId: string): void {
  updateUserSet(onboardingTourCompletedUsersKey, userId, true)
}

export function isTourCompleted(userId: string): boolean {
  return readUserSet(onboardingTourCompletedUsersKey).has(userId)
}

function readUserSet(key: string): Set<string> {
  const storedValue = window.localStorage.getItem(key)
  if (!storedValue) {
    return new Set<string>()
  }

  try {
    const parsed = JSON.parse(storedValue)
    if (Array.isArray(parsed)) {
      return new Set(
        parsed.filter((entry): entry is string => typeof entry === 'string' && entry.trim().length > 0),
      )
    }
  } catch {
    return new Set<string>()
  }

  return new Set<string>()
}

function updateUserSet(key: string, userId: string, shouldInclude: boolean): void {
  const normalizedUserId = userId.trim()
  if (!normalizedUserId) {
    return
  }

  const userSet = readUserSet(key)
  if (shouldInclude) {
    userSet.add(normalizedUserId)
  } else {
    userSet.delete(normalizedUserId)
  }

  window.localStorage.setItem(key, JSON.stringify([...userSet]))
}
