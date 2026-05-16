<script setup lang="ts">
import type { DashboardSummary } from '../../app/types/subscription'
import { formatCurrency } from '../../app/utils/formatting'

defineProps<{
  summary: DashboardSummary | null
}>()
</script>

<template>
  <section class="summary-grid">
    <!-- Monatlich -->
    <article class="summary-card">
      <div class="summary-card-header">
        <div class="summary-card-icon">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <rect x="3" y="4" width="18" height="18" rx="2" ry="2"/>
            <line x1="16" y1="2" x2="16" y2="6"/><line x1="8" y1="2" x2="8" y2="6"/>
            <line x1="3" y1="10" x2="21" y2="10"/>
          </svg>
        </div>
        <span class="summary-card-label">Monatlich</span>
      </div>
      <p class="summary-card-value">{{ summary ? formatCurrency(summary.monthlyTotal) : '–' }}</p>
      <p class="summary-card-sub">Aktuelle Belastung</p>
    </article>

    <!-- Jährlich -->
    <article class="summary-card">
      <div class="summary-card-header">
        <div class="summary-card-icon">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <line x1="18" y1="20" x2="18" y2="10"/><line x1="12" y1="20" x2="12" y2="4"/>
            <line x1="6" y1="20" x2="6" y2="14"/><line x1="2" y1="20" x2="22" y2="20"/>
          </svg>
        </div>
        <span class="summary-card-label">Jährliche Belastung</span>
      </div>
      <p class="summary-card-value">{{ summary ? formatCurrency(summary.yearlyTotal) : '–' }}</p>
      <p class="summary-card-sub">{{ summary?.activeSubscriptionsCount ?? 0 }} aktive Abos</p>
    </article>

    <!-- Aktive Abos / Pro Tag -->
    <article class="summary-card">
      <div class="summary-card-header">
        <div class="summary-card-icon">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <circle cx="12" cy="12" r="10"/>
            <polyline points="12 6 12 12 16 14"/>
          </svg>
        </div>
        <span class="summary-card-label">Pro Tag</span>
      </div>
      <p class="summary-card-value">
        {{ summary ? formatCurrency(summary.monthlyTotal / 30) : '–' }}
      </p>
      <p class="summary-card-sub">Durchschnitt 30 Tage</p>
    </article>

    <!-- Anstehend 30 Tage -->
    <article class="summary-card">
      <div class="summary-card-header">
        <div class="summary-card-icon">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"/>
            <path d="M13.73 21a2 2 0 0 1-3.46 0"/>
          </svg>
        </div>
        <span class="summary-card-label">Anstehend (30T)</span>
      </div>
      <p class="summary-card-value">{{ summary ? formatCurrency(summary.upcomingPaymentsTotal30Days) : '–' }}</p>
      <p class="summary-card-sub">{{ summary?.upcomingPaymentsCount30Days ?? 0 }} Zahlungen</p>
    </article>
  </section>
</template>
