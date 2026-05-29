<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'

interface TourStep {
  routeName: string
  targetSelector: string
  title: string
  description: string
}

const props = defineProps<{
  active: boolean
}>()

const emit = defineEmits<{
  (event: 'complete'): void
  (event: 'skip'): void
}>()

const route = useRoute()
const router = useRouter()

const tourSteps: TourStep[] = [
  {
    routeName: 'dashboard',
    targetSelector: '[data-tour="sidebar-navigation"]',
    title: 'Navigation & Bereiche',
    description:
      'Über diese Navigation wechselst du zwischen Dashboard, Abos, Kalender, Analyse, Kategorien und Einstellungen.',
  },
  {
    routeName: 'dashboard',
    targetSelector: '[data-tour="dashboard-summary"]',
    title: 'Dashboard-Überblick',
    description:
      'Hier siehst du deine wichtigsten Kennzahlen auf einen Blick: Kosten, aktive Abos und anstehende Zahlungen.',
  },
  {
    routeName: 'dashboard',
    targetSelector: '[data-tour="dashboard-add-subscription"]',
    title: 'Neues Abo hinzufügen',
    description:
      'Mit „Hinzufügen“ legst du neue Subscriptions in wenigen Sekunden an.',
  },
  {
    routeName: 'subscriptions',
    targetSelector: '[data-tour="subscriptions-list"]',
    title: 'Abo-Verwaltung',
    description:
      'In dieser Tabelle kannst du Abos erstellen, bearbeiten, pausieren oder kündigen.',
  },
  {
    routeName: 'calendar',
    targetSelector: '[data-tour="calendar-toolbar"]',
    title: 'Zahlungs-Kalender',
    description:
      'Im Kalender planst du kommende Abbuchungen und wechselst zwischen Monats- und Wochenansicht.',
  },
  {
    routeName: 'analytics',
    targetSelector: '[data-tour="analytics-charts"]',
    title: 'Analyse & Trends',
    description:
      'Diese Diagramme helfen dir, Kostenentwicklungen, Forecasts und Top-Abos schnell zu verstehen.',
  },
  {
    routeName: 'categories',
    targetSelector: '[data-tour="categories-management"]',
    title: 'Kategorien',
    description:
      'Hier organisierst du deine Abos in Kategorien und behältst Struktur in deiner Liste.',
  },
  {
    routeName: 'settings',
    targetSelector: '[data-tour="settings-import-export"]',
    title: 'Import & Export',
    description:
      'In den Einstellungen kannst du deine Daten sichern oder bestehende Daten bequem importieren.',
  },
]

const currentStepIndex = ref(0)
const highlightedElement = ref<HTMLElement | null>(null)
const highlightRect = ref<DOMRect | null>(null)
const tooltipStyle = ref<Record<string, string>>({})
const preparingStep = ref(false)

const totalSteps = computed(() => tourSteps.length)
const currentStep = computed(() => tourSteps[currentStepIndex.value])
const nextButtonLabel = computed(() => (currentStepIndex.value === totalSteps.value - 1 ? 'Tour abschließen' : 'Weiter'))

async function startTour(): Promise<void> {
  currentStepIndex.value = 0
  await prepareCurrentStep()
}

async function goToNextStep(): Promise<void> {
  if (currentStepIndex.value >= totalSteps.value - 1) {
    emit('complete')
    return
  }

  currentStepIndex.value += 1
  await prepareCurrentStep()
}

async function goToPreviousStep(): Promise<void> {
  if (currentStepIndex.value === 0) {
    return
  }

  currentStepIndex.value -= 1
  await prepareCurrentStep()
}

function skipTour(): void {
  emit('skip')
}

async function prepareCurrentStep(): Promise<void> {
  if (!props.active) {
    return
  }

  const step = currentStep.value
  if (!step) {
    return
  }

  preparingStep.value = true
  highlightedElement.value = null
  highlightRect.value = null

  if (route.name !== step.routeName) {
    await router.push({ name: step.routeName })
  }

  await nextTick()
  const element = await waitForElement(step.targetSelector)
  if (element) {
    highlightedElement.value = element
    element.scrollIntoView({ block: 'center', behavior: 'smooth', inline: 'nearest' })
    updateOverlayPosition()
  } else {
    tooltipStyle.value = {
      top: '50%',
      left: '50%',
      transform: 'translate(-50%, -50%)',
    }
  }

  preparingStep.value = false
}

function updateOverlayPosition(): void {
  if (!highlightedElement.value) {
    return
  }

  const rect = highlightedElement.value.getBoundingClientRect()
  if (rect.width === 0 && rect.height === 0) {
    return
  }

  highlightRect.value = rect

  const tooltipWidth = 340
  const tooltipHeight = 220
  const gap = 16
  const viewportWidth = window.innerWidth
  const viewportHeight = window.innerHeight

  const preferredTop = rect.bottom + gap
  const hasBottomSpace = preferredTop + tooltipHeight <= viewportHeight - 12
  const top = hasBottomSpace
    ? preferredTop
    : Math.max(12, rect.top - tooltipHeight - gap)

  const maxLeft = viewportWidth - tooltipWidth - 12
  const left = Math.min(Math.max(12, rect.left), Math.max(12, maxLeft))

  tooltipStyle.value = {
    top: `${top}px`,
    left: `${left}px`,
    transform: 'none',
  }
}

