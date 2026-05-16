<script setup lang="ts">
const navigation = [
  { to: '/dashboard', label: 'Dashboard' },
  { to: '/subscriptions', label: 'Abos' },
]
</script>

<template>
  <div class="app-shell">
    <aside class="sidebar">
      <div class="brand">Subly</div>
      <nav class="nav">
        <RouterLink
          v-for="item in navigation"
          :key="item.to"
          :to="item.to"
          class="nav-link"
          active-class="nav-link-active"
        >
          {{ item.label }}
        </RouterLink>
      </nav>
    </aside>
    <main class="content">
      <slot />
    </main>
  </div>
</template>
