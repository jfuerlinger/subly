<script setup lang="ts">
import { computed, nextTick, onMounted, ref } from 'vue'
import { useCategoryStore } from '../app/stores/categoriesStore'
import { useSubscriptionStore } from '../app/stores/subscriptionStore'

const categoryStore = useCategoryStore()
const subscriptionStore = useSubscriptionStore()

// ─── Colour palette for unknown/custom categories ────────

const PALETTE = [
  '#6366f1', '#8b5cf6', '#ec4899', '#f43f5e', '#f97316',
  '#eab308', '#22c55e', '#10b981', '#06b6d4', '#3b82f6',
]

const KNOWN_CATEGORY_COLORS: Record<string, string> = {
  streaming: 'var(--category-streaming)',
  software: 'var(--category-software)',
  insurance: 'var(--category-insurance)',
  telecom: 'var(--category-telecom)',
  energy: 'var(--category-energy)',
  fitness: 'var(--category-fitness)',
  news: 'var(--category-news)',
  cloud: 'var(--category-cloud)',
  membership: 'var(--category-membership)',
}

function categoryColor(name: string): string {
  if (KNOWN_CATEGORY_COLORS[name]) return KNOWN_CATEGORY_COLORS[name]
  let hash = 0
  for (let i = 0; i < name.length; i++) {
    hash = (hash * 31 + name.charCodeAt(i)) >>> 0
  }
  return PALETTE[hash % PALETTE.length]
}

// ─── Subscriptions per category ──────────────────────────

function subscriptionsForCategory(categoryId: string) {
  return subscriptionStore.subscriptions.filter((s) => s.categoryId === categoryId)
}

// ─── Expanded cards ──────────────────────────────────────

const expandedIds = ref<Set<string>>(new Set())

function toggleExpanded(id: string) {
  const next = new Set(expandedIds.value)
  if (next.has(id)) {
    next.delete(id)
  } else {
    next.add(id)
  }
  expandedIds.value = next
}

const draggedSubscriptionId = ref<string | null>(null)
const moveError = ref<string | null>(null)

function startTouchDragging(subscriptionId: string) {
  draggedSubscriptionId.value = subscriptionId
  moveError.value = null
}

function startDragging(subscriptionId: string, event: DragEvent) {
  draggedSubscriptionId.value = subscriptionId
  moveError.value = null
  event.dataTransfer?.setData('text/plain', subscriptionId)
  if (event.dataTransfer) event.dataTransfer.effectAllowed = 'move'
}

function stopDragging() {
  draggedSubscriptionId.value = null
}

function allowDrop(event: DragEvent) {
  event.preventDefault()
  if (event.dataTransfer) event.dataTransfer.dropEffect = 'move'
}

async function moveSubscription(subscriptionId: string, categoryId: string) {
  const subscription = subscriptionStore.subscriptions.find((item) => item.id === subscriptionId)
  if (!subscription || subscription.categoryId === categoryId) return

  moveError.value = null
  try {
    await subscriptionStore.updateCategory(subscriptionId, categoryId)
  } catch {
    moveError.value = `„${subscription.name}“ konnte nicht verschoben werden.`
  } finally {
    stopDragging()
  }
}

function dropSubscription(categoryId: string, event: DragEvent) {
  event.preventDefault()
  const subscriptionId = event.dataTransfer?.getData('text/plain') || draggedSubscriptionId.value
  if (subscriptionId) void moveSubscription(subscriptionId, categoryId)
}

function releaseTouchDrop(categoryId: string) {
  if (draggedSubscriptionId.value) void moveSubscription(draggedSubscriptionId.value, categoryId)
}

// ─── Rename ──────────────────────────────────────────────

const renamingId = ref<string | null>(null)
const renameValue = ref('')
const renameInputRef = ref<HTMLInputElement | null>(null)
const renameError = ref<string | null>(null)

function startRename(id: string, currentName: string) {
  renamingId.value = id
  renameValue.value = currentName
  renameError.value = null
  nextTick(() => renameInputRef.value?.select())
}

