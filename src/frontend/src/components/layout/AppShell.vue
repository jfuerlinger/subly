<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { AxiosError } from 'axios'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '../../app/stores/authStore'
import { useProfileStore } from '../../app/stores/profileStore'
import { useSubscriptionStore } from '../../app/stores/subscriptionStore'
import { useCategoryStore } from '../../app/stores/categoriesStore'
import { buildDemoSubscriptions } from '../../app/onboarding/demoSubscriptions'
import {
  clearOnboardingPending,
  isDemoDataSeeded,
  isOnboardingPending,
  isTourCompleted,
  markDemoDataSeeded,
  markTourCompleted,
} from '../../app/onboarding/onboardingStorage'
import OnboardingTour from '../onboarding/OnboardingTour.vue'

interface NavItem {
  to: string
  label: string
  icon: string
  badge?: number | null
}

const overviewNav: NavItem[] = [
  { to: '/dashboard', label: 'Dashboard', icon: 'grid' },
  { to: '/subscriptions', label: 'Alle Abos', icon: 'list' },
  { to: '/calendar', label: 'Kalender', icon: 'calendar' },
  { to: '/analytics', label: 'Analyse', icon: 'chart' },
  { to: '/categories', label: 'Kategorien', icon: 'tag' },
]

const systemNav: NavItem[] = [
  { to: '/settings', label: 'Einstellungen', icon: 'settings' },
]

const authStore = useAuthStore()
const router = useRouter()
const profileStore = useProfileStore()
const subscriptionStore = useSubscriptionStore()
const categoryStore = useCategoryStore()

const route = useRoute()
const isMobileMenuOpen = ref(false)
const mobileNavigationId = 'primary-navigation'
const showOnboardingPrompt = ref(false)
const wantsDemoData = ref(true)
const wantsWalkthrough = ref(true)
const onboardingError = ref<string | null>(null)
const onboardingLoading = ref(false)
const showWalkthrough = ref(false)

const toggleMobileMenu = () => {
  isMobileMenuOpen.value = !isMobileMenuOpen.value
}

const closeMobileMenu = () => {
  isMobileMenuOpen.value = false
}

const handleGlobalKeydown = (event: KeyboardEvent) => {
  if (event.key === 'Escape' && isMobileMenuOpen.value) {
    closeMobileMenu()
  }
}

onMounted(() => {
  window.addEventListener('keydown', handleGlobalKeydown)
})

onBeforeUnmount(() => {
  window.removeEventListener('keydown', handleGlobalKeydown)
})

watch(
  () => route.fullPath,
  () => {
    closeMobileMenu()
  },
)

const displayName = computed(() => {
  if (profileStore.displayName !== 'Mein Profil') {
    return profileStore.displayName
  }

  if (!authStore.user) {
    return 'Mein Profil'
  }

  return `${authStore.user.firstName} ${authStore.user.lastName}`.trim()
})

const userInitials = computed(() => {
  if (profileStore.initials !== '?') {
    return profileStore.initials
  }

  if (!authStore.user) {
    return 'MP'
  }

  return `${authStore.user.firstName[0] ?? ''}${authStore.user.lastName[0] ?? ''}`.toUpperCase()
})

onMounted(() => {
  if (authStore.user && !profileStore.firstName && !profileStore.lastName) {
    profileStore.setName(authStore.user.firstName, authStore.user.lastName)
  }

  openOnboardingPromptIfNeeded(authStore.user?.id ?? null)
})

watch(
  () => authStore.user?.id ?? null,
  (userId) => {
    openOnboardingPromptIfNeeded(userId)
  },
)

watch(
  () => route.name,
  (currentRouteName) => {
    if (currentRouteName !== 'auth') {
      openOnboardingPromptIfNeeded(authStore.user?.id ?? null)
    }
  },
)

function logout() {
  authStore.logout()
  closeMobileMenu()
  void router.push({ name: 'auth' })
}

function openOnboardingPromptIfNeeded(userId: string | null): void {
  if (!userId || route.name === 'auth' || !isOnboardingPending(userId)) {
    return
  }

  wantsDemoData.value = !isDemoDataSeeded(userId)
  wantsWalkthrough.value = !isTourCompleted(userId)
  onboardingError.value = null
  showOnboardingPrompt.value = true
}

