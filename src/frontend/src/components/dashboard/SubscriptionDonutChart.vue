<script setup lang="ts">
import { computed, ref } from 'vue'
import type { Subscription } from '../../app/types/subscription'
import { formatCurrency } from '../../app/utils/formatting'
import { toMonthlyAmount } from '../../app/utils/subscriptionMath'

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

function categoryColorIndex(category: string): number {
  let hash = 0
  for (let i = 0; i < category.length; i++) {
    hash = (hash * 31 + category.charCodeAt(i)) >>> 0
  }
  return hash % FALLBACK_COLORS.length
}

const categoryData = computed(() => {
  const active = props.subscriptions.filter((s) => s.status === 'active')
  const map = new Map<string, { total: number; count: number; items: Subscription[] }>()

  for (const s of active) {
    const key = s.category.trim().toLowerCase()
    const existing = map.get(key) ?? { total: 0, count: 0, items: [] }
    map.set(key, {
      total: existing.total + toMonthlyAmount(s),
      count: existing.count + 1,
      items: [...existing.items, s],
    })
  }

  return Array.from(map.entries())
    .map(([category, data]) => ({
      category,
      label: category.charAt(0).toUpperCase() + category.slice(1),
      total: data.total,
      count: data.count,
      items: data.items,
      color: CATEGORY_COLORS[category] ?? FALLBACK_COLORS[categoryColorIndex(category)],
    }))
    .sort((a, b) => b.total - a.total)
})

// Tooltip state
const hoveredCategory = ref<string | null>(null)
const tooltipX = ref(0)
const tooltipY = ref(0)

const hoveredData = computed(() =>
  hoveredCategory.value
    ? categoryData.value.find((c) => c.category === hoveredCategory.value) ?? null
    : null,
)

function showTooltip(category: string, event: MouseEvent) {
  hoveredCategory.value = category
  positionTooltip(event)
}

function hideTooltip() {
  hoveredCategory.value = null
}

function positionTooltip(event: MouseEvent) {
  const offset = 14
  const estimatedWidth = 260
  tooltipX.value =
    event.clientX + estimatedWidth + offset > window.innerWidth
      ? event.clientX - estimatedWidth - offset
      : event.clientX + offset
  tooltipY.value = event.clientY + offset
}

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
            :stroke-width="hoveredCategory === seg.category ? STROKE_WIDTH + 5 : STROKE_WIDTH"
            :stroke-dasharray="seg.dasharray"
            :stroke-dashoffset="seg.dashoffset"
            stroke-linecap="butt"
            :transform="`rotate(-90 ${CENTER} ${CENTER})`"
            :style="{
              opacity: hoveredCategory && hoveredCategory !== seg.category ? 0.4 : 1,
              transition: 'opacity 0.15s ease, stroke-width 0.15s ease',
              cursor: 'pointer',
            }"
            @mouseenter="showTooltip(seg.category, $event)"
            @mouseleave="hideTooltip"
            @mousemove="positionTooltip($event)"
          />
        </svg>
        <div class="donut-center">
          <span class="donut-total">{{ formatCurrency(totalMonthly) }}</span>
          <span class="donut-sublabel">monatlich</span>
        </div>
      </div>

      <ul class="donut-legend">
        <li
          v-for="cat in categoryData"
          :key="cat.category"
          class="donut-legend-item"
          :class="{ 'is-hovered': hoveredCategory === cat.category }"
          @mouseenter="showTooltip(cat.category, $event)"
          @mouseleave="hideTooltip"
          @mousemove="positionTooltip($event)"
        >
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

  <Teleport to="body">
    <div
      v-if="hoveredData"
      class="donut-tooltip"
      :style="{ left: `${tooltipX}px`, top: `${tooltipY}px` }"
    >
      <div class="donut-tooltip-header">
        <span class="donut-tooltip-dot" :style="{ background: hoveredData.color }" />
        <strong>{{ hoveredData.label }}</strong>
      </div>
      <ul class="donut-tooltip-list">
        <li v-for="sub in hoveredData.items" :key="sub.id">
          <span class="donut-tooltip-sub-name">{{ sub.name }}</span>
          <span class="donut-tooltip-sub-price">{{ formatCurrency(toMonthlyAmount(sub)) }}/Mo.</span>
        </li>
      </ul>
      <div class="donut-tooltip-total">
        Gesamt: {{ formatCurrency(hoveredData.total) }}/Mo.
      </div>
    </div>
  </Teleport>
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
  cursor: default;
  border-radius: 4px;
  transition: background 0.12s ease;
}

.donut-legend-item.is-hovered {
  background: var(--color-surface-hover, rgba(0, 0, 0, 0.04));
  padding-left: 0.3rem;
  padding-right: 0.3rem;
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

/* Floating tooltip */
.donut-tooltip {
  position: fixed;
  z-index: 1000;
  background: var(--color-surface, #fff);
  border: 1px solid var(--color-border, #e5e7eb);
  border-radius: 8px;
  padding: 0.75rem;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.12);
  pointer-events: none;
  min-width: 180px;
  max-width: 260px;
}

.donut-tooltip-header {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-bottom: 0.5rem;
  font-size: 0.875rem;
}

.donut-tooltip-dot {
  width: 10px;
  height: 10px;
  border-radius: 2px;
  flex-shrink: 0;
}

.donut-tooltip-list {
  list-style: none;
  margin: 0;
  padding: 0;
}

.donut-tooltip-list li {
  display: flex;
  justify-content: space-between;
  gap: 1rem;
  padding: 0.2rem 0;
  font-size: 0.8rem;
}

.donut-tooltip-sub-name {
  color: var(--color-text-muted, #6b7280);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.donut-tooltip-sub-price {
  color: var(--color-text, #111827);
  font-variant-numeric: tabular-nums;
  white-space: nowrap;
}

.donut-tooltip-total {
  margin-top: 0.5rem;
  padding-top: 0.5rem;
  border-top: 1px solid var(--color-border-light, #f3f4f6);
  font-size: 0.8rem;
  font-weight: 600;
  text-align: right;
  color: var(--color-text);
}
</style>
