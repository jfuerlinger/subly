<script setup lang="ts">
import { onMounted } from 'vue'
import type { NewSubscriptionRequest, SubscriptionStatus } from '../app/types/subscription'
import { useSubscriptionStore } from '../app/stores/subscriptionStore'
import SubscriptionForm from '../components/subscriptions/SubscriptionForm.vue'
import SubscriptionTable from '../components/subscriptions/SubscriptionTable.vue'

const store = useSubscriptionStore()

async function createSubscription(request: NewSubscriptionRequest) {
  await store.create(request)
}

async function updateStatus(id: string, status: SubscriptionStatus) {
  await store.updateStatus(id, status)
}

async function remove(id: string) {
  await store.remove(id)
}

onMounted(async () => {
  if (store.subscriptions.length === 0) {
    await store.initialize()
  }
})
</script>

<template>
  <section class="view">
    <header class="view-header">
      <h1>Alle Abos</h1>
      <p v-if="store.error" class="error">{{ store.error }}</p>
    </header>

    <section class="card">
      <h2>Neues Abo</h2>
      <SubscriptionForm @submit="createSubscription" />
    </section>

    <section class="card">
      <h2>Abos</h2>
      <SubscriptionTable
        :subscriptions="store.subscriptions"
        @update-status="updateStatus"
        @remove="remove"
      />
    </section>
  </section>
</template>