function postponeOnboarding(): void {
  showOnboardingPrompt.value = false
}

async function applyOnboardingSelection(): Promise<void> {
  const currentUserId = authStore.user?.id
  if (!currentUserId || onboardingLoading.value) {
    return
  }

  onboardingLoading.value = true
  onboardingError.value = null

  try {
    if (wantsDemoData.value && !isDemoDataSeeded(currentUserId)) {
      if (categoryStore.categories.length === 0) {
        await categoryStore.initialize()
      }
      const categoryIdByName = new Map(categoryStore.categories.map((c) => [c.name, c.id]))
      await subscriptionStore.createMany(buildDemoSubscriptions(categoryIdByName))
      markDemoDataSeeded(currentUserId)
    }

    clearOnboardingPending(currentUserId)
    showOnboardingPrompt.value = false

    if (wantsWalkthrough.value) {
      showWalkthrough.value = true
      return
    }

    markTourCompleted(currentUserId)
  } catch (error) {
    onboardingError.value = toOnboardingError(error)
  } finally {
    onboardingLoading.value = false
  }
}

function handleWalkthroughComplete(): void {
  const currentUserId = authStore.user?.id
  if (currentUserId) {
    markTourCompleted(currentUserId)
  }

  showWalkthrough.value = false
}

function handleWalkthroughSkip(): void {
  const currentUserId = authStore.user?.id
  if (currentUserId) {
    markTourCompleted(currentUserId)
  }

  showWalkthrough.value = false
}

function toOnboardingError(error: unknown): string {
  if (error instanceof AxiosError) {
    const detail = error.response?.data?.detail
    if (typeof detail === 'string' && detail.length > 0) {
      return detail
    }
  }

  return 'Onboarding konnte nicht abgeschlossen werden. Bitte versuche es erneut.'
}
</script>

