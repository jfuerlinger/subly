import { defineStore } from 'pinia'
import { ref, computed, watch } from 'vue'

const STORAGE_KEY_FIRST = 'subly-profile-first-name'
const STORAGE_KEY_LAST = 'subly-profile-last-name'

export const useProfileStore = defineStore('profile', () => {
  const firstName = ref(localStorage.getItem(STORAGE_KEY_FIRST) ?? '')
  const lastName = ref(localStorage.getItem(STORAGE_KEY_LAST) ?? '')

  const displayName = computed(() => {
    const first = firstName.value.trim()
    const last = lastName.value.trim()
    if (!first && !last) return 'Mein Profil'
    if (!last) return first
    if (!first) return last
    return `${first} ${last.charAt(0)}.`
  })

  const initials = computed(() => {
    const first = firstName.value.trim()
    const last = lastName.value.trim()
    const f = first.charAt(0).toUpperCase()
    const l = last.charAt(0).toUpperCase()
    if (!f && !l) return '?'
    return `${f}${l}`
  })

  watch(firstName, (val) => localStorage.setItem(STORAGE_KEY_FIRST, val))
  watch(lastName, (val) => localStorage.setItem(STORAGE_KEY_LAST, val))

  function setName(first: string, last: string) {
    firstName.value = first
    lastName.value = last
  }

  return { firstName, lastName, displayName, initials, setName }
})