function cancelRename() {
  renamingId.value = null
  renameValue.value = ''
  renameError.value = null
}

async function commitRename(id: string) {
  const trimmed = renameValue.value.trim()
  if (!trimmed) {
    renameError.value = 'Name darf nicht leer sein.'
    return
  }
  renameError.value = null
  try {
    await categoryStore.rename(id, trimmed)
    renamingId.value = null
  } catch {
    renameError.value = 'Umbenennen fehlgeschlagen.'
  }
}

function onRenameKeydown(event: KeyboardEvent, id: string) {
  if (event.key === 'Enter') commitRename(id)
  if (event.key === 'Escape') cancelRename()
}

// ─── Delete category ─────────────────────────────────────

const deletingCategory = ref<{ id: string; name: string; subscriptionCount: number } | null>(null)
const replacementCategoryId = ref('')
const deleteError = ref<string | null>(null)
const isDeleting = ref(false)

function startDelete(id: string, name: string, subscriptionCount: number) {
  if (subscriptionCount === 0) {
    void deleteCategory(id)
    return
  }

  deletingCategory.value = { id, name, subscriptionCount }
  replacementCategoryId.value = categoryStore.categories.find((category) => category.id !== id)?.id ?? ''
  deleteError.value = null
}

function cancelDelete() {
  deletingCategory.value = null
  replacementCategoryId.value = ''
  deleteError.value = null
}

async function deleteCategory(id: string, replacementId?: string) {
  isDeleting.value = true
  deleteError.value = null
  try {
    await categoryStore.remove(id, replacementId)
    await subscriptionStore.initialize()
    cancelDelete()
  } catch {
    if (deletingCategory.value) {
      deleteError.value = 'Kategorie konnte nicht gelöscht werden.'
    } else {
      categoryStore.error = 'Kategorie konnte nicht gelöscht werden.'
    }
  } finally {
    isDeleting.value = false
  }
}

async function confirmDelete() {
  if (!deletingCategory.value || !replacementCategoryId.value) {
    deleteError.value = 'Bitte wähle eine Ersatzkategorie aus.'
    return
  }

  await deleteCategory(deletingCategory.value.id, replacementCategoryId.value)
}

// ─── Create new category ─────────────────────────────────

const showCreateForm = ref(false)
const createName = ref('')
const createError = ref<string | null>(null)
const createInputRef = ref<HTMLInputElement | null>(null)
const isCreating = ref(false)

function openCreateForm() {
  showCreateForm.value = true
  createName.value = ''
  createError.value = null
  nextTick(() => createInputRef.value?.focus())
}

function cancelCreate() {
  showCreateForm.value = false
  createName.value = ''
  createError.value = null
}

async function submitCreate() {
  const trimmed = createName.value.trim()
  if (!trimmed) {
    createError.value = 'Name darf nicht leer sein.'
    return
  }
  isCreating.value = true
  createError.value = null
  try {
    await categoryStore.create(trimmed)
    showCreateForm.value = false
    createName.value = ''
  } catch {
    createError.value = 'Kategorie konnte nicht erstellt werden.'
  } finally {
    isCreating.value = false
  }
}

function onCreateKeydown(event: KeyboardEvent) {
  if (event.key === 'Enter') submitCreate()
  if (event.key === 'Escape') cancelCreate()
}

// ─── Computed display data ───────────────────────────────

const enrichedCategories = computed(() =>
  categoryStore.categories.map((cat) => ({
    ...cat,
    color: categoryColor(cat.name),
    subscriptions: subscriptionsForCategory(cat.id),
  })),
)

onMounted(async () => {
  const promises: Promise<void>[] = []
  if (categoryStore.categories.length === 0) promises.push(categoryStore.initialize())
  if (subscriptionStore.subscriptions.length === 0) promises.push(subscriptionStore.initialize())
  await Promise.all(promises)
  expandedIds.value = new Set(categoryStore.categories.map((category) => category.id))
})
</script>

