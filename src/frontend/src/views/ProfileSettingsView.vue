<script setup lang="ts">
import { ref } from 'vue'
import { useThemeStore, type ThemeMode } from '../app/stores/themeStore'
import { useProfileStore } from '../app/stores/profileStore'

const themeStore = useThemeStore()
const profileStore = useProfileStore()

const firstNameInput = ref(profileStore.firstName)
const lastNameInput = ref(profileStore.lastName)
const saved = ref(false)

function saveName() {
  profileStore.setName(firstNameInput.value.trim(), lastNameInput.value.trim())
  saved.value = true
  setTimeout(() => { saved.value = false }, 2000)
}

interface ThemeOption {
  value: ThemeMode
  label: string
  icon: string
}

const themeOptions: ThemeOption[] = [
  { value: 'light', label: 'Hell', icon: 'sun' },
  { value: 'dark', label: 'Dunkel', icon: 'moon' },
  { value: 'system', label: 'System', icon: 'monitor' },
]
</script>

<template>
  <div class="view">
    <div class="view-header">
      <h1>Profileinstellungen</h1>
    </div>

    <div class="card profile-section">
      <h3>Persönliche Informationen</h3>
      <p class="muted" style="margin-bottom: 1rem;">Dein Name wird in der Sidebar angezeigt.</p>

      <form class="name-form" @submit.prevent="saveName">
        <div class="name-row">
          <div class="form-field">
            <label for="first-name">Vorname</label>
            <input id="first-name" v-model="firstNameInput" type="text" placeholder="z. B. Max" maxlength="50" />
          </div>
          <div class="form-field">
            <label for="last-name">Nachname</label>
            <input id="last-name" v-model="lastNameInput" type="text" placeholder="z. B. Mustermann" maxlength="50" />
          </div>
        </div>
        <div class="name-actions">
          <button type="submit" class="btn-primary">Speichern</button>
          <span v-if="saved" class="save-feedback">✓ Gespeichert</span>
        </div>
      </form>
    </div>

    <div class="card profile-section">
      <h3>Darstellung</h3>
      <p class="muted" style="margin-bottom: 1rem;">Wähle, wie Subly für dich aussehen soll.</p>

      <div class="theme-options">
        <button
          v-for="option in themeOptions"
          :key="option.value"
          class="theme-option"
          :class="{ 'theme-option--active': themeStore.theme === option.value }"
          :aria-pressed="themeStore.theme === option.value"
          @click="themeStore.setTheme(option.value)"
        >
          <!-- Sun icon -->
          <svg v-if="option.icon === 'sun'" width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <circle cx="12" cy="12" r="5"/>
            <line x1="12" y1="1" x2="12" y2="3"/>
            <line x1="12" y1="21" x2="12" y2="23"/>
            <line x1="4.22" y1="4.22" x2="5.64" y2="5.64"/>
            <line x1="18.36" y1="18.36" x2="19.78" y2="19.78"/>
            <line x1="1" y1="12" x2="3" y2="12"/>
            <line x1="21" y1="12" x2="23" y2="12"/>
            <line x1="4.22" y1="19.78" x2="5.64" y2="18.36"/>
            <line x1="18.36" y1="5.64" x2="19.78" y2="4.22"/>
          </svg>
          <!-- Moon icon -->
          <svg v-else-if="option.icon === 'moon'" width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/>
          </svg>
          <!-- Monitor icon -->
          <svg v-else-if="option.icon === 'monitor'" width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <rect x="2" y="3" width="20" height="14" rx="2" ry="2"/>
            <line x1="8" y1="21" x2="16" y2="21"/>
            <line x1="12" y1="17" x2="12" y2="21"/>
          </svg>
          <span>{{ option.label }}</span>
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.profile-section h3 {
  margin: 0 0 0.25rem;
  font-size: 0.9375rem;
}

.name-form {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.name-row {
  display: flex;
  gap: 1rem;
  flex-wrap: wrap;
}

.form-field {
  display: flex;
  flex-direction: column;
  gap: 0.375rem;
  flex: 1;
  min-width: 160px;
}

.form-field label {
  font-size: 0.8125rem;
  font-weight: 500;
  color: var(--color-text-muted);
}

.form-field input {
  padding: 0.5rem 0.75rem;
  border: 1.5px solid var(--color-border);
  border-radius: 0.5rem;
  background: var(--color-surface);
  color: var(--color-text);
  font-size: 0.9375rem;
  outline: none;
  transition: border-color 0.15s;
}

.form-field input:focus {
  border-color: var(--color-primary);
}

.name-actions {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.btn-primary {
  padding: 0.5rem 1.25rem;
  background: var(--color-primary);
  color: #fff;
  border: none;
  border-radius: 0.5rem;
  font-size: 0.875rem;
  font-weight: 600;
  cursor: pointer;
  transition: opacity 0.15s;
}

.btn-primary:hover {
  opacity: 0.88;
}

.save-feedback {
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--color-success, #16a34a);
}

.theme-options {
  display: flex;
  gap: 0.75rem;
}

.theme-option {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
  padding: 1rem 1.5rem;
  border: 2px solid var(--color-border);
  border-radius: 0.75rem;
  background: var(--color-surface);
  color: var(--color-text-muted);
  cursor: pointer;
  font-size: 0.8125rem;
  font-weight: 500;
  transition: border-color 0.15s, color 0.15s, background 0.15s;
  min-width: 90px;
}

.theme-option:hover {
  border-color: var(--color-primary);
  color: var(--color-text);
  background: var(--color-primary-light);
}

.theme-option--active {
  border-color: var(--color-primary);
  color: var(--color-primary);
  background: var(--color-primary-light);
  font-weight: 600;
}
</style>
