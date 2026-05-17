<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import type { Subscription, SubscriptionStatus } from '../../app/types/subscription'
import { formatCurrency, formatDate } from '../../app/utils/formatting'
import { getSubscriptionStatusMeta, subscriptionStatusOptions } from '../../app/utils/subscriptionStatus'
import SubscriptionStatusBadge from './SubscriptionStatusBadge.vue'

const props = defineProps<{
  subscriptions: Subscription[]
}>()

const emit = defineEmits<{
  updateStatus: [id: string, status: SubscriptionStatus, cancelledAt?: string | null]
  remove: [id: string]
}>()

const today = new Date().toISOString().slice(0, 10)

// ─── Filter state ────────────────────────────────────────

const filterName = ref('')
const filterStatuses = ref<SubscriptionStatus[]>([])
const filterCategories = ref<string[]>([])
const statusDropdownOpen = ref(false)
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

const statusLabel = computed(() => {
  if (filterStatuses.value.length === 0) return 'Alle Status'
  if (filterStatuses.value.length === 1) {
    return getSubscriptionStatusMeta(filterStatuses.value[0]).label
  }
  return `${filterStatuses.value.length} Status`
})

const singleSelectedStatus = computed<SubscriptionStatus | null>(() => {
  if (filterStatuses.value.length !== 1) {
    return null
  }

  return filterStatuses.value[0]
})

function toggleStatus(status: SubscriptionStatus) {
  if (filterStatuses.value.includes(status)) {
    filterStatuses.value = filterStatuses.value.filter((value) => value !== status)
  } else {
    filterStatuses.value = [...filterStatuses.value, status]
  }
}

function toggleCategory(cat: string) {
  if (filterCategories.value.includes(cat)) {
    filterCategories.value = filterCategories.value.filter((c) => c !== cat)
  } else {
    filterCategories.value = [...filterCategories.value, cat]
  }
}

function toggleStatusDropdown() {
  statusDropdownOpen.value = !statusDropdownOpen.value
  categoryDropdownOpen.value = false
}

function toggleCategoryDropdown() {
  categoryDropdownOpen.value = !categoryDropdownOpen.value
  statusDropdownOpen.value = false
}

function closeDropdowns() {
  statusDropdownOpen.value = false
  categoryDropdownOpen.value = false
}

onMounted(() => document.addEventListener('click', closeDropdowns))
onUnmounted(() => document.removeEventListener('click', closeDropdowns))

// ─── Sort state ──────────────────────────────────────────

type SortKey = 'name' | 'category' | 'price' | 'nextPaymentDate' | 'startedAt' | 'cancelledAt' | 'status'

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

function cancelSubscription(subscription: Subscription) {
  emit('updateStatus', subscription.id, 'cancelled', subscription.cancelledAt ?? today)
}

// ─── Filtered + sorted list ──────────────────────────────

const processedSubscriptions = computed(() => {
  let result = props.subscriptions

  const name = filterName.value.trim().toLowerCase()
  if (name) {
    result = result.filter((s) => s.name.toLowerCase().includes(name))
  }

  if (filterStatuses.value.length > 0) {
    result = result.filter((s) => filterStatuses.value.includes(s.status))
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
      return String(av ?? '').localeCompare(String(bv ?? '')) * dir
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

      <div class="multiselect" @click.stop>
        <button
          type="button"
          class="multiselect-trigger"
          :class="{ 'multiselect-trigger--active': filterStatuses.length > 0 }"
          @click="toggleStatusDropdown"
        >
          <span class="multiselect-trigger__label">
            <SubscriptionStatusBadge v-if="singleSelectedStatus" :status="singleSelectedStatus" />
            <span v-else>{{ statusLabel }}</span>
          </span>
          <span class="multiselect-arrow">{{ statusDropdownOpen ? '▲' : '▼' }}</span>
        </button>
        <div v-show="statusDropdownOpen" class="multiselect-panel">
          <label
            v-for="status in subscriptionStatusOptions"
            :key="status.value"
            class="multiselect-option"
          >
            <input
              type="checkbox"
              :checked="filterStatuses.includes(status.value)"
              @change="toggleStatus(status.value)"
            />
            <SubscriptionStatusBadge :status="status.value" />
          </label>
        </div>
      </div>

      <div class="multiselect" @click.stop>
        <button
          type="button"
          class="multiselect-trigger"
          :class="{ 'multiselect-trigger--active': filterCategories.length > 0 }"
          @click="toggleCategoryDropdown"
        >
          <span class="multiselect-trigger__label">{{ categoryLabel }}</span>
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
            <th class="th-sortable" @click="toggleSort('startedAt')">
              Abgeschlossen am <span class="sort-icon" :class="{ 'sort-icon--active': sortKey === 'startedAt' }">{{ sortIcon('startedAt') }}</span>
            </th>
            <th class="th-sortable" @click="toggleSort('cancelledAt')">
              Kündigungsdatum <span class="sort-icon" :class="{ 'sort-icon--active': sortKey === 'cancelledAt' }">{{ sortIcon('cancelledAt') }}</span>
            </th>
            <th class="th-sortable" @click="toggleSort('status')">
              Status <span class="sort-icon" :class="{ 'sort-icon--active': sortKey === 'status' }">{{ sortIcon('status') }}</span>
            </th>
            <th>Aktionen</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="processedSubscriptions.length === 0">
            <td colspan="8" class="table-empty">Keine Abos gefunden.</td>
          </tr>
          <tr v-for="subscription in processedSubscriptions" :key="subscription.id">
            <td>{{ subscription.name }}</td>
            <td>{{ subscription.category }}</td>
            <td>{{ formatCurrency(subscription.price) }}</td>
            <td>{{ formatDate(subscription.nextPaymentDate) }}</td>
            <td>{{ formatDate(subscription.startedAt) }}</td>
            <td>{{ subscription.cancelledAt ? formatDate(subscription.cancelledAt) : '—' }}</td>
            <td>
              <SubscriptionStatusBadge :status="subscription.status" />
            </td>
            <td class="actions">
              <button
                v-for="status in subscriptionStatusOptions"
                :key="status.value"
                type="button"
                @click="status.value === 'cancelled' ? cancelSubscription(subscription) : emit('updateStatus', subscription.id, status.value)"
              >
                {{ status.label }}
              </button>
              <button type="button" class="danger" @click="emit('remove', subscription.id)">Löschen</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
