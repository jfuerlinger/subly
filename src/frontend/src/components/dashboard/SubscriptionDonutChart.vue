<script setup lang="ts">
import { computed } from 'vue'
import type { Subscription } from '../../app/types/subscription'
import { formatCurrency } from '../../app/utils/formatting'

const props = defineProps<{
  subscriptions: Subscription[]
}>()

const CATEGORY_COLORS: Record<string, string> = {
  streaming: '#f97316',
  software: '#3b82f6',
  insurance: '#22c55e',
  telecom: '#06b6d4',
  energy: '#eab308',
  fitness: '#ef4444',
  news: '#a855f7',
  cloud: '#0ea5e9',
  membership: '#6366f1',
}

const FALLBACK_COLORS = [
  '#f97316', '#3b82f6', '#22c55e', '#06b6d4',
  '#eab308', '#ef4444', '#a855f7', '#0ea5e9', '#6366f1',
]

function toMonthlyAmount(subscription: Subscription): number {
  return subscription.cycle === 'yearly' ? subscription.price / 12 : subscription.price
}

const categoryData = computed(() => {
  const active = props.subscriptions.filter((s) => s.status === 'active')
  const map = new Map<string, { total: number; count: number }>()

  for (const s of active) {
    const key = s.category.toLowerCase()
    const existing = map.get(key) ?? { total: 0, count: 0 }
    map.set(key, { total: existing.total + toMonthlyAmount(s), count: existing.count + 1 })
  }

  return Array.from(map.entries())
    .map(([category, data], index) => ({
      category,
      label: category.charAt(0).toUpperCase() + category.slice(1),
      total: data.total,
      count: data.count,
      color: CATEGORY_COLORS[category] ?? FALLBACK_COLORS[index % FALLBACK_COLORS.length],
    }))
    .sort((a, b) => b.total - a.total)
})

const totalMonthly = computed(() =>
  categoryData.value.reduce((sum, c) => sum + c.total, 0),
)

const RADIUS = 58
const CENTER = 80
const STROKE_WIDTH = 20
const circumference = 2 * Math.PI * RADIUS

const segments = computed(() => {
  if (totalMonthly.value === 0) return []

  let cumulativeLength = 0
  return categoryData.value.map((cat) => {
    const segmentLength = (cat.total / totalMonthly.value) * circumference
    const dashoffset = -cumulativeLength
    cumulativeLength += segmentLength
    return {
      ...cat,
      dasharray: `${segmentLength} ${circumference - segmentLength}`,
      dashoffset,
    }
  })
})
</script>

<template>
  <section class="card">
    <h2>Aufteilung nach Kategorie</h2>

    <p v-if="categoryData.length === 0" class="muted">Keine aktiven Abonnements.</p>

    <div v-else class="donut-wrapper">
      <div class="donut-svg-container">
        <svg width="160" height="160" viewBox="0 0 160 160" aria-hidden="true">
          <circle
            :cx="CENTER"
            :cy="CENTER"
            :r="RADIUS"
            fill="none"
            stroke="#f3f4f6"
            :stroke-width="STROKE_WIDTH"
          />
          <circle
            v-for="seg in segments"
            :key="seg.category"
            :cx="CENTER"
            :cy="CENTER"
            :r="RADIUS"
            fill="none"
            :stroke="seg.color"
            :stroke-width="STROKE_WIDTH"
            :stroke-dasharray="seg.dasharray"
            :stroke-dashoffset="seg.dashoffset"
            stroke-linecap="butt"
            transform="rotate(-90 80 80)"
          />
        </svg>
        <div class="donut-center">
          <span class="donut-total">{{ formatCurrency(totalMonthly) }}</span>
          <span class="donut-sublabel">monatlich</span>
        </div>
      </div>

      <ul class="donut-legend">
        <li v-for="cat in categoryData" :key="cat.category" class="donut-legend-item">
          <span class="donut-dot" :style="{ background: cat.color }" />
          <span class="donut-name">
            {{ cat.label }}
            <span class="donut-count">({{ cat.count }})</span>
          </span>
          <span class="donut-value">{{ formatCurrency(cat.total) }}</span>
        </li>
      </ul>
    </div>
  </section>
</template>

<style scoped>
.donut-wrapper {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1.25rem;
}

.donut-svg-container {
  position: relative;
  display: inline-flex;
  align-items: center;
  justify-content: center;
}

.donut-center {
  position: absolute;
  display: flex;
  flex-direction: column;
  align-items: center;
  pointer-events: none;
}

.donut-total {
  font-size: 1.05rem;
  font-weight: 700;
  color: var(--color-text);
  line-height: 1.1;
}

.donut-sublabel {
  font-size: 0.7rem;
  color: var(--color-text-muted);
  margin-top: 0.15rem;
}

.donut-legend {
  list-style: none;
  margin: 0;
  padding: 0;
  width: 100%;
}

.donut-legend-item {
  display: grid;
  grid-template-columns: 14px 1fr auto;
  align-items: center;
  gap: 0.625rem;
  padding: 0.6rem 0;
  border-bottom: 1px solid var(--color-border-light);
  font-size: 0.875rem;
}

.donut-legend-item:last-child {
  border-bottom: none;
}

.donut-dot {
  width: 14px;
  height: 14px;
  border-radius: 3px;
  flex-shrink: 0;
}

.donut-name {
  font-weight: 500;
  color: var(--color-text);
}

.donut-count {
  color: var(--color-text-muted);
  font-weight: 400;
}

.donut-value {
  font-weight: 600;
  color: var(--color-text);
  text-align: right;
}
</style>
