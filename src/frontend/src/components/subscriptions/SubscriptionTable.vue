<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import type { Subscription, SubscriptionStatus } from '../../app/types/subscription'
import { formatCurrency, formatDate } from '../../app/utils/formatting'

const props = defineProps<{
  subscriptions: Subscription[]
}>()

const emit = defineEmits<{
  updateStatus: [id: string, status: SubscriptionStatus]
  remove: [id: string]
}>()

// ─── Filter state ────────────────────────────────────────

const filterName = ref('')
const filterStatus = ref<SubscriptionStatus | ''>('')
const filterCategories = ref<string[]>([])
const categoryDropdownOpen = ref(false)

const availableCategories = computed(() => {
  const cats = new Set(props.subscriptions.map((s) => s.category))
  return [...cats].sort((a, b) => a.localeCompare(b))
})

const categoryLabel = computed(() => {
  if (filterCategories.value.length === 0) return 'Alle Kategorien'
  if (filterCategories.value.length === 1) return filterCategories.value[0]
  return `${filterCategories.value.length} Kategorien`
})

function toggleCategory(cat: string) {
  if (filterCategories.value.includes(cat)) {
    filterCategories.value = filterCategories.value.filter((c) => c !== cat)
  } else {
    filterCategories.value = [...filterCategories.value, cat]
  }
}

function closeCategoryDropdown() {
  categoryDropdownOpen.value = false
}

onMounted(() => document.addEventListener('click', closeCategoryDropdown))
onUnmounted(() => document.removeEventListener('click', closeCategoryDropdown))

// ─── Sort state ──────────────────────────────────────────

type SortKey = 'name' | 'category' | 'price' | 'nextPaymentDate' | 'status'

const sortKey = ref<SortKey | null>(null)
const sortDir = ref<'asc' | 'desc'>('asc')

function toggleSort(key: SortKey) {
  if (sortKey.value === key) {
    sortDir.value = sortDir.value === 'asc' ? 'desc' : 'asc'
  } else {
    sortKey.value = key
    sortDir.value = 'asc'
  }
}

function sortIcon(key: SortKey): string {
  if (sortKey.value !== key) return '⇅'
  return sortDir.value === 'asc' ? '↑' : '↓'
}

// ─── Filtered + sorted list ──────────────────────────────

const processedSubscriptions = computed(() => {
  let result = props.subscriptions

  const name = filterName.value.trim().toLowerCase()
  if (name) {
    result = result.filter((s) => s.name.toLowerCase().includes(name))
  }

  if (filterStatus.value) {
    result = result.filter((s) => s.status === filterStatus.value)
  }

  if (filterCategories.value.length > 0) {
    result = result.filter((s) => filterCategories.value.includes(s.category))
  }

  if (sortKey.value) {
    const key = sortKey.value
    const dir = sortDir.value === 'asc' ? 1 : -1
    result = [...result].sort((a, b) => {
      const av = a[key]
      const bv = b[key]
      if (typeof av === 'number' && typeof bv === 'number') {
        return (av - bv) * dir
      }
      return String(av).localeCompare(String(bv)) * dir
    })
  }

  return result
})
</script>

<template>
  <div>
    <!-- Filter bar -->
    <div class="filter-bar">
      <input
        v-model="filterName"
        type="text"
        placeholder="Nach Name filtern…"
        class="filter-input"
      />

      <select v-model="filterStatus" class="filter-select">
        <option value="">Alle Status</option>
        <option value="active">Aktiv</option>
        <option value="paused">Pausiert</option>
        <option value="cancelled">Gekündigt</option>
      </select>

      <div class="multiselect" @click.stop>
        <button
          type="button"
          class="multiselect-trigger"
          :class="{ 'multiselect-trigger--active': filterCategories.length > 0 }"
          @click="categoryDropdownOpen = !categoryDropdownOpen"
        >
          {{ categoryLabel }}
          <span class="multiselect-arrow">{{ categoryDropdownOpen ? '▲' : '▼' }}</span>
        </button>
        <div v-show="categoryDropdownOpen" class="multiselect-panel">
          <label v-for="cat in availableCategories" :key="cat" class="multiselect-option">
            <input type="checkbox" :checked="filterCategories.includes(cat)" @change="toggleCategory(cat)" />
            {{ cat }}
          </label>
          <p v-if="availableCategories.length === 0" class="multiselect-empty">Keine Kategorien</p>
        </div>
      </div>
    </div>

    <!-- Table -->
    <div class="table-container">
      <table class="table">
        <thead>
          <tr>
            <th class="th-sortable" @click="toggleSort('name')">
              Name <span class="sort-icon" :class="{ 'sort-icon--active': sortKey === 'name' }">{{ sortIcon('name') }}</span>
            </th>
            <th class="th-sortable" @click="toggleSort('category')">
              Kategorie <span class="sort-icon" :class="{ 'sort-icon--active': sortKey === 'category' }">{{ sortIcon('category') }}</span>
            </th>
            <th class="th-sortable" @click="toggleSort('price')">
              Preis <span class="sort-icon" :class="{ 'sort-icon--active': sortKey === 'price' }">{{ sortIcon('price') }}</span>
            </th>
            <th class="th-sortable" @click="toggleSort('nextPaymentDate')">
              Nächste Zahlung <span class="sort-icon" :class="{ 'sort-icon--active': sortKey === 'nextPaymentDate' }">{{ sortIcon('nextPaymentDate') }}</span>
            </th>
            <th class="th-sortable" @click="toggleSort('status')">
              Status <span class="sort-icon" :class="{ 'sort-icon--active': sortKey === 'status' }">{{ sortIcon('status') }}</span>
            </th>
            <th>Aktionen</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="processedSubscriptions.length === 0">
            <td colspan="6" class="table-empty">Keine Abos gefunden.</td>
          </tr>
          <tr v-for="subscription in processedSubscriptions" :key="subscription.id">
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
  </div>
</template>
