<script setup lang="ts">
import { ref } from 'vue'
import { deleteAllData, seedData } from '../app/api/adminApi'
import { useSubscriptionStore } from '../app/stores/subscriptionStore'

const store = useSubscriptionStore()

const isDeleting = ref(false)
const isSeeding = ref(false)
const deleteError = ref<string | null>(null)
const seedError = ref<string | null>(null)

async function handleDeleteAllData() {
  if (!confirm('Alle Abonnement-Daten wirklich löschen? Diese Aktion kann nicht rückgängig gemacht werden.')) {
    return
  }
  isDeleting.value = true
  deleteError.value = null
  try {
    await deleteAllData()
    await store.initialize()
  } catch {
    deleteError.value = 'Fehler beim Löschen der Daten. Bitte versuche es erneut.'
  } finally {
    isDeleting.value = false
  }
}

async function handleSeedData() {
  if (!confirm('Alle bestehenden Abonnements werden gelöscht und durch Testdaten ersetzt. Fortfahren?')) {
    return
  }
  isSeeding.value = true
  seedError.value = null
  try {
    await seedData()
    await store.initialize()
  } catch {
    seedError.value = 'Fehler beim Seeden der Daten. Bitte versuche es erneut.'
  } finally {
    isSeeding.value = false
  }
}
</script>

<template>
  <div class="view">
    <div class="view-header">
      <h1>Einstellungen</h1>
    </div>

    <!-- Delete all data -->
    <div class="card settings-section">
      <div class="settings-section-header">
        <div class="settings-section-icon settings-section-icon--danger">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <polyline points="3 6 5 6 21 6"/>
            <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/>
            <path d="M10 11v6"/><path d="M14 11v6"/>
            <path d="M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2"/>
          </svg>
        </div>
        <div>
          <h3>Daten löschen</h3>
          <p class="muted">Alle Abonnements werden dauerhaft gelöscht. Diese Aktion kann nicht rückgängig gemacht werden.</p>
        </div>
      </div>
      <p v-if="deleteError" class="error-message">{{ deleteError }}</p>
      <button
        class="btn btn--danger"
        :disabled="isDeleting"
        @click="handleDeleteAllData"
      >
        <svg v-if="isDeleting" class="btn-spinner" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <path d="M21 12a9 9 0 1 1-6.219-8.56"/>
        </svg>
        {{ isDeleting ? 'Wird gelöscht…' : 'Alle Daten löschen' }}
      </button>
    </div>

    <!-- Seed data -->
    <div class="card settings-section">
      <div class="settings-section-header">
        <div class="settings-section-icon settings-section-icon--primary">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <polyline points="23 4 23 10 17 10"/>
            <path d="M20.49 15a9 9 0 1 1-.53-4.7L23 10"/>
          </svg>
        </div>
        <div>
          <h3>Testdaten laden</h3>
          <p class="muted">Bestehende Abonnements werden gelöscht und durch vordefinierte Beispieldaten ersetzt.</p>
        </div>
      </div>
      <p v-if="seedError" class="error-message">{{ seedError }}</p>
      <button
        class="btn btn--primary"
        :disabled="isSeeding"
        @click="handleSeedData"
      >
        <svg v-if="isSeeding" class="btn-spinner" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <path d="M21 12a9 9 0 1 1-6.219-8.56"/>
        </svg>
        {{ isSeeding ? 'Wird geladen…' : 'Testdaten laden' }}
      </button>
    </div>
  </div>
</template>

<style scoped>
.settings-section {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  margin-bottom: 1rem;
}

.settings-section-header {
  display: flex;
  gap: 1rem;
  align-items: flex-start;
}

.settings-section-icon {
  flex-shrink: 0;
  width: 2.5rem;
  height: 2.5rem;
  border-radius: 0.625rem;
  display: flex;
  align-items: center;
  justify-content: center;
}

.settings-section-icon--danger {
  background: color-mix(in srgb, var(--color-error, #ef4444) 12%, transparent);
  color: var(--color-error, #ef4444);
}

.settings-section-icon--primary {
  background: var(--color-primary-light);
  color: var(--color-primary);
}

.settings-section-header h3 {
  margin: 0 0 0.25rem;
  font-size: 0.9375rem;
}

.btn {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 1rem;
  border-radius: 0.5rem;
  font-size: 0.875rem;
  font-weight: 500;
  border: none;
  cursor: pointer;
  transition: opacity 0.15s, background 0.15s;
  align-self: flex-start;
}

.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn--danger {
  background: var(--color-error, #ef4444);
  color: #fff;
}

.btn--danger:not(:disabled):hover {
  background: color-mix(in srgb, var(--color-error, #ef4444) 85%, black);
}

.btn--primary {
  background: var(--color-primary);
  color: #fff;
}

.btn--primary:not(:disabled):hover {
  background: color-mix(in srgb, var(--color-primary) 85%, black);
}

.btn-spinner {
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.error-message {
  color: var(--color-error, #ef4444);
  font-size: 0.875rem;
  margin: 0;
}
</style>