<template>
  <section class="view">
    <header class="view-header">
      <h1>Kategorien</h1>
      <p v-if="categoryStore.error" class="error">{{ categoryStore.error }}</p>
      <div class="view-header-actions">
        <button class="btn-primary" data-tour="categories-management" @click="openCreateForm">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
            <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
          </svg>
          Neue Kategorie
        </button>
      </div>
    </header>

    <!-- Create form inline card -->
    <Transition name="slide-down">
      <div v-if="showCreateForm" class="card create-form-card">
        <div class="create-form-header">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M20.59 13.41l-7.17 7.17a2 2 0 0 1-2.83 0L2 12V2h10l8.59 8.59a2 2 0 0 1 0 2.82z"/>
            <line x1="7" y1="7" x2="7.01" y2="7"/>
          </svg>
          <span>Neue Kategorie anlegen</span>
        </div>
        <div class="create-form-row">
          <input
            ref="createInputRef"
            v-model="createName"
            type="text"
            class="create-input"
            placeholder="Kategoriename eingeben…"
            :disabled="isCreating"
            @keydown="onCreateKeydown"
          />
          <button class="btn-primary btn-sm" :disabled="isCreating" @click="submitCreate">
            <svg v-if="isCreating" class="spinner" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
              <path d="M21 12a9 9 0 1 1-6.219-8.56"/>
            </svg>
            {{ isCreating ? 'Erstellen…' : 'Erstellen' }}
          </button>
          <button class="btn-ghost btn-sm" :disabled="isCreating" @click="cancelCreate">Abbrechen</button>
        </div>
        <p v-if="createError" class="field-error">{{ createError }}</p>
      </div>
    </Transition>

    <!-- Loading skeleton -->
    <div v-if="categoryStore.loading" class="categories-grid">
      <div v-for="i in 6" :key="i" class="category-card skeleton" />
    </div>

    <!-- Empty state -->
    <div v-else-if="enrichedCategories.length === 0" class="empty-state card">
      <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" class="empty-icon">
        <path d="M20.59 13.41l-7.17 7.17a2 2 0 0 1-2.83 0L2 12V2h10l8.59 8.59a2 2 0 0 1 0 2.82z"/>
        <line x1="7" y1="7" x2="7.01" y2="7"/>
      </svg>
      <p>Noch keine Kategorien vorhanden.</p>
      <button class="btn-primary" @click="openCreateForm">Erste Kategorie anlegen</button>
    </div>

    <!-- Category cards grid -->
    <div v-else class="categories-grid">
      <div
        v-for="cat in enrichedCategories"
        :key="cat.id"
        class="category-card"
        :class="{ 'category-card--expanded': expandedIds.has(cat.id), 'category-card--drop-target': draggedSubscriptionId }"
        @dragover="allowDrop"
        @drop="dropSubscription(cat.id, $event)"
        @pointerup="releaseTouchDrop(cat.id)"
      >
        <!-- Card header -->
        <div class="category-card-header">
          <div class="category-icon" :style="{ background: `color-mix(in srgb, ${cat.color} 15%, transparent)`, color: cat.color }">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M20.59 13.41l-7.17 7.17a2 2 0 0 1-2.83 0L2 12V2h10l8.59 8.59a2 2 0 0 1 0 2.82z"/>
              <line x1="7" y1="7" x2="7.01" y2="7"/>
            </svg>
          </div>

          <!-- Rename mode -->
          <div v-if="renamingId === cat.id" class="rename-row">
            <input
              ref="renameInputRef"
              v-model="renameValue"
              type="text"
              class="rename-input"
              @keydown="onRenameKeydown($event, cat.id)"
            />
            <button class="icon-action-btn icon-action-btn--confirm" title="Bestätigen" @click="commitRename(cat.id)">
              <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
                <polyline points="20 6 9 17 4 12"/>
              </svg>
            </button>
            <button class="icon-action-btn" title="Abbrechen" @click="cancelRename">
              <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
                <line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>
              </svg>
            </button>
          </div>

          <!-- Display mode -->
          <div v-else class="category-name-row">
            <span class="category-name">{{ cat.name }}</span>
            <span
              class="category-count"
              :style="{ background: `color-mix(in srgb, ${cat.color} 12%, transparent)`, color: cat.color }"
              :title="cat.subscriptions.length > 0 ? cat.subscriptions.map((s) => s.name).join('\n') : 'Keine Abos in dieser Kategorie'"
            >
              {{ cat.subscriptions.length }}
            </span>
            <button class="icon-action-btn" title="Umbenennen" @click="startRename(cat.id, cat.name)">
              <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
                <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
              </svg>
            </button>
            <button class="icon-action-btn icon-action-btn--danger" title="Löschen" @click="startDelete(cat.id, cat.name, cat.subscriptions.length)">
              <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <polyline points="3 6 5 6 21 6"/><path d="M19 6l-1 14H6L5 6m3 0V4h8v2m-6 4v6m4-6v6"/>
              </svg>
            </button>
          </div>

          <button
            class="icon-action-btn expand-btn"
            :title="expandedIds.has(cat.id) ? 'Einklappen' : 'Abos anzeigen'"
            @click="toggleExpanded(cat.id)"
          >
            <svg
              width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"
              stroke-linecap="round" stroke-linejoin="round"
              :style="{ transform: expandedIds.has(cat.id) ? 'rotate(180deg)' : 'none', transition: 'transform 0.2s' }"
            >
              <polyline points="6 9 12 15 18 9"/>
            </svg>
          </button>
        </div>

        <p v-if="renamingId === cat.id && renameError" class="field-error">{{ renameError }}</p>
        <p v-if="moveError" class="field-error category-move-error">{{ moveError }}</p>

        <!-- Colour accent bar -->
        <div class="category-accent" :style="{ background: cat.color }" />

        <!-- Subscriptions list (expanded) -->
        <Transition name="expand">
          <div v-if="expandedIds.has(cat.id)" class="subscription-list">
            <p v-if="cat.subscriptions.length === 0" class="subscription-list-empty">
              Keine Abos in dieser Kategorie.
            </p>
            <ul v-else>
              <li v-for="sub in cat.subscriptions" :key="sub.id" class="subscription-list-item">
                <span
                  class="sub-name"
                  draggable="true"
                  :aria-label="`${sub.name} verschieben`"
                  @dragstart="startDragging(sub.id, $event)"
                  @dragend="stopDragging"
                  @pointerdown="startTouchDragging(sub.id)"
                >{{ sub.name }}</span>
              </li>
            </ul>
          </div>
        </Transition>
      </div>
    </div>

    <Teleport to="body">
      <Transition name="modal">
        <div v-if="deletingCategory" class="modal-backdrop">
          <div class="modal-dialog" role="dialog" aria-modal="true" aria-labelledby="delete-category-title">
            <header class="modal-header">
              <h2 id="delete-category-title" class="modal-title">Kategorie löschen?</h2>
            </header>
            <div class="modal-body">
              <p>
                Der Kategorie „{{ deletingCategory.name }}“ sind {{ deletingCategory.subscriptionCount }}
                {{ deletingCategory.subscriptionCount === 1 ? 'Abo' : 'Abos' }} zugeordnet.
                Wähle eine Ersatzkategorie, bevor sie gelöscht wird.
              </p>
              <label class="replacement-label">
                Ersatzkategorie
                <select v-model="replacementCategoryId" class="replacement-select" :disabled="isDeleting">
                  <option disabled value="">Kategorie auswählen</option>
                  <option v-for="category in categoryStore.categories.filter((category) => category.id !== deletingCategory?.id)" :key="category.id" :value="category.id">
                    {{ category.name }}
                  </option>
                </select>
              </label>
              <p v-if="deleteError" class="field-error">{{ deleteError }}</p>
              <div class="delete-actions">
                <button class="btn-ghost btn-sm" :disabled="isDeleting" @click="cancelDelete">Abbrechen</button>
                <button class="btn-danger btn-sm" :disabled="isDeleting || !replacementCategoryId" @click="confirmDelete">
                  {{ isDeleting ? 'Löschen…' : 'Abos verschieben & Kategorie löschen' }}
                </button>
              </div>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>
  </section>
