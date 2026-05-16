<script setup lang="ts">
import { reactive } from 'vue'
import type { NewSubscriptionRequest } from '../../app/types/subscription'

const emit = defineEmits<{
  submit: [request: NewSubscriptionRequest]
}>()

const form = reactive<NewSubscriptionRequest>({
  name: '',
  vendor: '',
  category: 'streaming',
  price: 0,
  cycle: 'monthly',
  nextPaymentDate: new Date().toISOString().slice(0, 10),
  paymentMethod: 'Visa',
})

function onSubmit() {
  if (!form.name || !form.vendor || form.price <= 0) {
    return
  }

  emit('submit', { ...form })
  form.name = ''
  form.vendor = ''
  form.category = 'streaming'
  form.price = 0
  form.cycle = 'monthly'
  form.paymentMethod = 'Visa'
}
</script>

<template>
  <form class="form" @submit.prevent="onSubmit">
    <input v-model="form.name" type="text" placeholder="Name" required>
    <input v-model="form.vendor" type="text" placeholder="Anbieter" required>
    <input v-model="form.category" type="text" placeholder="Kategorie" required>
    <input v-model.number="form.price" type="number" min="0.01" step="0.01" placeholder="Preis" required>
    <select v-model="form.cycle">
      <option value="monthly">Monatlich</option>
      <option value="yearly">Jährlich</option>
    </select>
    <input v-model="form.nextPaymentDate" type="date" required>
    <input v-model="form.paymentMethod" type="text" placeholder="Zahlungsmethode" required>
    <button type="submit">Abo hinzufügen</button>
  </form>
</template>
