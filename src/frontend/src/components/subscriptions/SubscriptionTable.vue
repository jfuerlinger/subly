<script setup lang="ts">
import type { Subscription, SubscriptionStatus } from '../../app/types/subscription'
import { formatCurrency, formatDate } from '../../app/utils/formatting'

defineProps<{
  subscriptions: Subscription[]
}>()

const emit = defineEmits<{
  updateStatus: [id: string, status: SubscriptionStatus]
  remove: [id: string]
}>()
</script>

<template>
  <div class="table-container">
    <table class="table">
      <thead>
        <tr>
          <th>Name</th>
          <th>Kategorie</th>
          <th>Preis</th>
          <th>Nächste Zahlung</th>
          <th>Status</th>
          <th>Aktionen</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="subscription in subscriptions" :key="subscription.id">
          <td>{{ subscription.name }}</td>
          <td>{{ subscription.category }}</td>
          <td>{{ formatCurrency(subscription.price) }}</td>
          <td>{{ formatDate(subscription.nextPaymentDate) }}</td>
          <td>{{ subscription.status }}</td>
          <td class="actions">
            <button type="button" @click="emit('updateStatus', subscription.id, 'active')">Aktiv</button>
            <button type="button" @click="emit('updateStatus', subscription.id, 'paused')">Pausiert</button>
            <button type="button" @click="emit('updateStatus', subscription.id, 'cancelled')">Gekündigt</button>
            <button type="button" class="danger" @click="emit('remove', subscription.id)">Löschen</button>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
