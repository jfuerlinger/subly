<script setup lang="ts">
import { computed, ref, watch } from 'vue'

const props = withDefaults(defineProps<{
  name: string
  logoUrl?: string | null
  size?: number
}>(), {
  logoUrl: null,
  size: 24,
})

const hasImageError = ref(false)

watch(() => props.logoUrl, () => {
  hasImageError.value = false
})

const normalizedLogoUrl = computed(() => {
  const value = props.logoUrl?.trim()
  return value && !hasImageError.value ? value : null
})

const initials = computed(() => {
  const parts = props.name
    .split(/\s+/)
    .map((part) => part.trim())
    .filter((part) => part.length > 0)

  if (parts.length === 0) {
    return 'AB'
  }

  return parts
    .slice(0, 2)
    .map((part) => part[0].toUpperCase())
    .join('')
})

const sizePx = computed(() => `${props.size}px`)
</script>

<template>
  <span class="subscription-logo" :style="{ width: sizePx, height: sizePx }" aria-hidden="true">
    <img
      v-if="normalizedLogoUrl"
      :src="normalizedLogoUrl"
      alt=""
      @error="hasImageError = true"
    />
    <span v-else class="subscription-logo__fallback">{{ initials }}</span>
  </span>
</template>

<style scoped>
.subscription-logo {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border-radius: 0.45rem;
  background: var(--color-border-light);
  border: 1px solid var(--color-border);
  overflow: hidden;
  flex-shrink: 0;
}

.subscription-logo img {
  width: 100%;
  height: 100%;
  object-fit: contain;
  display: block;
  background: #fff;
}

.subscription-logo__fallback {
  font-size: 0.62rem;
  font-weight: 700;
  color: var(--color-text-muted);
  letter-spacing: 0.03em;
  line-height: 1;
}
</style>
