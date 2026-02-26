<script setup>
import { onMounted } from 'vue'
import AppLayout from './components/AppLayout.vue'
import LoginView from './components/LoginView.vue'
import ChangePasswordView from './components/ChangePasswordView.vue'
import { useAuth, signalUnauthorized } from './composables/useAuth.js'
import { setUnauthorizedHandler } from './api.js'

const { user, isChecking, checkAuth, logout } = useAuth()

onMounted(async () => {
  setUnauthorizedHandler(signalUnauthorized)
  await checkAuth()
})
</script>

<template>
  <!-- Blank while the /api/auth/me probe is in flight -->
  <template v-if="isChecking" />

  <!-- Login page -->
  <LoginView v-else-if="!user" />

  <!-- Force password change on first login -->
  <ChangePasswordView v-else-if="user.mustChangePassword" />

  <!-- Main app -->
  <AppLayout v-else :user="user" @logout="logout" />
</template>
