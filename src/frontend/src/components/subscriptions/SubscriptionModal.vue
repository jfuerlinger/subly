<script setup lang="ts">
import { onMounted, onUnmounted } from 'vue'
import type { NewSubscriptionRequest } from '../../app/types/subscription'
import SubscriptionForm from './SubscriptionForm.vue'

const props = defineProps<{ show: boolean }>()

const emit = defineEmits<{
  close: []
  submit: [request: NewSubscriptionRequest]
}>()

function onSubmit(request: NewSubscriptionRequest) {
  emit('submit', request)
  emit('close')
}

function onKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape' && props.show) {
    emit('close')
  }
}

onMounted(() => document.addEventListener('keydown', onKeydown))
onUnmounted(() => document.removeEventListener('keydown', onKeydown))
</script>

<template>
  <Teleport to="body">
    <Transition name="modal">
      <div v-if="show" class="modal-backdrop" @click.self="emit('close')">
        <div class="modal-dialog" role="dialog" aria-modal="true" aria-labelledby="modal-title">
          <header class="modal-header">
            <h2 id="modal-title" class="modal-title">Neues Abo hinzufügen</h2>
            <button class="modal-close" type="button" :aria-label="'Schließen'" @click="emit('close')">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
                <line x1="18" y1="6" x2="6" y2="18"/>
                <line x1="6" y1="6" x2="18" y2="18"/>
              </svg>
            </button>
          </header>

          <div class="modal-body">
            <SubscriptionForm @submit="onSubmit" />
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
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
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1.125rem 1.25rem 0.875rem;
  border-bottom: 1px solid var(--color-border);
}

.modal-title {
  margin: 0;
  font-size: 1rem;
  font-weight: 700;
  color: var(--color-text);
}

.modal-close {
  border: none;
  background: transparent;
  padding: 0.25rem;
  cursor: pointer;
  color: var(--color-text-muted);
  border-radius: 0.375rem;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: background 0.1s, color 0.1s;
}

.modal-close:hover {
  background: var(--color-border-light);
  color: var(--color-text);
}

.modal-body {
  padding: 1.25rem;
}

/* Transition */
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