</template>

<style scoped>
/* ─── Grid ─────────────────────────────────────────────── */

.categories-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 1rem;
}

/* ─── Category card ─────────────────────────────────────── */

.category-card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: 0.875rem;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  transition: box-shadow 0.15s;
}

.category-card:hover {
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.07);
}

.category-card--expanded {
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.07);
}

.category-card--drop-target {
  transition: box-shadow 0.15s, border-color 0.15s;
}

.category-card--drop-target:hover {
  border-color: var(--color-primary);
  box-shadow: 0 0 0 2px color-mix(in srgb, var(--color-primary) 18%, transparent);
}

.category-accent {
  height: 3px;
  flex-shrink: 0;
}

.category-card-header {
  display: flex;
  align-items: center;
  gap: 0.625rem;
  padding: 0.875rem 1rem 0.875rem;
}

.category-icon {
  width: 2.25rem;
  height: 2.25rem;
  border-radius: 0.5rem;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

/* ─── Name row ──────────────────────────────────────────── */

.category-name-row {
  flex: 1;
  display: flex;
  align-items: center;
  gap: 0.4rem;
  min-width: 0;
}

.category-name {
  font-size: 0.9375rem;
  font-weight: 600;
  color: var(--color-text);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  text-transform: capitalize;
}

.category-count {
  font-size: 0.75rem;
  font-weight: 600;
  padding: 0.15rem 0.45rem;
  border-radius: 999px;
  flex-shrink: 0;
}

/* ─── Rename row ────────────────────────────────────────── */

.rename-row {
  flex: 1;
  display: flex;
  align-items: center;
  gap: 0.375rem;
  min-width: 0;
}

.rename-input {
  flex: 1;
  min-width: 0;
  border: 1px solid var(--color-primary);
  border-radius: 0.375rem;
  padding: 0.3rem 0.5rem;
  font-size: 0.875rem;
  background: var(--color-surface);
  color: var(--color-text);
  outline: none;
}

/* ─── Action buttons ────────────────────────────────────── */

.icon-action-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 1.75rem;
  height: 1.75rem;
  border: none;
  border-radius: 0.375rem;
  background: transparent;
  color: var(--color-text-muted);
  cursor: pointer;
  flex-shrink: 0;
  padding: 0;
  transition: background 0.1s, color 0.1s;
}

