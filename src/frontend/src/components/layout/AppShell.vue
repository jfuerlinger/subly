<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '../../app/stores/authStore'
import { useProfileStore } from '../../app/stores/profileStore'

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

const route = useRoute()
const isMobileMenuOpen = ref(false)
const mobileNavigationId = 'primary-navigation'

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
})

function logout() {
  authStore.logout()
  closeMobileMenu()
  void router.push({ name: 'auth' })
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
      <nav class="nav">
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
</template>
