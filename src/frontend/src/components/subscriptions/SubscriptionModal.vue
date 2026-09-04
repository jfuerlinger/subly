<script setup lang="ts">
import { computed, onMounted, onUnmounted } from 'vue'
import type { NewSubscriptionRequest } from '../../app/types/subscription'
import SubscriptionForm from './SubscriptionForm.vue'

const props = withDefaults(
  defineProps<{
    show: boolean
    mode?: 'create' | 'edit'
    initialValues?: NewSubscriptionRequest | null
  }>(),
  {
    mode: 'create',
    initialValues: null,
  },
)

const emit = defineEmits<{
  close: []
  submit: [request: NewSubscriptionRequest]
}>()

const title = computed(() => (props.mode === 'edit' ? 'Abo bearbeiten' : 'Neues Abo hinzufügen'))
const submitLabel = computed(() => (props.mode === 'edit' ? 'Änderungen speichern' : 'Abo hinzufügen'))

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
      <div v-if="props.show" class="modal-backdrop">
        <div class="modal-dialog" role="dialog" aria-modal="true" aria-labelledby="modal-title">
          <header class="modal-header">
            <h2 id="modal-title" class="modal-title">{{ title }}</h2>
          </header>

          <div class="modal-body">
            <SubscriptionForm
              :initial-values="props.initialValues"
              :submit-label="submitLabel"
              @submit="onSubmit"
              @cancel="emit('close')"
            />
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