async function waitForElement(selector: string): Promise<HTMLElement | null> {
  const timeoutAt = Date.now() + 2_000

  while (Date.now() < timeoutAt) {
    const candidate = document.querySelector(selector)
    if (candidate instanceof HTMLElement) {
      return candidate
    }

    await new Promise((resolve) => window.setTimeout(resolve, 50))
  }

  return null
}

function handleViewportChange(): void {
  updateOverlayPosition()
}

watch(
  () => props.active,
  async (isActive) => {
    if (!isActive) {
      highlightedElement.value = null
      highlightRect.value = null
      return
    }

    await startTour()
  },
  { immediate: true },
)

watch(
  () => route.fullPath,
  async () => {
    if (!props.active) {
      return
    }

    await nextTick()
    updateOverlayPosition()
  },
)

onMounted(() => {
  window.addEventListener('resize', handleViewportChange)
  window.addEventListener('scroll', handleViewportChange, true)
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', handleViewportChange)
  window.removeEventListener('scroll', handleViewportChange, true)
})
</script>

<template>
  <Teleport to="body">
    <Transition name="tour-fade">
      <div v-if="active" class="tour-layer" aria-live="polite">
        <div v-if="highlightRect" class="tour-highlight" :style="{
          top: `${highlightRect.top - 8}px`,
          left: `${highlightRect.left - 8}px`,
          width: `${highlightRect.width + 16}px`,
          height: `${highlightRect.height + 16}px`,
        }" />

        <section class="tour-card" role="dialog" aria-modal="true" :style="tooltipStyle">
          <p class="tour-progress">Schritt {{ currentStepIndex + 1 }} von {{ totalSteps }}</p>
          <h2>{{ currentStep?.title }}</h2>
          <p>{{ currentStep?.description }}</p>
          <p class="tour-note">Hinweis: Diese Tour ist eine gefuehrte Vorschau. Die markierten Elemente sind dabei nicht klickbar.</p>
          <p v-if="preparingStep" class="tour-loading">Inhalt wird vorbereitet…</p>

          <div class="tour-actions">
            <button type="button" class="tour-btn tour-btn--ghost" @click="skipTour">Überspringen</button>
            <button
              type="button"
              class="tour-btn tour-btn--ghost"
              :disabled="currentStepIndex === 0 || preparingStep"
              @click="goToPreviousStep"
            >
              Zurück
            </button>
            <button type="button" class="tour-btn tour-btn--primary" :disabled="preparingStep" @click="goToNextStep">
              {{ nextButtonLabel }}
            </button>
          </div>
        </section>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
.tour-layer {
  position: fixed;
  inset: 0;
  z-index: 120;
  pointer-events: none;
}

.tour-highlight {
  position: fixed;
  border-radius: 12px;
  border: 2px solid #818cf8;
  box-shadow: 0 0 0 9999px rgba(15, 23, 42, 0.62);
  pointer-events: none;
  transition: top 0.2s ease, left 0.2s ease, width 0.2s ease, height 0.2s ease;
}

.tour-card {
  position: fixed;
  width: min(340px, calc(100vw - 24px));
  background: #fff;
  border-radius: 14px;
  border: 1px solid #e5e7eb;
  box-shadow: 0 16px 36px rgba(15, 23, 42, 0.35);
  padding: 1rem;
  pointer-events: auto;
}

.tour-progress {
  margin: 0 0 0.4rem;
  font-size: 0.75rem;
  font-weight: 600;
  color: #4f46e5;
}

.tour-card h2 {
  margin: 0;
  font-size: 1.1rem;
}

.tour-card p {
  margin: 0.55rem 0 0;
  color: #4b5563;
  font-size: 0.9rem;
}

.tour-note {
  font-size: 0.8rem;
  color: #6b7280;
}

.tour-loading {
  font-size: 0.8rem;
  color: #6b7280;
}

.tour-actions {
  margin-top: 1rem;
  display: flex;
  justify-content: flex-end;
  gap: 0.5rem;
}

.tour-btn {
  border-radius: 10px;
  padding: 0.55rem 0.85rem;
  font-weight: 600;
  border: 1px solid transparent;
  cursor: pointer;
}

.tour-btn:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

.tour-btn--ghost {
  background: #fff;
  border-color: #d1d5db;
  color: #374151;
}

.tour-btn--ghost:hover:not(:disabled) {
  background: #f9fafb;
}

.tour-btn--primary {
  background: #4f46e5;
  color: #fff;
}

.tour-btn--primary:hover:not(:disabled) {
  background: #4338ca;
}

.tour-fade-enter-active,
.tour-fade-leave-active {
  transition: opacity 0.2s ease;
}

.tour-fade-enter-from,
.tour-fade-leave-to {
  opacity: 0;
}

@media (max-width: 900px) {
  .tour-card {
    width: calc(100vw - 24px);
  }
}
</style>
