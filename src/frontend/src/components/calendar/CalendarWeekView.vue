<script setup lang="ts">
import { computed } from 'vue'
import type { CalendarPaymentEntry } from '../../app/utils/subscriptionMath'
import { toDateKey } from '../../app/utils/subscriptionMath'
import { formatCurrency } from '../../app/utils/formatting'

const props = defineProps<{
  weekStart: Date
  paymentsByDate: Map<string, CalendarPaymentEntry[]>
}>()

const WEEK_DAY_LABELS = ['Mo', 'Di', 'Mi', 'Do', 'Fr', 'Sa', 'So']

function categoryColor(category: string): string {
  return `var(--category-${category}, var(--color-primary))`
}

interface WeekDay {
  date: Date
  dateKey: string
  shortLabel: string
  isToday: boolean
  payments: CalendarPaymentEntry[]
}

const todayKey = toDateKey(new Date())

const weekDays = computed((): WeekDay[] =>
  Array.from({ length: 7 }, (_, i) => {
    const date = new Date(
      props.weekStart.getFullYear(),
      props.weekStart.getMonth(),
      props.weekStart.getDate() + i,
    )
    const dateKey = toDateKey(date)
    const dow = (date.getDay() + 6) % 7 // Mon=0
    return {
      date,
      dateKey,
      shortLabel: WEEK_DAY_LABELS[dow],
      isToday: dateKey === todayKey,
      payments: props.paymentsByDate.get(dateKey) ?? [],
    }
  }),
)

function dayTotal(payments: CalendarPaymentEntry[]): number {
  return payments.reduce((sum, e) => sum + e.amount, 0)
}
</script>

<template>
  <div class="week-calendar card">
    <div class="week-grid">
      <div
        v-for="day in weekDays"
        :key="day.dateKey"
        :class="['week-day-col', { 'week-day-col--today': day.isToday }]"
      >
        <!-- Day header -->
        <div class="week-day-header">
          <span class="week-day-name">{{ day.shortLabel }}</span>
          <span :class="['week-day-number', { 'week-day-number--today': day.isToday }]">
            {{ day.date.getDate() }}
          </span>
          <span class="week-day-month">
            {{ day.date.toLocaleString('de-DE', { month: 'short' }) }}
          </span>
        </div>

        <!-- Payment entries -->
        <div class="week-day-content">
          <div
            v-for="entry in day.payments"
            :key="entry.subscription.id"
            class="week-payment-card"
            :style="{ '--chip-color': categoryColor(entry.subscription.categoryName) }"
          >
            <div class="week-payment-top">
              <span class="week-payment-dot" />
              <span class="week-payment-name">{{ entry.subscription.name }}</span>
            </div>
            <div class="week-payment-amount">{{ formatCurrency(entry.amount) }}</div>
          </div>

          <div v-if="day.payments.length > 1" class="week-day-total">
            <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <line x1="12" y1="1" x2="12" y2="23"/><path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"/>
            </svg>
            {{ formatCurrency(dayTotal(day.payments)) }}
          </div>

          <p v-if="day.payments.length === 0" class="week-day-empty">Keine Zahlungen</p>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.week-calendar {
  padding: 0;
  overflow-x: auto;
}

.week-grid {
  display: grid;
  grid-template-columns: repeat(7, minmax(110px, 1fr));
  min-height: 280px;
}

.week-day-col {
  border-right: 1px solid var(--color-border-light);
  display: flex;
  flex-direction: column;
}

.week-day-col:last-child {
  border-right: none;
}

.week-day-col--today {
  background: var(--color-primary-light);
}

.week-day-header {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.1rem;
  padding: 0.625rem 0.5rem 0.5rem;
  border-bottom: 1px solid var(--color-border-light);
  background: var(--color-border-light);
}

.week-day-col--today .week-day-header {
  background: var(--color-primary-light);
  border-bottom-color: var(--color-border);
}

.week-day-name {
  font-size: 0.68rem;
  font-weight: 600;
  color: var(--color-text-faint);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.week-day-col--today .week-day-name {
  color: var(--color-primary);
}

.week-day-number {
  font-size: 1.25rem;
  font-weight: 700;
  color: var(--color-text);
  width: 2.1rem;
  height: 2.1rem;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
}

.week-day-number--today {
  background: var(--color-primary);
  color: #fff;
}

.week-day-month {
  font-size: 0.68rem;
  color: var(--color-text-faint);
}

.week-day-content {
  padding: 0.5rem 0.4rem;
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  flex: 1;
}

.week-payment-card {
  border-left: 3px solid var(--chip-color);
  border-radius: 0.375rem;
  padding: 0.35rem 0.45rem;
  background: var(--color-surface);
  border-top: 1px solid var(--color-border-light);
  border-right: 1px solid var(--color-border-light);
  border-bottom: 1px solid var(--color-border-light);
}

.week-day-col--today .week-payment-card {
  border-top-color: var(--color-border);
  border-right-color: var(--color-border);
  border-bottom-color: var(--color-border);
}

.week-payment-top {
  display: flex;
  align-items: center;
  gap: 0.3rem;
  margin-bottom: 0.2rem;
}

.week-payment-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: var(--chip-color);
  flex-shrink: 0;
}

.week-payment-name {
  font-size: 0.75rem;
  font-weight: 600;
  color: var(--color-text);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.week-payment-amount {
  font-size: 0.8rem;
  font-weight: 700;
  color: var(--color-text);
  padding-left: 0.95rem;
}

.week-day-total {
  display: flex;
  align-items: center;
  gap: 0.3rem;
  font-size: 0.75rem;
  font-weight: 700;
  color: var(--color-primary);
  justify-content: flex-end;
  padding: 0.1rem 0;
}

.week-day-empty {
  font-size: 0.75rem;
  color: var(--color-text-faint);
  text-align: center;
  padding: 0.75rem 0;
  margin: 0;
}
</style>
