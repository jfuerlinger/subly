<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useSubscriptionStore } from '../app/stores/subscriptionStore'
import SummaryCards from '../components/dashboard/SummaryCards.vue'
import { formatCurrency, formatDate } from '../app/utils/formatting'

const store = useSubscriptionStore()

const upcoming = computed(() => {
  const today = new Date()
  const end = new Date(today)
  end.setDate(end.getDate() + 30)
  return store.activeSubscriptions.filter((subscription) => {
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
    </header>

    <SummaryCards :summary="store.summary" />

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
  </section>
</template>
