<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import type { NewSubscriptionRequest, Subscription, SubscriptionStatus } from '../app/types/subscription'
import { useSubscriptionStore } from '../app/stores/subscriptionStore'
import SubscriptionModal from '../components/subscriptions/SubscriptionModal.vue'
import SubscriptionTable from '../components/subscriptions/SubscriptionTable.vue'

const store = useSubscriptionStore()
const showModal = ref(false)
const modalMode = ref<'create' | 'edit'>('create')
const editSubscriptionId = ref<string | null>(null)

const subscriptionToEdit = computed(() => {
  if (!editSubscriptionId.value) {
    return null
  }

  return store.subscriptions.find((subscription) => subscription.id === editSubscriptionId.value) ?? null
})

const modalInitialValues = computed<NewSubscriptionRequest | null>(() => {
  const subscription = subscriptionToEdit.value
  if (!subscription) {
    return null
  }

  return {
    name: subscription.name,
    vendor: subscription.vendor,
    logoUrl: subscription.logoUrl,
    category: subscription.category,
    price: subscription.price,
    cycle: subscription.cycle,
    nextPaymentDate: subscription.nextPaymentDate,
    paymentMethod: subscription.paymentMethod,
    startedAt: subscription.startedAt,
    cancelledAt: subscription.cancelledAt,
  }
})

async function updateStatus(id: string, status: SubscriptionStatus, cancelledAt?: string | null) {
  await store.updateStatus(id, status, cancelledAt)
}

function openCreateModal() {
  modalMode.value = 'create'
  editSubscriptionId.value = null
  showModal.value = true
}

function openEditModal(subscription: Subscription) {
  modalMode.value = 'edit'
  editSubscriptionId.value = subscription.id
  showModal.value = true
}

function closeModal() {
  showModal.value = false
  modalMode.value = 'create'
  editSubscriptionId.value = null
}

async function submitModal(request: NewSubscriptionRequest) {
  if (modalMode.value === 'edit' && editSubscriptionId.value) {
    await store.update(editSubscriptionId.value, request)
    return
  }

  await store.create(request)
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
        <button class="btn-primary" @click="openCreateModal">
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
        @edit="openEditModal"
        @update-status="updateStatus"
        @remove="remove"
      />
    </section>
  </section>

  <SubscriptionModal
    :show="showModal"
    :mode="modalMode"
    :initial-values="modalInitialValues"
    @close="closeModal"
    @submit="submitModal"
  />
</template>
