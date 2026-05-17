<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useSubscriptionStore } from '../app/stores/subscriptionStore'
import CalendarMonthView from '../components/calendar/CalendarMonthView.vue'
import CalendarWeekView from '../components/calendar/CalendarWeekView.vue'
import { buildCalendarPayments } from '../app/utils/subscriptionMath'
import { formatCurrency } from '../app/utils/formatting'

type ViewMode = 'month' | 'week'

const store = useSubscriptionStore()
const viewMode = ref<ViewMode>('month')
const anchorDate = ref(new Date())

// ─── Date range ─────────────────────────────────────────────────────────────

const monthStart = computed(() => new Date(anchorDate.value.getFullYear(), anchorDate.value.getMonth(), 1))
const monthEnd = computed(() => new Date(anchorDate.value.getFullYear(), anchorDate.value.getMonth() + 1, 0))

const weekStart = computed(() => {
  const d = anchorDate.value
  const dow = (d.getDay() + 6) % 7 // Mon=0
  return new Date(d.getFullYear(), d.getMonth(), d.getDate() - dow)
})
const weekEnd = computed(() => new Date(weekStart.value.getFullYear(), weekStart.value.getMonth(), weekStart.value.getDate() + 6))

const rangeStart = computed(() => (viewMode.value === 'month' ? monthStart.value : weekStart.value))
const rangeEnd = computed(() => (viewMode.value === 'month' ? monthEnd.value : weekEnd.value))

// ─── Payments ────────────────────────────────────────────────────────────────

const paymentsByDate = computed(() =>
  buildCalendarPayments(store.subscriptions, rangeStart.value, rangeEnd.value),
)

const periodTotal = computed(() => {
  let total = 0
  for (const entries of paymentsByDate.value.values()) {
    for (const entry of entries) total += entry.amount
  }
  return total
})

const paymentCount = computed(() => {
  let count = 0
  for (const entries of paymentsByDate.value.values()) count += entries.length
  return count
})

// ─── Navigation ─────────────────────────────────────────────────────────────

function navigatePrev() {
  const d = anchorDate.value
  if (viewMode.value === 'month') {
    anchorDate.value = new Date(d.getFullYear(), d.getMonth() - 1, 1)
  } else {
    anchorDate.value = new Date(d.getFullYear(), d.getMonth(), d.getDate() - 7)
  }
}

function navigateNext() {
  const d = anchorDate.value
  if (viewMode.value === 'month') {
    anchorDate.value = new Date(d.getFullYear(), d.getMonth() + 1, 1)
  } else {
    anchorDate.value = new Date(d.getFullYear(), d.getMonth(), d.getDate() + 7)
  }
}

function navigateToday() {
  anchorDate.value = new Date()
}

// ─── Period label ─────────────────────────────────────────────────────────────

const periodLabel = computed(() => {
  if (viewMode.value === 'month') {
    return anchorDate.value.toLocaleString('de-DE', { month: 'long', year: 'numeric' })
  }
  const startStr = weekStart.value.toLocaleString('de-DE', { day: 'numeric', month: 'short' })
  const endStr = weekEnd.value.toLocaleString('de-DE', { day: 'numeric', month: 'short', year: 'numeric' })
  return `${startStr} – ${endStr}`
})

// ─── Init ────────────────────────────────────────────────────────────────────

onMounted(async () => {
  if (store.subscriptions.length === 0) {
    await store.initialize()
  }
})
</script>

<template>
  <section class="view">
    <header class="view-header">
      <h1>Kalender</h1>
      <p v-if="store.error" class="error">{{ store.error }}</p>
      <div class="view-header-actions">
        <div class="view-toggle">
          <button
            :class="['view-toggle-btn', { 'view-toggle-btn--active': viewMode === 'month' }]"
            @click="viewMode = 'month'"
          >
            Monat
          </button>
          <button
            :class="['view-toggle-btn', { 'view-toggle-btn--active': viewMode === 'week' }]"
            @click="viewMode = 'week'"
          >
            Woche
          </button>
        </div>
      </div>
    </header>

    <!-- Navigation & summary bar -->
    <div class="calendar-toolbar">
      <div class="calendar-nav">
        <button class="icon-btn" title="Zurück" @click="navigatePrev">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
            <polyline points="15 18 9 12 15 6"/>
          </svg>
        </button>
        <button class="calendar-today-btn" @click="navigateToday">Heute</button>
        <button class="icon-btn" title="Vor" @click="navigateNext">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
            <polyline points="9 18 15 12 9 6"/>
          </svg>
        </button>
        <span class="calendar-period-label">{{ periodLabel }}</span>
      </div>

      <div v-if="paymentCount > 0" class="calendar-summary">
        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <line x1="12" y1="1" x2="12" y2="23"/><path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"/>
        </svg>
        <span class="calendar-summary-count">
          {{ paymentCount }} Zahlung{{ paymentCount !== 1 ? 'en' : '' }}:
        </span>
        <span class="calendar-summary-total">{{ formatCurrency(periodTotal) }}</span>
      </div>
      <div v-else class="calendar-summary calendar-summary--empty">
        Keine Zahlungen im Zeitraum
      </div>
    </div>

    <!-- Calendar grid -->
    <CalendarMonthView
      v-if="viewMode === 'month'"
      :year="anchorDate.getFullYear()"
      :month="anchorDate.getMonth()"
      :payments-by-date="paymentsByDate"
    />
    <CalendarWeekView
      v-else
      :week-start="weekStart"
      :week-end="weekEnd"
      :payments-by-date="paymentsByDate"
    />
  </section>
</template>

<style scoped>
/* ─── View toggle ─────────────────────────────────────────────────────────── */

.view-toggle {
  display: flex;
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  overflow: hidden;
  background: var(--color-surface);
}

.view-toggle-btn {
  border: none;
  border-radius: 0;
  background: transparent;
  color: var(--color-text-muted);
  padding: 0.35rem 0.875rem;
  font-size: 0.8125rem;
  font-weight: 500;
  cursor: pointer;
  transition: background 0.15s, color 0.15s;
}

.view-toggle-btn:hover {
  background: var(--color-border-light);
  color: var(--color-text);
}

.view-toggle-btn--active {
  background: var(--color-primary);
  color: #fff;
}

.view-toggle-btn--active:hover {
  background: var(--color-primary-hover);
}

/* ─── Toolbar ─────────────────────────────────────────────────────────────── */

.calendar-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  flex-wrap: wrap;
}

.calendar-nav {
  display: flex;
  align-items: center;
  gap: 0.375rem;
}

.calendar-today-btn {
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  background: var(--color-surface);
  padding: 0.35rem 0.7rem;
  font-size: 0.8125rem;
  font-weight: 500;
  cursor: pointer;
  color: var(--color-text);
  transition: background 0.1s;
}

.calendar-today-btn:hover {
  background: var(--color-border-light);
}

.calendar-period-label {
  font-size: 1rem;
  font-weight: 700;
  color: var(--color-text);
  margin-left: 0.375rem;
  text-transform: capitalize;
}

/* ─── Summary ─────────────────────────────────────────────────────────────── */

.calendar-summary {
  display: flex;
  align-items: center;
  gap: 0.45rem;
  background: var(--color-primary-light);
  color: var(--color-primary);
  border-radius: 0.5rem;
  padding: 0.4rem 0.875rem;
  font-size: 0.875rem;
}

.calendar-summary--empty {
  background: var(--color-border-light);
  color: var(--color-text-faint);
}

.calendar-summary-count {
  font-weight: 500;
}

.calendar-summary-total {
  font-weight: 700;
}
</style>
