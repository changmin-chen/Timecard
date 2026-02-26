<script setup>
import { ref, computed } from 'vue'
import TodayCard from './TodayCard.vue'
import MonthReport from './MonthReport.vue'
import RulesPanel from './RulesPanel.vue'
import AdminUsersPanel from './AdminUsersPanel.vue'
import AttendanceView from './AttendanceView.vue'
import ToastContainer from './ToastContainer.vue'

const props = defineProps({
  user: { type: Object, required: true },
})
const emit = defineEmits(['logout'])

const activePage = ref('daily')

const pages = computed(() => {
  const items = [
    { key: 'daily',      label: '日報表',  icon: '◉', section: '主選單' },
    { key: 'monthly',    label: '月報表',  icon: '◫', section: '主選單' },
    { key: 'attendance', label: '出勤管理', icon: '◪', section: '管理' },
  ]
  if (props.user.isAdmin) {
    items.push({ key: 'admin', label: '人員管理', icon: '◧', section: '管理' })
  }
  return items
})

const sections = computed(() => {
  const map = {}
  for (const p of pages.value) {
    if (!map[p.section]) map[p.section] = []
    map[p.section].push(p)
  }
  return Object.entries(map)
})

const pageTitle = computed(() => {
  return pages.value.find(p => p.key === activePage.value)?.label ?? ''
})

const userInitial = computed(() => {
  const name = props.user?.name || props.user?.email || '?'
  return name.charAt(0).toUpperCase()
})
</script>

<template>
  <div class="app-shell">
    <!-- Sidebar -->
    <aside class="app-sidebar">
      <div class="sidebar-logo">
        <div class="sidebar-logo-title">勤務系統</div>
        <div class="sidebar-logo-sub">Timecard</div>
      </div>

      <nav class="sidebar-nav">
        <template v-for="[sectionName, items] in sections" :key="sectionName">
          <div class="sidebar-nav-section-label">{{ sectionName }}</div>
          <button
            v-for="page in items"
            :key="page.key"
            class="sidebar-nav-item"
            :class="{ active: activePage === page.key }"
            @click="activePage = page.key"
          >
            <span class="sidebar-nav-icon">{{ page.icon }}</span>
            {{ page.label }}
          </button>
        </template>
      </nav>

      <div class="sidebar-user">
        <div class="sidebar-user-avatar">{{ userInitial }}</div>
        <div class="sidebar-user-info">
          <div class="sidebar-user-name" :title="user.email">{{ user.name || user.email }}</div>
          <div class="sidebar-user-role">{{ user.isAdmin ? 'Admin' : '一般使用者' }}</div>
        </div>
        <button class="sidebar-logout-btn" @click="emit('logout')" title="登出">⏻</button>
      </div>
    </aside>

    <!-- Main content -->
    <div class="app-main">
      <div class="app-topbar">
        <span class="app-topbar-title">{{ pageTitle }}</span>
      </div>

      <div class="page-content">
        <template v-if="activePage === 'daily'">
          <TodayCard />
          <RulesPanel />
        </template>

        <MonthReport v-show="activePage === 'monthly'" />

        <AttendanceView v-if="activePage === 'attendance'" />

        <AdminUsersPanel v-if="activePage === 'admin' && user.isAdmin" />
      </div>
    </div>

    <ToastContainer />
  </div>
</template>

<style scoped>
.app-shell {
  display: flex;
  min-height: 100vh;
}

/* ── Sidebar ── */
.app-sidebar {
  width: 220px;
  position: fixed;
  top: 0;
  left: 0;
  height: 100vh;
  background: var(--card);
  border-right: 1px solid var(--line);
  display: flex;
  flex-direction: column;
  z-index: 100;
}

.sidebar-logo {
  padding: 20px 20px 16px;
  border-bottom: 1px solid var(--line);
}
.sidebar-logo-title {
  font-size: 17px;
  font-weight: 700;
  color: var(--text);
  letter-spacing: -0.01em;
}
.sidebar-logo-sub {
  font-size: 11px;
  color: var(--muted);
  margin-top: 2px;
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
  letter-spacing: 0.05em;
}

.sidebar-nav {
  flex: 1;
  padding: 12px 10px;
  display: flex;
  flex-direction: column;
  gap: 2px;
  overflow-y: auto;
}
.sidebar-nav-section-label {
  font-size: 10px;
  font-weight: 700;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: var(--muted);
  padding: 10px 10px 5px;
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
}
.sidebar-nav-item {
  display: flex;
  align-items: center;
  gap: 9px;
  padding: 8px 12px;
  border-radius: 8px;
  cursor: pointer;
  font-size: 13.5px;
  color: var(--muted);
  background: transparent;
  border: 1px solid transparent;
  text-align: left;
  transition: all 0.15s ease;
  font-family: inherit;
  width: 100%;
}
.sidebar-nav-item:hover {
  background: var(--bg);
  color: var(--text);
  filter: none;
}
.sidebar-nav-item.active {
  background: rgba(37, 99, 235, 0.07);
  border-color: rgba(37, 99, 235, 0.18);
  color: var(--primary);
  font-weight: 600;
}
.sidebar-nav-icon {
  font-size: 14px;
  width: 18px;
  text-align: center;
  flex-shrink: 0;
}

.sidebar-user {
  padding: 14px 16px;
  border-top: 1px solid var(--line);
  display: flex;
  align-items: center;
  gap: 10px;
}
.sidebar-user-avatar {
  width: 30px;
  height: 30px;
  border-radius: 50%;
  background: rgba(37, 99, 235, 0.1);
  color: var(--primary);
  font-weight: 700;
  font-size: 13px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}
.sidebar-user-info {
  flex: 1;
  overflow: hidden;
}
.sidebar-user-name {
  font-size: 13px;
  font-weight: 500;
  color: var(--text);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.sidebar-user-role {
  font-size: 11px;
  color: var(--muted);
  margin-top: 1px;
}
.sidebar-logout-btn {
  padding: 5px 7px;
  border-radius: 6px;
  border: 1px solid var(--line);
  background: transparent;
  color: var(--muted);
  font-size: 13px;
  cursor: pointer;
  flex-shrink: 0;
  transition: all 0.15s ease;
  line-height: 1;
}
.sidebar-logout-btn:hover {
  color: var(--danger);
  border-color: rgba(220, 38, 38, 0.35);
  background: rgba(220, 38, 38, 0.06);
  filter: none;
}

/* ── Main area ── */
.app-main {
  margin-left: 220px;
  flex: 1;
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

.app-topbar {
  position: sticky;
  top: 0;
  background: rgba(245, 246, 248, 0.92);
  backdrop-filter: blur(10px);
  border-bottom: 1px solid var(--line);
  padding: 0 28px;
  height: 52px;
  display: flex;
  align-items: center;
  z-index: 50;
}
.app-topbar-title {
  font-size: 15px;
  font-weight: 700;
  color: var(--text);
  letter-spacing: -0.01em;
}

.page-content {
  max-width: 1100px;
  margin: 0 auto;
  padding: 20px 28px 40px;
  width: 100%;
}
</style>