.icon-action-btn:hover {
  background: var(--color-border-light);
  color: var(--color-text);
}

.icon-action-btn--confirm {
  color: #16a34a;
}

.icon-action-btn--confirm:hover {
  background: #dcfce7;
  color: #15803d;
}

.icon-action-btn--danger:hover {
  background: #fef2f2;
  color: var(--color-danger);
}

.expand-btn {
  margin-left: auto;
}

/* ─── Subscriptions list ────────────────────────────────── */

.subscription-list {
  border-top: 1px solid var(--color-border-light);
  padding: 0.625rem 1rem 0.75rem;
}

.subscription-list ul {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.subscription-list-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.3rem 0.25rem;
  border-radius: 0.375rem;
  font-size: 0.8125rem;
}

.subscription-list-item:hover {
  background: var(--color-border-light);
}

.sub-name {
  cursor: grab;
  touch-action: none;
}

.sub-name:active {
  cursor: grabbing;
}

.category-move-error {
  margin: 0 1rem 0.5rem;
}

.sub-name {
  font-weight: 500;
  color: var(--color-text);
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.sub-vendor {
  font-size: 0.75rem;
  flex-shrink: 0;
}

.sub-status {
  flex-shrink: 0;
}

.subscription-list-empty {
  margin: 0;
  font-size: 0.8125rem;
  color: var(--color-text-faint);
  text-align: center;
  padding: 0.25rem 0;
}

/* ─── Create form ───────────────────────────────────────── */

.create-form-card {
  display: flex;
  flex-direction: column;
  gap: 0.625rem;
}

.create-form-header {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-weight: 600;
  font-size: 0.9375rem;
  color: var(--color-text);
}

.create-form-row {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.create-input {
  flex: 1;
  min-width: 0;
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  padding: 0.4rem 0.65rem;
  font-size: 0.875rem;
  background: var(--color-surface);
  color: var(--color-text);
  outline: none;
  transition: border-color 0.15s;
}

.create-input:focus {
  border-color: var(--color-primary);
}

/* ─── Buttons ───────────────────────────────────────────── */

.btn-sm {
  padding: 0.35rem 0.75rem;
  font-size: 0.8125rem;
}

.btn-ghost {
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  background: var(--color-surface);
  color: var(--color-text-muted);
  cursor: pointer;
  font-size: 0.8125rem;
  font-weight: 500;
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  transition: background 0.1s;
}

.btn-ghost:hover {
  background: var(--color-border-light);
}

/* ─── Skeleton ──────────────────────────────────────────── */

.skeleton {
  min-height: 80px;
  background: linear-gradient(90deg, var(--color-border-light) 25%, var(--color-border) 50%, var(--color-border-light) 75%);
  background-size: 200% 100%;
  animation: shimmer 1.4s ease-in-out infinite;
}

@keyframes shimmer {
  0% { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}

/* ─── Empty state ───────────────────────────────────────── */

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.75rem;
  padding: 3rem 2rem;
  text-align: center;
}

.empty-icon {
  color: var(--color-text-faint);
}

.empty-state p {
  margin: 0;
  color: var(--color-text-muted);
}

/* ─── Errors ────────────────────────────────────────────── */

.field-error {
  margin: 0 0 0 0.125rem;
  font-size: 0.8rem;
  color: var(--color-danger);
  padding: 0 1rem 0.5rem;
}

/* ─── Delete dialog ─────────────────────────────────────── */

.modal-backdrop {
  position: fixed;
  inset: 0;
  z-index: 1000;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1rem;
  background: rgba(0, 0, 0, 0.45);
}

.modal-dialog {
  width: 100%;
  max-width: 460px;
  border: 1px solid var(--color-border);
  border-radius: 1rem;
  background: var(--color-surface);
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.18);
}

.modal-header {
  padding: 1.125rem 1.25rem 0.875rem;
  border-bottom: 1px solid var(--color-border);
}

.modal-title {
  margin: 0;
  font-size: 1rem;
}

.modal-body {
  padding: 1.25rem;
}

.modal-body p {
  margin-top: 0;
  line-height: 1.5;
}

.replacement-label {
  display: flex;
  flex-direction: column;
  gap: 0.375rem;
  font-size: 0.875rem;
  font-weight: 600;
}

.replacement-select {
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  padding: 0.5rem 0.65rem;
  background: var(--color-surface);
  color: var(--color-text);
}

.delete-actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.5rem;
  margin-top: 1.25rem;
}

.btn-danger {
  border: 1px solid var(--color-danger);
  border-radius: 0.5rem;
  background: var(--color-danger);
  color: white;
  cursor: pointer;
  font-weight: 600;
}

.btn-danger:disabled {
  cursor: not-allowed;
  opacity: 0.65;
}

.modal-enter-active,
.modal-leave-active {
  transition: opacity 0.18s ease;
}

.modal-enter-from,
.modal-leave-to {
  opacity: 0;
}

/* ─── Transitions ───────────────────────────────────────── */

.expand-enter-active,
.expand-leave-active {
  transition: max-height 0.22s ease, opacity 0.18s ease;
  max-height: 600px;
  overflow: hidden;
}

.expand-enter-from,
.expand-leave-to {
  max-height: 0;
  opacity: 0;
}

.slide-down-enter-active,
.slide-down-leave-active {
  transition: max-height 0.2s ease, opacity 0.15s ease;
  max-height: 200px;
  overflow: hidden;
}

.slide-down-enter-from,
.slide-down-leave-to {
  max-height: 0;
  opacity: 0;
}

/* ─── Spinner ───────────────────────────────────────────── */

.spinner {
  animation: spin 0.8s linear infinite;
  flex-shrink: 0;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}
</style>
