<template>
  <AppShell>
    <RouterView />
  </AppShell>
</template>

<script setup lang="ts">
import AppShell from './components/layout/AppShell.vue'
import { useThemeStore } from './app/stores/themeStore'

// Initialize theme store on app startup so the theme is applied immediately
useThemeStore()
</script>
