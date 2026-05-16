<script setup lang="ts">
import { onMounted } from 'vue'
import { useSubscriptionStore } from '../app/stores/subscriptionStore'
import AnalyticsKpis from '../components/analytics/AnalyticsKpis.vue'
import SpendingTrendChart from '../components/analytics/SpendingTrendChart.vue'
import PaymentForecastChart from '../components/analytics/PaymentForecastChart.vue'
import TopSubscriptionsChart from '../components/analytics/TopSubscriptionsChart.vue'
import PaymentMethodChart from '../components/analytics/PaymentMethodChart.vue'

const store = useSubscriptionStore()

onMounted(async () => {
  if (store.subscriptions.length === 0) {
    await store.initialize()
  }
})
</script>

<template>
  <section class="view">
    <header class="view-header">
      <h1>Analyse</h1>
      <p v-if="store.error" class="error">{{ store.error }}</p>
    </header>

    <AnalyticsKpis :subscriptions="store.subscriptions" />

    <div class="analytics-grid">
      <SpendingTrendChart :subscriptions="store.subscriptions" />
      <PaymentForecastChart :subscriptions="store.subscriptions" />
    </div>

    <div class="analytics-grid">
      <TopSubscriptionsChart :subscriptions="store.subscriptions" />
      <PaymentMethodChart :subscriptions="store.subscriptions" />
    </div>
  </section>
</template>

<style scoped>
.analytics-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
  align-items: start;
}

@media (max-width: 1100px) {
  .analytics-grid {
    grid-template-columns: 1fr;
  }
}
</style>