<template>
  <div class="app-shell" :class="{ 'app-shell--menu-open': isMobileMenuOpen }">
    <button
      type="button"
      class="mobile-nav-backdrop"
      :class="{ 'mobile-nav-backdrop--visible': isMobileMenuOpen }"
      aria-label="Menü schließen"
      @click="closeMobileMenu"
    />

    <aside
      class="sidebar"
      :class="{ 'sidebar--open': isMobileMenuOpen }"
      :id="mobileNavigationId"
    >
      <!-- Brand -->
      <div class="sidebar-brand">
        <div class="sidebar-brand-icon">S</div>
        <div class="sidebar-brand-text">
          <strong>Subly</strong>
          <small>Subscription Manager</small>
        </div>
      </div>

      <!-- Overview nav -->
      <div class="sidebar-section-label">Übersicht</div>
      <nav class="nav" data-tour="sidebar-navigation">
        <RouterLink
          v-for="item in overviewNav"
          :key="item.to"
          :to="item.to"
          class="nav-link"
          active-class="nav-link-active"
          @click="closeMobileMenu"
        >
          <!-- Grid icon -->
          <svg v-if="item.icon === 'grid'" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <rect x="3" y="3" width="7" height="7"/><rect x="14" y="3" width="7" height="7"/>
            <rect x="14" y="14" width="7" height="7"/><rect x="3" y="14" width="7" height="7"/>
          </svg>
          <!-- List icon -->
          <svg v-else-if="item.icon === 'list'" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <line x1="8" y1="6" x2="21" y2="6"/><line x1="8" y1="12" x2="21" y2="12"/>
            <line x1="8" y1="18" x2="21" y2="18"/><line x1="3" y1="6" x2="3.01" y2="6"/>
            <line x1="3" y1="12" x2="3.01" y2="12"/><line x1="3" y1="18" x2="3.01" y2="18"/>
          </svg>
          <!-- Calendar icon -->
          <svg v-else-if="item.icon === 'calendar'" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <rect x="3" y="4" width="18" height="18" rx="2" ry="2"/>
            <line x1="16" y1="2" x2="16" y2="6"/><line x1="8" y1="2" x2="8" y2="6"/>
            <line x1="3" y1="10" x2="21" y2="10"/>
          </svg>
          <!-- Chart icon -->
          <svg v-else-if="item.icon === 'chart'" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <line x1="18" y1="20" x2="18" y2="10"/><line x1="12" y1="20" x2="12" y2="4"/>
            <line x1="6" y1="20" x2="6" y2="14"/><line x1="2" y1="20" x2="22" y2="20"/>
          </svg>
          <!-- Tag icon -->
          <svg v-else-if="item.icon === 'tag'" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M20.59 13.41l-7.17 7.17a2 2 0 0 1-2.83 0L2 12V2h10l8.59 8.59a2 2 0 0 1 0 2.82z"/>
            <line x1="7" y1="7" x2="7.01" y2="7"/>
          </svg>
          {{ item.label }}
          <span v-if="item.badge !== undefined && item.badge !== null" class="nav-badge">{{ item.badge }}</span>
        </RouterLink>
      </nav>

      <!-- System nav -->
      <div class="sidebar-section-label">System</div>
      <nav class="nav">
        <RouterLink
          v-for="item in systemNav"
          :key="item.to"
          :to="item.to"
          class="nav-link"
          active-class="nav-link-active"
          @click="closeMobileMenu"
        >
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <circle cx="12" cy="12" r="3"/>
            <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83-2.83l.06-.06A1.65 1.65 0 0 0 4.68 15a1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 2.83-2.83l.06.06A1.65 1.65 0 0 0 9 4.68a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 2.83l-.06.06A1.65 1.65 0 0 0 19.4 9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z"/>
          </svg>
          {{ item.label }}
        </RouterLink>
      </nav>

      <div class="sidebar-spacer" />

      <!-- Subly Plus promo -->
      <div class="sidebar-promo">
        <div class="sidebar-promo-title">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="currentColor">
            <path d="M12 2l2.4 7.4H22l-6.2 4.5 2.4 7.4L12 17l-6.2 4.3 2.4-7.4L2 9.4h7.6z"/>
          </svg>
          Subly Plus
        </div>
        <div class="sidebar-promo-desc">Automatischer Import von Buchungen, unbegrenzte Anhänge &amp; Familien-Sharing.</div>
        <button class="sidebar-promo-btn">14 Tage testen</button>
      </div>

      <!-- User -->
      <RouterLink
        to="/profile"
        class="sidebar-user"
        active-class="sidebar-user--active"
        @click="closeMobileMenu"
      >
        <div class="sidebar-user-avatar">{{ userInitials }}</div>
        <div class="sidebar-user-info">
          <div class="sidebar-user-name">{{ displayName }}</div>
          <div class="sidebar-user-plan">Free Plan</div>
        </div>
      </RouterLink>
      <button class="sidebar-promo-btn" type="button" @click="logout">Abmelden</button>
    </aside>

    <main class="content">
      <header class="mobile-topbar">
        <button
          type="button"
          class="mobile-menu-btn"
          :aria-expanded="isMobileMenuOpen"
          :aria-controls="mobileNavigationId"
          :aria-label="isMobileMenuOpen ? 'Menü schließen' : 'Menü öffnen'"
          @click="toggleMobileMenu"
        >
          <svg
            v-if="!isMobileMenuOpen"
            width="18"
            height="18"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2"
            stroke-linecap="round"
            stroke-linejoin="round"
          >
            <line x1="3" y1="6" x2="21" y2="6" />
            <line x1="3" y1="12" x2="21" y2="12" />
            <line x1="3" y1="18" x2="21" y2="18" />
          </svg>
          <svg
            v-else
            width="18"
            height="18"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2"
            stroke-linecap="round"
            stroke-linejoin="round"
          >
            <line x1="18" y1="6" x2="6" y2="18" />
            <line x1="6" y1="6" x2="18" y2="18" />
          </svg>
        </button>
        <RouterLink class="mobile-topbar-brand" to="/dashboard">Subly</RouterLink>
      </header>
      <slot />
    </main>
  </div>

  <Teleport to="body">
    <Transition name="onboarding-fade">
      <div v-if="showOnboardingPrompt" class="onboarding-backdrop" @click.self="postponeOnboarding">
        <section
          class="onboarding-dialog"
          role="dialog"
          aria-modal="true"
          aria-labelledby="onboarding-title"
        >
          <h2 id="onboarding-title">Willkommen bei Subly</h2>
          <p class="onboarding-intro">
            Möchtest du den Einstieg direkt einrichten? Du kannst jetzt Demodaten anlegen und eine kurze Tour starten.
          </p>

          <div class="onboarding-choice-list">
            <label class="onboarding-choice">
              <input v-model="wantsDemoData" type="checkbox">
              <span>
                <strong>Demodaten erstellen</strong>
                <small>Wir legen dir Beispiel-Abos an, damit du Funktionen sofort testen kannst.</small>
              </span>
            </label>

            <label class="onboarding-choice">
              <input v-model="wantsWalkthrough" type="checkbox">
              <span>
                <strong>Interaktive Tour starten</strong>
                <small>Subly führt dich durch alle Seiten und erklärt die wichtigsten Funktionen.</small>
              </span>
            </label>
          </div>

          <p v-if="onboardingError" class="onboarding-error">{{ onboardingError }}</p>

          <footer class="onboarding-actions">
            <button type="button" class="onboarding-btn onboarding-btn--ghost" @click="postponeOnboarding">
              Später
            </button>
            <button
              type="button"
              class="onboarding-btn onboarding-btn--primary"
              :disabled="onboardingLoading"
              @click="applyOnboardingSelection"
            >
              {{ onboardingLoading ? 'Wird eingerichtet…' : 'Auswahl übernehmen' }}
            </button>
          </footer>
        </section>
      </div>
    </Transition>
  </Teleport>

  <OnboardingTour
    :active="showWalkthrough"
    @complete="handleWalkthroughComplete"
    @skip="handleWalkthroughSkip"
  />
