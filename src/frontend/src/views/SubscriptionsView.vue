<script setup lang="ts">
import { onMounted, ref } from 'vue'
import type { SubscriptionStatus } from '../app/types/subscription'
import { useSubscriptionStore } from '../app/stores/subscriptionStore'
import SubscriptionModal from '../components/subscriptions/SubscriptionModal.vue'
import SubscriptionTable from '../components/subscriptions/SubscriptionTable.vue'

const store = useSubscriptionStore()
const showModal = ref(false)

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
      <div class="view-header-actions">
        <button class="btn-primary" @click="showModal = true">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
            <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
          </svg>
          Hinzufügen
        </button>
      </div>
    </header>

    <section class="card">
      <h2>Abos</h2>
      <SubscriptionTable
        :subscriptions="store.subscriptions"
        @update-status="updateStatus"
        @remove="remove"
      />
    </section>
  </section>

  <SubscriptionModal
    :show="showModal"
    @close="showModal = false"
    @submit="(req) => store.create(req)"
  />
</template>
