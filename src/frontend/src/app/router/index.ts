import { createRouter, createWebHistory } from 'vue-router'

const routes = [
  {
    path: '/',
    redirect: '/dashboard',
  },
  {
    path: '/dashboard',
    name: 'dashboard',
    component: () => import('../../views/DashboardView.vue'),
  },
  {
    path: '/subscriptions',
    name: 'subscriptions',
    component: () => import('../../views/SubscriptionsView.vue'),
  },
  {
    path: '/analytics',
    name: 'analytics',
    component: () => import('../../views/AnalyticsView.vue'),
  },
]

export const router = createRouter({
  history: createWebHistory(),
  routes,
})

const dynamicImportFetchError = 'Failed to fetch dynamically imported module'
const dynamicImportReloadKey = 'subly:dynamic-import-reload'

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
    if (window.sessionStorage.getItem(dynamicImportReloadKey) === to.fullPath) {
      return
    }

    window.sessionStorage.setItem(dynamicImportReloadKey, to.fullPath)
    window.location.href = to.fullPath
  }
})

router.afterEach(() => {
  if (import.meta.env.DEV) {
    window.sessionStorage.removeItem(dynamicImportReloadKey)
  }
})
