<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../app/stores/authStore'

const authStore = useAuthStore()
const router = useRouter()

const mode = ref<'login' | 'register'>('login')

const firstName = ref('')
const lastName = ref('')
const email = ref('')
const password = ref('')

async function submit() {
  if (mode.value === 'register') {
    await authStore.registerUser({
      firstName: firstName.value,
      lastName: lastName.value,
      email: email.value,
      password: password.value,
    })
  } else {
    await authStore.loginUser({
      email: email.value,
      password: password.value,
    })
  }

  if (authStore.isAuthenticated) {
    await router.push({ name: 'dashboard' })
  }
}
</script>

<template>
  <section class="auth-page">
    <div class="auth-card">
      <h1>Subly</h1>
      <p>{{ mode === 'register' ? 'Benutzer registrieren' : 'Mit E-Mail anmelden' }}</p>

      <div class="auth-switch">
        <button type="button" :class="{ active: mode === 'login' }" @click="mode = 'login'">Anmelden</button>
        <button type="button" :class="{ active: mode === 'register' }" @click="mode = 'register'">Registrieren</button>
      </div>

      <form class="auth-form" @submit.prevent="submit">
        <template v-if="mode === 'register'">
          <label>
            Vorname
            <input v-model.trim="firstName" type="text" required />
          </label>
          <label>
            Nachname
            <input v-model.trim="lastName" type="text" required />
          </label>
        </template>

        <label>
          E-Mail
          <input v-model.trim="email" type="email" required />
        </label>
        <label>
          Passwort
          <input v-model="password" type="password" minlength="8" required />
        </label>

        <p v-if="authStore.authError" class="error">{{ authStore.authError }}</p>

        <button type="submit" class="submit-btn" :disabled="authStore.loading">
          {{ mode === 'register' ? 'Registrieren' : 'Anmelden' }}
        </button>
      </form>
    </div>
  </section>
</template>

<style scoped>
.auth-page {
  min-height: 100vh;
  display: grid;
  place-items: center;
  background: linear-gradient(135deg, #111827, #1f2937);
}

.auth-card {
  width: min(420px, 94vw);
  background: #ffffff;
  border-radius: 16px;
  padding: 2rem;
  box-shadow: 0 20px 45px rgba(0, 0, 0, 0.25);
}

.auth-card h1 {
  margin: 0;
}

.auth-card p {
  margin: 0.5rem 0 1.5rem;
  color: #6b7280;
}

.auth-switch {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.5rem;
  margin-bottom: 1rem;
}

.auth-switch button {
  border: 1px solid #d1d5db;
  border-radius: 8px;
  padding: 0.5rem 0.75rem;
  background: #f9fafb;
  cursor: pointer;
}

.auth-switch button.active {
  background: #111827;
  color: #ffffff;
  border-color: #111827;
}

.auth-form {
  display: grid;
  gap: 0.9rem;
}

.auth-form label {
  display: grid;
  gap: 0.35rem;
  color: #111827;
  font-size: 0.9rem;
}

.auth-form input {
  border: 1px solid #d1d5db;
  border-radius: 8px;
  padding: 0.7rem 0.75rem;
}

.submit-btn {
  margin-top: 0.4rem;
  border: none;
  border-radius: 10px;
  background: #111827;
  color: #fff;
  padding: 0.75rem;
  cursor: pointer;
}

.submit-btn:disabled {
  opacity: 0.6;
  cursor: wait;
}

.error {
  margin: 0;
  color: #b91c1c;
  font-size: 0.9rem;
}
</style>
