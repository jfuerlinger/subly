<script setup lang="ts">
import { computed, ref } from 'vue'
import type { Subscription } from '../../app/types/subscription'
import { formatCurrency } from '../../app/utils/formatting'
import { toMonthlyAmount } from '../../app/utils/subscriptionMath'
import SubscriptionLogo from '../subscriptions/SubscriptionLogo.vue'

const props = defineProps<{ subscriptions: Subscription[] }>()

const MAX_ROWS = 8
const ROW_H = 34
const LABEL_W = 130
const BAR_GAP = 8
const BAR_X = LABEL_W + BAR_GAP
const VALUE_W = 72
const VIEW_W = 400
const BAR_MAX_W = VIEW_W - BAR_X - VALUE_W
const PAD_TOP = 6
const PAD_BOTTOM = 6

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
const FALLBACK_COLORS = ['#f97316', '#3b82f6', '#22c55e', '#06b6d4', '#eab308', '#ef4444', '#a855f7', '#0ea5e9', '#6366f1']

function barColor(category: string): string {
  const key = category.trim().toLowerCase()
  if (CATEGORY_COLORS[key]) return CATEGORY_COLORS[key]
  let hash = 0
  for (let j = 0; j < key.length; j++) hash = (hash * 31 + key.charCodeAt(j)) >>> 0
  return FALLBACK_COLORS[hash % FALLBACK_COLORS.length]
}

function truncate(text: string, maxLen: number): string {
  return text.length > maxLen ? `${text.slice(0, maxLen - 1)}…` : text
}

const items = computed(() => {
  return props.subscriptions
    .filter((s) => s.status === 'active')
    .map((s) => ({ ...s, monthly: toMonthlyAmount(s) }))
    .sort((a, b) => b.monthly - a.monthly)
    .slice(0, MAX_ROWS)
})

const maxMonthly = computed(() => Math.max(...items.value.map((s) => s.monthly), 1))
const viewH = computed(() => PAD_TOP + items.value.length * ROW_H + PAD_BOTTOM)

const rows = computed(() =>
  items.value.map((s, i) => {
    const yTop = PAD_TOP + i * ROW_H
    const yCenter = yTop + ROW_H / 2
    const barW = (s.monthly / maxMonthly.value) * BAR_MAX_W
    return {
      ...s,
      yTop,
      yCenter,
      textY: yCenter + 4,
      barY: yCenter - 9,
      barW: barW > 0 ? Math.max(barW, 3) : 0,
      color: barColor(s.category),
    }
  }),
)

const hoveredIndex = ref<number | null>(null)
const tooltipX = ref(0)
const tooltipY = ref(0)

function onEnter(i: number, e: MouseEvent) {
  hoveredIndex.value = i
  updateTooltip(e)
}
function onLeave() {
  hoveredIndex.value = null
}
function updateTooltip(e: MouseEvent) {
  tooltipX.value = Math.min(e.clientX + 12, window.innerWidth - 220)
  tooltipY.value = e.clientY - 56
}
</script>

<template>
  <section class="card">
    <h2>Top-Ausgaben</h2>

    <p v-if="items.length === 0" class="muted">Keine aktiven Abonnements.</p>

    <svg
      v-else
      :viewBox="`0 0 ${VIEW_W} ${viewH}`"
      width="100%"
      :style="{ maxHeight: `${viewH}px`, display: 'block' }"
      overflow="visible"
    >
      <g
        v-for="(row, i) in rows"
        :key="row.id"
        @mouseenter="onEnter(i, $event)"
        @mouseleave="onLeave"
        @mousemove="updateTooltip($event)"
        style="cursor: default"
      >
        <!-- Hover highlight -->
        <rect
          v-if="hoveredIndex === i"
          x="0" :y="row.yTop"
          :width="VIEW_W" :height="ROW_H"
          fill="var(--color-primary-light)" rx="4"
        />

        <!-- Name label -->
        <text
          :x="LABEL_W - 4" :y="row.textY"
          text-anchor="end" font-size="11" fill="var(--color-text-muted)"
        >{{ truncate(row.name, 18) }}</text>

        <!-- Bar track -->
        <rect
          :x="BAR_X" :y="row.barY"
          :width="BAR_MAX_W" height="18"
          fill="var(--color-border-light)" rx="4"
        />

        <!-- Value bar -->
        <rect
          :x="BAR_X" :y="row.barY"
          :width="row.barW" height="18"
          :fill="row.color" rx="4"
          :opacity="hoveredIndex !== null && hoveredIndex !== i ? 0.4 : 1"
          style="transition: opacity 0.12s ease"
        />

        <!-- Amount label -->
        <text
          :x="VIEW_W - 2" :y="row.textY"
          text-anchor="end" font-size="11" font-weight="600" fill="var(--color-text)"
        >{{ formatCurrency(row.monthly) }}</text>
      </g>
    </svg>

    <Teleport to="body">
      <div
        v-if="hoveredIndex !== null && rows[hoveredIndex]"
        class="chart-tooltip"
        :style="{ left: `${tooltipX}px`, top: `${tooltipY}px` }"
      >
        <strong class="top-chart-tooltip-title">
          <SubscriptionLogo :name="rows[hoveredIndex].name" :logo-url="rows[hoveredIndex].logoUrl" :size="18" />
          <span>{{ rows[hoveredIndex].name }}</span>
        </strong>
        <span>{{ rows[hoveredIndex].vendor }} · {{ rows[hoveredIndex].category }}</span>
        <span>
          {{ formatCurrency(rows[hoveredIndex].monthly) }}/Mo.
          · {{ rows[hoveredIndex].cycle === 'yearly' ? 'jährlich' : 'monatlich' }}
        </span>
      </div>
    </Teleport>
  </section>
</template>

<style scoped>
.chart-tooltip {
  position: fixed;
  z-index: 1000;
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: 6px;
  padding: 0.375rem 0.625rem;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  pointer-events: none;
  font-size: 0.8rem;
  display: flex;
  flex-direction: column;
  gap: 0.1rem;
  white-space: nowrap;
}

.top-chart-tooltip-title {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
}
</style>
