<script setup lang="ts">
import { computed, ref } from 'vue'
import type { Subscription } from '../../app/types/subscription'
import { formatCurrency } from '../../app/utils/formatting'
import { buildPaymentMethodBreakdown } from '../../app/utils/subscriptionMath'

const props = defineProps<{ subscriptions: Subscription[] }>()

const ROW_H = 34
const LABEL_W = 120
const BAR_GAP = 8
const BAR_X = LABEL_W + BAR_GAP
const VALUE_W = 76
const VIEW_W = 400
const BAR_MAX_W = VIEW_W - BAR_X - VALUE_W
const PAD_TOP = 6
const PAD_BOTTOM = 6

const PM_COLORS = [
  '#4f46e5', // indigo
  '#22c55e', // green
  '#f97316', // orange
  '#8b5cf6', // violet
  '#06b6d4', // cyan
  '#ef4444', // red
  '#eab308', // yellow
  '#3b82f6', // blue
]

function pmColor(index: number): string {
  return PM_COLORS[index % PM_COLORS.length]
}

function truncate(text: string, maxLen: number): string {
  return text.length > maxLen ? `${text.slice(0, maxLen - 1)}…` : text
}

const breakdown = computed(() => buildPaymentMethodBreakdown(props.subscriptions))
const maxTotal = computed(() => Math.max(...breakdown.value.map((b) => b.total), 1))
const viewH = computed(() => PAD_TOP + breakdown.value.length * ROW_H + PAD_BOTTOM)

const rows = computed(() =>
  breakdown.value.map((item, i) => {
    const yTop = PAD_TOP + i * ROW_H
    const yCenter = yTop + ROW_H / 2
    const barW = (item.total / maxTotal.value) * BAR_MAX_W
    return {
      ...item,
      yTop,
      yCenter,
      textY: yCenter + 4,
      barY: yCenter - 9,
      barW: barW > 0 ? Math.max(barW, 3) : 0,
      color: pmColor(i),
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
  tooltipX.value = Math.min(e.clientX + 12, window.innerWidth - 200)
  tooltipY.value = e.clientY - 50
}
</script>

<template>
  <section class="card">
    <h2>Zahlungsmethoden</h2>

    <p v-if="breakdown.length === 0" class="muted">Keine aktiven Abonnements.</p>

    <svg
      v-else
      :viewBox="`0 0 ${VIEW_W} ${viewH}`"
      width="100%"
      :style="{ maxHeight: `${viewH}px`, display: 'block' }"
      overflow="visible"
    >
      <g
        v-for="(row, i) in rows"
        :key="row.method"
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

        <!-- Label -->
        <text
          :x="LABEL_W - 4" :y="row.textY"
          text-anchor="end" font-size="11" fill="var(--color-text-muted)"
        >{{ truncate(row.method, 16) }}</text>

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
        >{{ formatCurrency(row.total) }}</text>
      </g>
    </svg>

    <Teleport to="body">
      <div
        v-if="hoveredIndex !== null && rows[hoveredIndex]"
        class="chart-tooltip"
        :style="{ left: `${tooltipX}px`, top: `${tooltipY}px` }"
      >
        <strong>{{ rows[hoveredIndex].method }}</strong>
        <span>{{ rows[hoveredIndex].count }} Abo{{ rows[hoveredIndex].count !== 1 ? 's' : '' }}</span>
        <span>{{ formatCurrency(rows[hoveredIndex].total) }}/Mo.</span>
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
</style>
