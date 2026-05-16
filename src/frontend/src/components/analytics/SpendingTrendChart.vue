<script setup lang="ts">
import { computed, ref } from 'vue'
import type { Subscription } from '../../app/types/subscription'
import { formatCurrency } from '../../app/utils/formatting'
import { buildSpendingTrend } from '../../app/utils/subscriptionMath'

const props = defineProps<{ subscriptions: Subscription[] }>()

const data = computed(() => buildSpendingTrend(props.subscriptions))
const maxValue = computed(() => Math.max(...data.value.map((d) => d.total), 1))

// SVG layout constants
const VIEW_W = 520
const VIEW_H = 190
const PAD_TOP = 8
const PAD_RIGHT = 10
const PAD_BOTTOM = 34
const PAD_LEFT = 50
const PLOT_W = VIEW_W - PAD_LEFT - PAD_RIGHT
const PLOT_H = VIEW_H - PAD_TOP - PAD_BOTTOM
const N = 12
const SLOT_W = PLOT_W / N
const BAR_W = Math.round(SLOT_W * 0.6)
const BAR_OFFSET = (SLOT_W - BAR_W) / 2
const BOTTOM_Y = PAD_TOP + PLOT_H

function formatShort(value: number): string {
  return value >= 1000 ? `${(value / 1000).toFixed(1)}k\u202f€` : `${Math.round(value)}\u202f€`
}

const gridLines = computed(() =>
  [0.5, 1.0].map((frac) => ({
    y: PAD_TOP + PLOT_H - frac * PLOT_H,
    value: frac * maxValue.value,
  })),
)

const bars = computed(() =>
  data.value.map((d, i) => {
    const barH = (d.total / maxValue.value) * PLOT_H
    return {
      ...d,
      x: PAD_LEFT + i * SLOT_W + BAR_OFFSET,
      y: BOTTOM_Y - barH,
      height: barH > 0 ? Math.max(barH, 2) : 0,
      cx: PAD_LEFT + i * SLOT_W + SLOT_W / 2,
    }
  }),
)

const hasData = computed(() => data.value.some((d) => d.total > 0))

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
  tooltipX.value = Math.min(e.clientX + 12, window.innerWidth - 180)
  tooltipY.value = e.clientY - 44
}
</script>

<template>
  <section class="card">
    <h2>Ausgaben-Trend</h2>
    <p class="chart-subtitle muted">Kumulative Monatliche Kosten – letzte 12 Monate</p>

    <p v-if="!hasData" class="muted">Keine aktiven Abos in den letzten 12 Monaten.</p>

    <svg
      v-else
      :viewBox="`0 0 ${VIEW_W} ${VIEW_H}`"
      width="100%"
      :style="{ maxHeight: `${VIEW_H}px`, display: 'block' }"
      overflow="visible"
    >
      <!-- Grid lines and Y-axis labels -->
      <g v-for="line in gridLines" :key="line.y">
        <line
          :x1="PAD_LEFT" :y1="line.y"
          :x2="PAD_LEFT + PLOT_W" :y2="line.y"
          stroke="var(--color-border-light)" stroke-width="1"
        />
        <text
          :x="PAD_LEFT - 5" :y="line.y + 4"
          text-anchor="end" font-size="9" fill="var(--color-text-muted)"
        >{{ formatShort(line.value) }}</text>
      </g>

      <!-- X-axis baseline -->
      <line
        :x1="PAD_LEFT" :y1="BOTTOM_Y"
        :x2="PAD_LEFT + PLOT_W" :y2="BOTTOM_Y"
        stroke="var(--color-border)" stroke-width="1"
      />

      <!-- Bars with transparent hover zones -->
      <g
        v-for="(bar, i) in bars"
        :key="bar.monthKey"
        style="cursor: pointer"
        @mouseenter="onEnter(i, $event)"
        @mouseleave="onLeave"
        @mousemove="updateTooltip($event)"
      >
        <!-- Actual bar -->
        <rect
          v-if="bar.height > 0"
          :x="bar.x" :y="bar.y"
          :width="BAR_W" :height="bar.height"
          rx="3"
          :fill="hoveredIndex === i ? '#4338ca' : '#4f46e5'"
          style="transition: fill 0.12s ease"
        />
        <!-- Full-height transparent hit area -->
        <rect
          :x="bar.x" :y="PAD_TOP"
          :width="BAR_W" :height="PLOT_H"
          fill="transparent"
        />
      </g>

      <!-- X-axis labels -->
      <text
        v-for="bar in bars"
        :key="`lbl-${bar.monthKey}`"
        :x="bar.cx" :y="BOTTOM_Y + 16"
        text-anchor="middle" font-size="10" fill="var(--color-text-muted)"
      >{{ bar.label }}</text>
    </svg>

    <Teleport to="body">
      <div
        v-if="hoveredIndex !== null"
        class="chart-tooltip"
        :style="{ left: `${tooltipX}px`, top: `${tooltipY}px` }"
      >
        <strong>{{ data[hoveredIndex].label }}</strong>
        <span>{{ formatCurrency(data[hoveredIndex].total) }}/Mo.</span>
      </div>
    </Teleport>
  </section>
</template>

<style scoped>
.chart-subtitle {
  font-size: 0.75rem;
  margin-top: -0.25rem;
  margin-bottom: 0.75rem;
}

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
  gap: 0.125rem;
  white-space: nowrap;
}
</style>
