<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import type { NewSubscriptionRequest } from '../../app/types/subscription'
import { createCategory, fetchCategories, type CategoryDto } from '../../app/api/categoriesApi'

const emit = defineEmits<{
  submit: [request: NewSubscriptionRequest]
  cancel: []
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

function cancelNewCategory() {
  showNewCategoryInput.value = false
  newCategoryName.value = ''
  newCategoryError.value = ''
  if (!form.category) {
    form.category = categories.value[0]?.name ?? ''
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
  <form class="sub-form" @submit.prevent="onSubmit">
    <div class="form-row">
      <div class="field">
        <label class="field-label" for="field-name">Name</label>
        <input id="field-name" v-model="form.name" type="text" placeholder="z.B. Netflix" required>
      </div>
      <div class="field">
        <label class="field-label" for="field-vendor">Anbieter</label>
        <input id="field-vendor" v-model="form.vendor" type="text" placeholder="z.B. Netflix Inc." required>
      </div>
    </div>

    <div class="field">
      <label class="field-label" for="field-category">Kategorie</label>
      <select id="field-category" :value="showNewCategoryInput ? '__new__' : form.category" @change="onCategoryChange" required>
        <option value="" disabled>Kategorie wählen</option>
        <option v-for="cat in categories" :key="cat.id" :value="cat.name">
          {{ cat.name }}
        </option>
        <option value="__new__">+ Neue Kategorie anlegen…</option>
      </select>

      <div v-if="showNewCategoryInput" class="new-category-panel">
        <p class="new-category-title">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
            <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
          </svg>
          Neue Kategorie anlegen
        </p>
        <div class="new-category-row">
          <input
            id="field-new-category"
            v-model="newCategoryName"
            type="text"
            placeholder="Kategoriename"
            aria-label="Neuer Kategoriename"
            autofocus
            @keydown.enter.prevent="onAddCategory"
          >
          <button type="button" class="btn-primary btn-sm" @click="onAddCategory">Erstellen</button>
          <button type="button" class="btn-ghost btn-sm" @click="cancelNewCategory">Abbrechen</button>
        </div>
        <p v-if="newCategoryError" class="field-error">{{ newCategoryError }}</p>
      </div>
    </div>

    <div class="form-row">
      <div class="field">
        <label class="field-label" for="field-price">Preis</label>
        <input id="field-price" v-model.number="form.price" type="number" min="0.01" step="0.01" placeholder="0,00" required>
      </div>
      <div class="field">
        <label class="field-label" for="field-cycle">Abrechnungszyklus</label>
        <select id="field-cycle" v-model="form.cycle">
          <option value="monthly">Monatlich</option>
          <option value="yearly">Jährlich</option>
        </select>
      </div>
    </div>

    <div class="form-row">
      <div class="field">
        <label class="field-label" for="field-next-payment">Nächste Zahlung</label>
        <input id="field-next-payment" v-model="form.nextPaymentDate" type="date" required>
      </div>
      <div class="field">
        <label class="field-label" for="field-payment-method">Zahlungsmethode</label>
        <input id="field-payment-method" v-model="form.paymentMethod" type="text" placeholder="z.B. Visa" required>
      </div>
    </div>

    <div class="form-actions">
      <button type="button" class="btn-ghost" @click="emit('cancel')">Abbrechen</button>
      <button type="submit" class="btn-primary" :disabled="showNewCategoryInput && !form.category">
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
          <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
        </svg>
        Abo hinzufügen
      </button>
    </div>
  </form>
</template>

<style scoped>
.sub-form {
  display: flex;
  flex-direction: column;
  gap: 0.875rem;
}

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.75rem;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
}

.field-label {
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--color-text-muted);
}

.field input,
.field select {
  width: 100%;
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  padding: 0.5rem 0.625rem;
  font-size: 0.875rem;
  outline: none;
  background: var(--color-surface);
  color: var(--color-text);
  transition: border-color 0.15s;
}

.field input:focus,
.field select:focus {
  border-color: var(--color-primary);
  box-shadow: 0 0 0 3px rgba(79, 70, 229, 0.08);
}

/* ─── Neue Kategorie Panel ────────────────────── */

.new-category-panel {
  margin-top: 0.5rem;
  background: var(--color-primary-light);
  border: 1px solid #c7d2fe;
  border-left: 3px solid var(--color-primary);
  border-radius: 0.5rem;
  padding: 0.75rem;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.new-category-title {
  margin: 0;
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--color-primary);
  display: flex;
  align-items: center;
  gap: 0.35rem;
}

.new-category-row {
  display: flex;
  gap: 0.5rem;
  align-items: center;
}

.new-category-row input {
  flex: 1;
  border: 1px solid #c7d2fe;
  border-radius: 0.375rem;
  padding: 0.4rem 0.55rem;
  font-size: 0.875rem;
  outline: none;
  background: var(--color-surface);
  color: var(--color-text);
  transition: border-color 0.15s;
}

.new-category-row input:focus {
  border-color: var(--color-primary);
  box-shadow: 0 0 0 3px rgba(79, 70, 229, 0.1);
}

.field-error {
  margin: 0;
  font-size: 0.8rem;
  color: var(--color-danger);
}

/* ─── Buttons ─────────────────────────────────── */

.btn-sm {
  padding: 0.35rem 0.7rem;
  font-size: 0.8rem;
  white-space: nowrap;
}

.btn-ghost {
  background: transparent;
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  padding: 0.45rem 0.875rem;
  font-size: 0.8125rem;
  font-weight: 500;
  color: var(--color-text-muted);
  cursor: pointer;
  transition: background 0.15s, color 0.15s;
}

.btn-ghost:hover {
  background: var(--color-border-light);
  color: var(--color-text);
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.5rem;
  padding-top: 0.25rem;
  border-top: 1px solid var(--color-border-light);
  margin-top: 0.125rem;
}
</style>

