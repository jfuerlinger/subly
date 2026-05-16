<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import type { NewSubscriptionRequest } from '../../app/types/subscription'
import { createCategory, fetchCategories, type CategoryDto } from '../../app/api/categoriesApi'

const emit = defineEmits<{
  submit: [request: NewSubscriptionRequest]
}>()

const categories = ref<CategoryDto[]>([])
const showNewCategoryInput = ref(false)
const newCategoryName = ref('')
const newCategoryError = ref('')

onMounted(async () => {
  categories.value = await fetchCategories()
})

const form = reactive<NewSubscriptionRequest>({
  name: '',
  vendor: '',
  category: '',
  price: 0,
  cycle: 'monthly',
  nextPaymentDate: new Date().toISOString().slice(0, 10),
  paymentMethod: 'Visa',
})

function onCategoryChange(event: Event) {
  const value = (event.target as HTMLSelectElement).value
  if (value === '__new__') {
    showNewCategoryInput.value = true
    form.category = ''
  } else {
    showNewCategoryInput.value = false
    form.category = value
  }
}

async function onAddCategory() {
  const name = newCategoryName.value.trim()
  if (!name) {
    newCategoryError.value = 'Bitte einen Kategorienamen eingeben.'
    return
  }
  try {
    const created = await createCategory(name)
    categories.value = [...categories.value, created].sort((a, b) =>
      a.name.localeCompare(b.name),
    )
    form.category = created.name
    showNewCategoryInput.value = false
    newCategoryName.value = ''
    newCategoryError.value = ''
  } catch {
    newCategoryError.value = 'Kategorie konnte nicht erstellt werden (bereits vorhanden?).'
  }
}

function onSubmit() {
  if (!form.name || !form.vendor || !form.category || form.price <= 0) {
    return
  }

  emit('submit', { ...form })
  form.name = ''
  form.vendor = ''
  form.category = categories.value[0]?.name ?? ''
  form.price = 0
  form.cycle = 'monthly'
  form.paymentMethod = 'Visa'
}
</script>

<template>
  <form class="form" @submit.prevent="onSubmit">
    <input v-model="form.name" type="text" placeholder="Name" required>
    <input v-model="form.vendor" type="text" placeholder="Anbieter" required>

    <select :value="showNewCategoryInput ? '__new__' : form.category" @change="onCategoryChange" required>
      <option value="" disabled>Kategorie wählen</option>
      <option v-for="cat in categories" :key="cat.id" :value="cat.name">
        {{ cat.name }}
      </option>
      <option value="__new__">+ Neue Kategorie…</option>
    </select>

    <div v-if="showNewCategoryInput" class="new-category">
      <input
        v-model="newCategoryName"
        type="text"
        placeholder="Neue Kategorie"
        @keydown.enter.prevent="onAddCategory"
      >
      <button type="button" @click="onAddCategory">Hinzufügen</button>
      <span v-if="newCategoryError" class="error">{{ newCategoryError }}</span>
    </div>

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

