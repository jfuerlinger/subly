<script setup lang="ts">
import { computed } from 'vue'
import type { SubscriptionStatus } from '../../app/types/subscription'
import { getSubscriptionStatusMeta } from '../../app/utils/subscriptionStatus'

const props = defineProps<{
  status: SubscriptionStatus
}>()

const meta = computed(() => getSubscriptionStatusMeta(props.status))
</script>

<template>
  <span class="status-badge" :class="`status-badge--${meta.tone}`">
    <svg
      v-if="meta.icon === 'active'"
      class="status-badge__icon"
      width="14"
      height="14"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      stroke-width="2"
      stroke-linecap="round"
      stroke-linejoin="round"
      aria-hidden="true"
    >
      <circle cx="12" cy="12" r="9" />
      <path d="m8.5 12 2.25 2.25L15.5 9.5" />
    </svg>
    <svg
      v-else-if="meta.icon === 'paused'"
      class="status-badge__icon"
      width="14"
      height="14"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      stroke-width="2"
      stroke-linecap="round"
      stroke-linejoin="round"
      aria-hidden="true"
    >
      <circle cx="12" cy="12" r="9" />
      <path d="M10 9v6" />
      <path d="M14 9v6" />
    </svg>
    <svg
      v-else
      class="status-badge__icon"
      width="14"
      height="14"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      stroke-width="2"
      stroke-linecap="round"
      stroke-linejoin="round"
      aria-hidden="true"
    >
      <circle cx="12" cy="12" r="9" />
      <path d="m9 9 6 6" />
      <path d="m15 9-6 6" />
    </svg>
    <span class="status-badge__label">{{ meta.label }}</span>
  </span>
</template>
