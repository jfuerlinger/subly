<script setup lang="ts">
import { computed } from 'vue'
import type { Subscription } from '../../app/types/subscription'
import { formatCurrency } from '../../app/utils/formatting'
import { toMonthlyAmount } from '../../app/utils/subscriptionMath'

const props = defineProps<{ subscriptions: Subscription[] }>()

const active = computed(() => props.subscriptions.filter((s) => s.status === 'active'))

const longestSubscription = computed(() => {
  if (active.value.length === 0) return null
  const sorted = [...active.value].sort(
    (a, b) => new Date(a.startedAt).getTime() - new Date(b.startedAt).getTime(),
  )
  const s = sorted[0]
  const months = Math.floor(
    (Date.now() - new Date(s.startedAt).getTime()) / (1000 * 60 * 60 * 24 * 30.44),
  )
  return { name: s.name, months }
})

const topCategory = computed(() => {
  if (active.value.length === 0) return null
  const map = new Map<string, number>()
  for (const s of active.value) {
    map.set(s.categoryName, (map.get(s.categoryName) ?? 0) + toMonthlyAmount(s))
  }
  const [cat, total] = [...map.entries()].sort((a, b) => b[1] - a[1])[0]
  return { category: cat.charAt(0).toUpperCase() + cat.slice(1), total }
})

const autoRenewalRatio = computed(() => {
  if (active.value.length === 0) return null
  const count = active.value.filter((s) => s.autoRenew).length
  const pct = Math.round((count / active.value.length) * 100)
  return { count, total: active.value.length, pct }
})

const avgCostPerSubscription = computed(() => {
  if (active.value.length === 0) return null
  const total = active.value.reduce((sum, s) => sum + toMonthlyAmount(s), 0)
  return total / active.value.length
})
</script>

<template>
  <section class="summary-grid">
    <article class="summary-card">
      <div class="summary-card-header">
        <div class="summary-card-icon">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/>
          </svg>
        </div>
        <span class="summary-card-label">Längstes Abo</span>
      </div>
      <p class="summary-card-value">{{ longestSubscription ? `${longestSubscription.months} Mo.` : '–' }}</p>
      <p class="summary-card-sub">{{ longestSubscription?.name ?? 'Keine aktiven Abos' }}</p>
    </article>

    <article class="summary-card">
      <div class="summary-card-header">
        <div class="summary-card-icon">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M20.59 13.41l-7.17 7.17a2 2 0 0 1-2.83 0L2 12V2h10l8.59 8.59a2 2 0 0 1 0 2.82z"/>
            <line x1="7" y1="7" x2="7.01" y2="7"/>
          </svg>
        </div>
        <span class="summary-card-label">Teuerste Kategorie</span>
      </div>
      <p class="summary-card-value">{{ topCategory?.category ?? '–' }}</p>
      <p class="summary-card-sub">{{ topCategory ? `${formatCurrency(topCategory.total)}/Mo.` : 'Keine aktiven Abos' }}</p>
    </article>

    <article class="summary-card">
      <div class="summary-card-header">
        <div class="summary-card-icon">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/>
            <polyline points="22 4 12 14.01 9 11.01"/>
          </svg>
        </div>
        <span class="summary-card-label">Auto-Renewal</span>
      </div>
      <p class="summary-card-value">{{ autoRenewalRatio ? `${autoRenewalRatio.pct}\u202f%` : '–' }}</p>
      <p class="summary-card-sub">
        {{ autoRenewalRatio
          ? `${autoRenewalRatio.count} von ${autoRenewalRatio.total} aktiven Abos`
          : 'Keine aktiven Abos'
        }}
      </p>
    </article>

    <article class="summary-card">
      <div class="summary-card-header">
        <div class="summary-card-icon">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <line x1="12" y1="1" x2="12" y2="23"/>
            <path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"/>
          </svg>
        </div>
        <span class="summary-card-label">Ø Kosten/Abo</span>
      </div>
      <p class="summary-card-value">{{ avgCostPerSubscription !== null ? formatCurrency(avgCostPerSubscription) : '–' }}</p>
      <p class="summary-card-sub">Monatlich pro aktivem Abo</p>
    </article>
  </section>
</template>
