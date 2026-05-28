<template>
  <AppShell v-if="showShell">
    <RouterView />
  </AppShell>
  <RouterView v-else />
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import AppShell from './components/layout/AppShell.vue'
import { useThemeStore } from './app/stores/themeStore'

// Initialize theme store on app startup so the theme is applied immediately
useThemeStore()

const route = useRoute()
const showShell = computed(() => route.name !== 'auth')
</script>
