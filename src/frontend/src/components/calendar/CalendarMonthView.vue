<script setup lang="ts">
import { computed } from 'vue'
import type { CalendarPaymentEntry } from '../../app/utils/subscriptionMath'
import { toDateKey } from '../../app/utils/subscriptionMath'
import { formatCurrency } from '../../app/utils/formatting'

const props = defineProps<{
  year: number
  month: number
  paymentsByDate: Map<string, CalendarPaymentEntry[]>
}>()

const WEEK_DAY_LABELS = ['Mo', 'Di', 'Mi', 'Do', 'Fr', 'Sa', 'So']

function categoryColor(category: string): string {
  return `var(--category-${category}, var(--color-primary))`
}

interface CalendarCell {
  date: Date
  dateKey: string
  isCurrentMonth: boolean
  isToday: boolean
  payments: CalendarPaymentEntry[]
}

const todayKey = toDateKey(new Date())

const grid = computed((): CalendarCell[] => {
  const firstDay = new Date(props.year, props.month, 1)
  const lastDay = new Date(props.year, props.month + 1, 0)

  // Monday = 0 (European week start)
  const startDow = (firstDay.getDay() + 6) % 7

  const cells: CalendarCell[] = []

  // Padding from previous month
  for (let i = startDow - 1; i >= 0; i--) {
    const date = new Date(props.year, props.month, -i)
    const dateKey = toDateKey(date)
    cells.push({ date, dateKey, isCurrentMonth: false, isToday: dateKey === todayKey, payments: props.paymentsByDate.get(dateKey) ?? [] })
  }

  // Days of the current month
  for (let d = 1; d <= lastDay.getDate(); d++) {
    const date = new Date(props.year, props.month, d)
    const dateKey = toDateKey(date)
    cells.push({ date, dateKey, isCurrentMonth: true, isToday: dateKey === todayKey, payments: props.paymentsByDate.get(dateKey) ?? [] })
  }

  // Padding for next month to complete the last row
  const remaining = cells.length % 7 === 0 ? 0 : 7 - (cells.length % 7)
  for (let d = 1; d <= remaining; d++) {
    const date = new Date(props.year, props.month + 1, d)
    const dateKey = toDateKey(date)
    cells.push({ date, dateKey, isCurrentMonth: false, isToday: dateKey === todayKey, payments: props.paymentsByDate.get(dateKey) ?? [] })
  }

  return cells
})

const weeks = computed(() => {
  const rows: CalendarCell[][] = []
  for (let i = 0; i < grid.value.length; i += 7) {
    rows.push(grid.value.slice(i, i + 7))
  }
  return rows
})

function dayTotal(payments: CalendarPaymentEntry[]): number {
  return payments.reduce((sum, e) => sum + e.amount, 0)
}
</script>

<template>
  <div class="month-calendar card">
    <!-- Weekday header -->
    <div class="month-grid-header">
      <div v-for="label in WEEK_DAY_LABELS" :key="label" class="month-day-name">{{ label }}</div>
    </div>

    <!-- Week rows -->
    <div class="month-grid-body">
      <div v-for="(week, wi) in weeks" :key="wi" class="month-week-row">
        <div
          v-for="cell in week"
          :key="cell.dateKey"
          :class="[
            'month-day-cell',
            { 'month-day-cell--other-month': !cell.isCurrentMonth },
          ]"
        >
          <div :class="['month-day-number', { 'month-day-number--today': cell.isToday }]">
            {{ cell.date.getDate() }}
          </div>

          <div v-if="cell.payments.length > 0" class="month-payment-list">
            <div
              v-for="entry in cell.payments"
              :key="entry.subscription.id"
              class="month-payment-chip"
              :style="{ '--chip-color': categoryColor(entry.subscription.categoryName) }"
              :title="`${entry.subscription.name} – ${formatCurrency(entry.amount)}`"
            >
              <span class="month-payment-chip-dot" />
              <span class="month-payment-chip-name">{{ entry.subscription.name }}</span>
              <span class="month-payment-chip-amount">{{ formatCurrency(entry.amount) }}</span>
            </div>
            <div v-if="cell.payments.length > 1" class="month-day-total">
              ∑ {{ formatCurrency(dayTotal(cell.payments)) }}
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.month-calendar {
  padding: 0;
  overflow: hidden;
}

.month-grid-header {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  background: var(--color-border-light);
  border-bottom: 1px solid var(--color-border);
}

.month-day-name {
  padding: 0.5rem;
  font-size: 0.7rem;
  font-weight: 600;
  color: var(--color-text-faint);
  text-transform: uppercase;
  letter-spacing: 0.05em;
  text-align: center;
}

.month-grid-body {
  display: flex;
  flex-direction: column;
}

.month-week-row {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  border-bottom: 1px solid var(--color-border-light);
}

.month-week-row:last-child {
  border-bottom: none;
}

.month-day-cell {
  min-height: 90px;
  padding: 0.4rem 0.4rem 0.35rem;
  border-right: 1px solid var(--color-border-light);
  display: flex;
  flex-direction: column;
  gap: 0.2rem;
}

.month-day-cell:last-child {
  border-right: none;
}

.month-day-cell--other-month {
  background: var(--color-border-light);
  opacity: 0.45;
}

.month-day-number {
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--color-text-muted);
  width: 1.4rem;
  height: 1.4rem;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  flex-shrink: 0;
}

.month-day-number--today {
  background: var(--color-primary);
  color: #fff;
}

.month-payment-list {
  display: flex;
  flex-direction: column;
  gap: 0.125rem;
  flex: 1;
}

.month-payment-chip {
  display: flex;
  align-items: center;
  gap: 0.25rem;
  border-left: 2px solid var(--chip-color);
  border-radius: 0.2rem;
  padding: 0.15rem 0.3rem;
  font-size: 0.68rem;
  overflow: hidden;
  background: var(--color-border-light);
  cursor: default;
}

.month-payment-chip-dot {
  width: 5px;
  height: 5px;
  border-radius: 50%;
  background: var(--chip-color);
  flex-shrink: 0;
}

.month-payment-chip-name {
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  color: var(--color-text);
  font-weight: 500;
}

.month-payment-chip-amount {
  color: var(--color-text-muted);
  font-weight: 600;
  white-space: nowrap;
  flex-shrink: 0;
}

.month-day-total {
  font-size: 0.68rem;
  color: var(--color-primary);
  font-weight: 700;
  text-align: right;
  padding: 0 0.1rem;
}
</style>
