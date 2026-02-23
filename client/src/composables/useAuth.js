import { ref } from 'vue'
import { timecardApi } from '../timecardApi.js'

// Module-level singleton — auth state is shared across all components.
const user = ref(null)        // null = unauthenticated | { id, email, name, isAdmin, mustChangePassword }
const isChecking = ref(true)  // true only during the initial /api/auth/me probe

export function useAuth() {
  async function checkAuth() {
    isChecking.value = true
    try {
      const res = await fetch('/api/auth/me', { credentials: 'include' })
      user.value = res.ok ? await res.json() : null
    } catch {
      user.value = null
    } finally {
      isChecking.value = false
    }
  }

  async function login(email, password) {
    const res = await timecardApi.login(email, password)
    user.value = res
    return res
  }

  async function logout() {
    try {
      await fetch('/api/auth/logout', { method: 'POST', credentials: 'include' })
    } finally {
      user.value = null
    }
  }

  return { user, isChecking, checkAuth, login, logout }
}

// Called by api.js when any request returns 401 (session expired mid-session).
export function signalUnauthorized() {
  user.value = null
}
