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
