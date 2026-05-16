<script setup lang="ts">
import type { DashboardSummary } from '../../app/types/subscription'
import { formatCurrency } from '../../app/utils/formatting'

defineProps<{
  summary: DashboardSummary | null
}>()
</script>

<template>
  <section class="summary-grid">
    <article class="card">
      <h3>Monatlich</h3>
      <p>{{ summary ? formatCurrency(summary.monthlyTotal) : '–' }}</p>
    </article>
    <article class="card">
      <h3>Jährlich</h3>
      <p>{{ summary ? formatCurrency(summary.yearlyTotal) : '–' }}</p>
    </article>
    <article class="card">
      <h3>Aktive Abos</h3>
      <p>{{ summary?.activeSubscriptionsCount ?? 0 }}</p>
    </article>
    <article class="card">
      <h3>Anstehend (30 Tage)</h3>
      <p>{{ summary ? formatCurrency(summary.upcomingPaymentsTotal30Days) : '–' }}</p>
    </article>
  </section>
</template>