</template>

<style scoped>
.onboarding-backdrop {
  position: fixed;
  inset: 0;
  z-index: 110;
  background: rgba(15, 23, 42, 0.58);
  display: grid;
  place-items: center;
  padding: 1rem;
}

.onboarding-dialog {
  width: min(560px, calc(100vw - 2rem));
  background: #ffffff;
  border-radius: 16px;
  border: 1px solid #e5e7eb;
  box-shadow: 0 24px 48px rgba(15, 23, 42, 0.3);
  padding: 1.25rem;
}

.onboarding-dialog h2 {
  margin: 0;
}

.onboarding-intro {
  margin: 0.6rem 0 1rem;
  color: #4b5563;
}

.onboarding-choice-list {
  display: grid;
  gap: 0.7rem;
}

.onboarding-choice {
  border: 1px solid #d1d5db;
  border-radius: 12px;
  padding: 0.75rem;
  display: flex;
  gap: 0.7rem;
  align-items: flex-start;
  cursor: pointer;
}

.onboarding-choice input {
  margin-top: 0.2rem;
}

.onboarding-choice span {
  display: grid;
  gap: 0.2rem;
}

.onboarding-choice small {
  color: #6b7280;
  font-size: 0.8rem;
}

.onboarding-error {
  margin: 0.9rem 0 0;
  color: #b91c1c;
  font-size: 0.86rem;
}

.onboarding-actions {
  margin-top: 1.1rem;
  display: flex;
  justify-content: flex-end;
  gap: 0.6rem;
}

.onboarding-btn {
  border-radius: 10px;
  border: 1px solid transparent;
  padding: 0.6rem 0.9rem;
  font-weight: 600;
  cursor: pointer;
}

.onboarding-btn--ghost {
  background: #fff;
  border-color: #d1d5db;
  color: #374151;
}

.onboarding-btn--ghost:hover {
  background: #f9fafb;
}

.onboarding-btn--primary {
  background: #4f46e5;
  color: #fff;
}

.onboarding-btn--primary:hover {
  background: #4338ca;
}

.onboarding-btn:disabled {
  opacity: 0.65;
  cursor: not-allowed;
}

.onboarding-fade-enter-active,
.onboarding-fade-leave-active {
  transition: opacity 0.2s ease;
}

.onboarding-fade-enter-from,
.onboarding-fade-leave-to {
  opacity: 0;
}
</style>
