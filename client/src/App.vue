<script setup>
import { computed, onMounted } from 'vue'
import TodayCard from './components/TodayCard.vue'
import MonthReport from './components/MonthReport.vue'
import RulesPanel from './components/RulesPanel.vue'
import AdminUsersPanel from './components/AdminUsersPanel.vue'
import ToastContainer from './components/ToastContainer.vue'
import LoginView from './components/LoginView.vue'
import ChangePasswordView from './components/ChangePasswordView.vue'
import { useAuth, signalUnauthorized } from './composables/useAuth.js'
import { setUnauthorizedHandler } from './api.js'

const { user, isChecking, checkAuth, logout } = useAuth()

onMounted(async () => {
  // Wire up the 401 handler before the auth check so any mid-session expiry
  // is handled cleanly (collapses back to the login view without a page reload).
  setUnauthorizedHandler(signalUnauthorized)
  await checkAuth()
})

const userInitial = computed(() => {
  const name = user.value?.name || user.value?.email || '?'
  return name.charAt(0).toUpperCase()
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
  <template v-else>
    <header class="wrap">
      <div class="app-header-row">
        <div>
          <h1>Timecard</h1>
          <p class="sub">出勤時間紀錄與彈性工時管理</p>
        </div>

        <div class="user-pill">
          <span class="user-avatar" aria-hidden="true">{{ userInitial }}</span>
          <span class="user-name" :title="user.email">{{ user.name || user.email }}</span>
          <span v-if="user.isAdmin" class="badge admin-flag">Admin</span>
          <button class="ghost small" @click="logout">登出</button>
        </div>
      </div>
    </header>

    <main class="wrap">
      <TodayCard />
      <RulesPanel />
      <MonthReport />
      <AdminUsersPanel v-if="user.isAdmin" />
      <ToastContainer />
    </main>
  </template>
</template>

<style>
.app-header-row {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  flex-wrap: wrap;
}

.user-pill {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 5px 5px 5px 6px;
  border: 1px solid var(--line);
  border-radius: 999px;
  background: var(--card);
  font-size: 13px;
  flex-shrink: 0;
  margin-top: 4px;
}

.user-avatar {
  width: 26px;
  height: 26px;
  border-radius: 50%;
  background: rgba(37, 99, 235, 0.1);
  color: var(--primary);
  font-weight: 700;
  font-size: 12px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.user-name {
  color: var(--muted);
  max-width: 200px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.user-pill button {
  padding: 4px 10px;
  font-size: 12px;
  border-radius: 999px;
}

.admin-flag {
  border-color: rgba(37, 99, 235, 0.35);
  color: var(--primary);
  background: rgba(37, 99, 235, 0.08);
}
</style>
