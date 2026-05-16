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
const dynamicImportReloadKey = 'subly:dynamic-import-reloads'

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
    const retriedPaths = new Set<string>()

    try {
      const storedPaths = JSON.parse(window.sessionStorage.getItem(dynamicImportReloadKey) ?? '[]')

      if (Array.isArray(storedPaths)) {
        storedPaths.forEach((path) => {
          if (typeof path === 'string') {
            retriedPaths.add(path)
          }
        })
      }
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
      console.warn(
        '[router] Failed to persist retry guard. Multiple reload attempts may occur for this route.',
      )
    }

    window.location.href = to.fullPath
  }
})
