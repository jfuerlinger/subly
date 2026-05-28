<script setup lang="ts">
import { ref } from 'vue'
import { resetDatabase, type DatabaseResetResult } from '../app/api/adminApi'
import { useSubscriptionStore } from '../app/stores/subscriptionStore'

const store = useSubscriptionStore()

const isResetConfirmationOpen = ref(false)
const isResetting = ref(false)
const resetError = ref<string | null>(null)
const resetResult = ref<DatabaseResetResult | null>(null)

function openResetConfirmation() {
  resetError.value = null
  isResetConfirmationOpen.value = true
}

function cancelReset() {
  if (isResetting.value) {
    return
  }
  isResetConfirmationOpen.value = false
}

function formatCompletionTimestamp(value: string): string {
  return new Intl.DateTimeFormat('de-AT', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}

async function handleResetDatabase() {
  isResetting.value = true
  resetError.value = null
  resetResult.value = null
  try {
    resetResult.value = await resetDatabase()
    isResetConfirmationOpen.value = false
    await store.initialize()
  } catch {
    resetError.value = 'Fehler beim Zurücksetzen der Datenbank. Bitte versuche es erneut.'
  } finally {
    isResetting.value = false
  }
}
</script>

<template>
  <div class="view">
    <div class="view-header">
      <h1>Einstellungen</h1>
    </div>

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
          <h3>Datenbank zurücksetzen</h3>
          <p class="muted">Die komplette Datenbank wird gelöscht, alle Migrationen werden erneut angewendet und die Seed-Daten werden neu geladen.</p>
        </div>
      </div>
      <p v-if="resetError" class="error-message">{{ resetError }}</p>
      <div class="settings-actions">
        <button
          class="btn btn--danger"
          :disabled="isResetting"
          @click="openResetConfirmation"
        >
          {{ isResetting ? 'Wird zurückgesetzt…' : 'Datenbank zurücksetzen' }}
        </button>
      </div>

      <div v-if="resetResult" class="reset-summary">
        <h4>Durchgeführte Schritte</h4>
        <p class="muted">
          Ausgeführt am {{ formatCompletionTimestamp(resetResult.completedAtUtc) }}
        </p>
        <ol class="reset-steps">
          <li
            v-for="(step, index) in resetResult.steps"
            :key="`${index}-${step}`"
            class="reset-step"
          >
            <span class="reset-step-icon">✓</span>
            <span>{{ step }}</span>
          </li>
        </ol>
      </div>
    </div>

    <Teleport to="body">
      <Transition name="modal">
        <div v-if="isResetConfirmationOpen" class="modal-backdrop" @click.self="cancelReset">
          <div class="modal-dialog" role="dialog" aria-modal="true" aria-labelledby="reset-modal-title">
            <header class="modal-header">
              <h2 id="reset-modal-title" class="modal-title">Datenbank zurücksetzen</h2>
            </header>
            <div class="modal-body">
              <p>
                Möchtest du die Datenbank wirklich vollständig zurücksetzen?
                Dabei werden alle vorhandenen Daten gelöscht.
              </p>
              <p class="muted">
                Danach werden automatisch alle Migrationen und das Seeding erneut ausgeführt.
              </p>
              <p v-if="resetError" class="error-message modal-error">{{ resetError }}</p>
            </div>
            <footer class="modal-actions">
              <button class="btn btn--ghost" type="button" :disabled="isResetting" @click="cancelReset">
                Abbrechen
              </button>
              <button
                class="btn btn--danger"
                type="button"
                :disabled="isResetting"
                @click="handleResetDatabase"
              >
                <svg v-if="isResetting" class="btn-spinner" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M21 12a9 9 0 1 1-6.219-8.56"/>
                </svg>
                {{ isResetting ? 'Wird zurückgesetzt…' : 'Datenbank zurücksetzen' }}
              </button>
            </footer>
          </div>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>

<style scoped>
.settings-section {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  margin-bottom: 1rem;
}

.settings-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
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

.btn--ghost {
  background: var(--color-surface);
  color: var(--color-text);
  border: 1px solid var(--color-border);
}

.btn--ghost:not(:disabled):hover {
  background: var(--color-border-light);
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

.reset-summary {
  border: 1px solid var(--color-border);
  border-radius: 0.75rem;
  background: var(--color-bg);
  padding: 0.875rem 1rem;
}

.reset-summary h4 {
  margin: 0 0 0.25rem;
  font-size: 0.9375rem;
}

.reset-steps {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  margin: 0.75rem 0 0;
  padding: 0;
  list-style: none;
}

.reset-step {
  display: flex;
  align-items: flex-start;
  gap: 0.5rem;
}

.reset-step-icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 1.125rem;
  height: 1.125rem;
  flex-shrink: 0;
  border-radius: 999px;
  background: color-mix(in srgb, var(--color-success, #10b981) 16%, transparent);
  color: var(--color-success, #10b981);
  font-size: 0.75rem;
  font-weight: 700;
  margin-top: 0.1rem;
}

.modal-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.45);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: 1rem;
}

.modal-dialog {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: 1rem;
  width: 100%;
  max-width: 560px;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.18);
  display: flex;
  flex-direction: column;
}

.modal-header {
  padding: 1.125rem 1.25rem 0.875rem;
  border-bottom: 1px solid var(--color-border);
}

.modal-title {
  margin: 0;
  font-size: 1rem;
  font-weight: 700;
  color: var(--color-text);
}

.modal-body {
  padding: 1.25rem;
}

.modal-body p {
  margin: 0;
}

.modal-body p + p {
  margin-top: 0.625rem;
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.625rem;
  padding: 0 1.25rem 1.25rem;
}

.modal-enter-active,
.modal-leave-active {
  transition: opacity 0.18s ease;
}

.modal-enter-active .modal-dialog,
.modal-leave-active .modal-dialog {
  transition: transform 0.18s ease, opacity 0.18s ease;
}

.modal-enter-from,
.modal-leave-to {
  opacity: 0;
}

.modal-enter-from .modal-dialog,
.modal-leave-to .modal-dialog {
  transform: translateY(-12px);
  opacity: 0;
}
</style>
