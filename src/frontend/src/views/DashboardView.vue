<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useSubscriptionStore } from '../app/stores/subscriptionStore'
import { buildDashboardSummary } from '../app/utils/subscriptionMath'
import SummaryCards from '../components/dashboard/SummaryCards.vue'
import SubscriptionDonutChart from '../components/dashboard/SubscriptionDonutChart.vue'
import SubscriptionModal from '../components/subscriptions/SubscriptionModal.vue'
import { formatCurrency, formatDate } from '../app/utils/formatting'

const store = useSubscriptionStore()
const showModal = ref(false)
const searchQuery = ref('')

const filteredSubscriptions = computed(() => {
  const query = searchQuery.value.trim().toLowerCase()
  if (!query) {
    return store.subscriptions
  }

  return store.subscriptions.filter((subscription) => {
    const searchableValues = [subscription.name, subscription.vendor, subscription.category]
    return searchableValues.some((value) => value.toLowerCase().includes(query))
  })
})

const filteredSummary = computed(() => {
  if (!searchQuery.value.trim()) {
    return store.summary
  }

  return buildDashboardSummary(filteredSubscriptions.value)
})

const upcoming = computed(() => {
  const today = new Date()
  const end = new Date(today)
  end.setDate(end.getDate() + 30)
  return filteredSubscriptions.value.filter((subscription) => {
    if (subscription.status !== 'active') {
      return false
    }

    const date = new Date(subscription.nextPaymentDate)
    return date >= today && date <= end
  })
})

onMounted(async () => {
  if (store.subscriptions.length === 0) {
    await store.initialize()
  }
})
</script>

<template>
  <section class="view">
    <header class="view-header">
      <h1>Dashboard</h1>
      <p v-if="store.error" class="error">{{ store.error }}</p>
      <div class="view-header-actions">
        <input v-model="searchQuery" class="search-input" type="text" placeholder="Abo suchen…" />
        <button class="icon-btn" title="Exportieren">
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/>
            <polyline points="17 8 12 3 7 8"/><line x1="12" y1="3" x2="12" y2="15"/>
          </svg>
        </button>
        <button class="icon-btn" title="Benachrichtigungen">
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"/>
            <path d="M13.73 21a2 2 0 0 1-3.46 0"/>
          </svg>
        </button>
        <button class="btn-primary" @click="showModal = true">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
            <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
          </svg>
          Hinzufügen
        </button>
      </div>
    </header>

    <SummaryCards :summary="filteredSummary" />

    <div class="dashboard-bottom-grid">
      <SubscriptionDonutChart :subscriptions="filteredSubscriptions" />

      <section class="card">
        <h2>Anstehende Zahlungen</h2>
        <ul class="upcoming-list">
          <li v-for="subscription in upcoming" :key="subscription.id">
            <span>{{ subscription.name }}</span>
            <span>{{ formatDate(subscription.nextPaymentDate) }}</span>
            <span>{{ formatCurrency(subscription.price) }}</span>
          </li>
        </ul>
        <p v-if="upcoming.length === 0" class="muted">Keine Zahlungen in den nächsten 30 Tagen.</p>
      </section>
    </div>
  </section>

  <SubscriptionModal
    :show="showModal"
    @close="showModal = false"
    @submit="(req) => store.create(req)"
  />
</template>

<style scoped>
.dashboard-bottom-grid {
  display: grid;
  grid-template-columns: 320px 1fr;
  gap: 1rem;
  align-items: start;
}

@media (max-width: 900px) {
  .dashboard-bottom-grid {
    grid-template-columns: 1fr;
  }
}
</style>
