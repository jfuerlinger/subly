import { createRouter, createWebHistory } from 'vue-router'
import { getAccessToken } from '../auth/tokenStorage'

const routes = [
  {
    path: '/',
    redirect: '/auth',
  },
  {
    path: '/auth',
    name: 'auth',
    component: () => import('../../views/AuthView.vue'),
    meta: { requiresAuth: false },
  },
  {
    path: '/dashboard',
    name: 'dashboard',
    component: () => import('../../views/DashboardView.vue'),
    meta: { requiresAuth: true },
  },
  {
    path: '/subscriptions',
    name: 'subscriptions',
    component: () => import('../../views/SubscriptionsView.vue'),
    meta: { requiresAuth: true },
  },
  {
    path: '/calendar',
    name: 'calendar',
    component: () => import('../../views/CalendarView.vue'),
    meta: { requiresAuth: true },
  },
  {
    path: '/analytics',
    name: 'analytics',
    component: () => import('../../views/AnalyticsView.vue'),
    meta: { requiresAuth: true },
  },
  {
    path: '/settings',
    name: 'settings',
    component: () => import('../../views/SettingsView.vue'),
    meta: { requiresAuth: true },
  },
  {
    path: '/profile',
    name: 'profile',
    component: () => import('../../views/ProfileSettingsView.vue'),
    meta: { requiresAuth: true },
  },
]

export const router = createRouter({
  history: createWebHistory(),
  routes,
})

router.beforeEach((to) => {
  const authenticated = !!getAccessToken()
  const requiresAuth = to.meta.requiresAuth === true

  if (requiresAuth && !authenticated) {
    return { name: 'auth' }
  }

  if (to.name === 'auth' && authenticated) {
    return { name: 'dashboard' }
  }

  return true
})

const dynamicImportFetchError = 'Failed to fetch dynamically imported module'
const dynamicImportReloadKey = 'subly:dynamic-import-reloads'
const fallbackRetriedPaths = new Set<string>()
// In dev mode, Vite can trigger a full page reload when it detects new
// dependencies during a session (e.g. after adding a new source file that
// introduces a new import chain). This causes any in-flight dynamic import
// to fail with "Failed to fetch dynamically imported module". Reloading the
// page after such a failure picks up the freshly optimised bundle.
router.onError((err, to) => {
  if (!import.meta.env.DEV) {
    return
  }

  if (
    err instanceof TypeError &&
    err.message.includes(dynamicImportFetchError)
  ) {
    let retriedPaths = fallbackRetriedPaths

    try {
      const storedRetriedPaths = new Set<string>()
      const storedPaths = JSON.parse(window.sessionStorage.getItem(dynamicImportReloadKey) ?? '[]')

      if (Array.isArray(storedPaths)) {
        storedPaths.forEach((path) => {
          if (typeof path === 'string') {
            storedRetriedPaths.add(path)
          }
        })
      }

      retriedPaths = storedRetriedPaths
    } catch {
      console.warn(
        '[router] sessionStorage is unavailable. Automatic reload will proceed without loop protection.',
      )
    }

    if (retriedPaths.has(to.fullPath)) {
      console.warn(
        `[router] Dynamic import failed again for "${to.fullPath}". Skipping automatic reload to avoid a loop.`,
      )
      return
    }

    retriedPaths.add(to.fullPath)

    try {
      window.sessionStorage.setItem(dynamicImportReloadKey, JSON.stringify([...retriedPaths]))
    } catch {
      fallbackRetriedPaths.add(to.fullPath)
      console.warn(
        '[router] Failed to persist retry guard. Multiple reload attempts may occur for this route.',
      )
    }
    window.location.href = to.fullPath
  }
})
