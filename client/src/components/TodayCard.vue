<script setup>
import { ref, onMounted } from 'vue'
import { mins, fmtDate, fmtTime } from '../utils.js'
import { useDay } from '../composables/useDay.js'
import PunchList from './PunchList.vue'
import AttendanceForm from './AttendanceForm.vue'
import AttendanceList from './AttendanceList.vue'

const {
  day, loading, currentDate, isToday,
  refreshToday, goToday, goPrev, goNext,
  addAttendanceRequest, deleteAttendanceRequest
} = useDay()

const showPunches = ref(false)

onMounted(() => refreshToday())
</script>

<template>
  <section class="card">
    <div class="row">
      <div class="date-nav">
        <button class="date-nav-btn" @click="goPrev" :disabled="loading" title="前一天">&#9664;</button>
        <span class="date-nav-label">{{ fmtDate(currentDate) }}</span>
        <button class="date-nav-btn" @click="goNext" :disabled="loading" title="後一天">&#9654;</button>
        <span v-if="isToday" class="date-nav-pill today">今天</span>
        <button v-else class="date-nav-pill back" @click="goToday" :disabled="loading">回到今天</button>
      </div>
      <div class="actions">
        <button v-if="isToday" class="ghost" @click="refreshToday" :disabled="loading">重新整理</button>
      </div>
    </div>

    <div v-if="day" class="stat-grid">
      <div class="stat-item">
        <span class="stat-label">上班時間</span>
        <span class="stat-value">{{ fmtTime(day.start) }}</span>
      </div>
      <div class="stat-item">
        <span class="stat-label">下班時間</span>
        <span class="stat-value">{{ fmtTime(day.end) }}</span>
      </div>
      <div class="stat-item">
        <span class="stat-label">實際工時 / 標準工時</span>
        <span class="stat-value">{{ day.eligibleMinutes }} / {{ day.plannedMinutes }} 分</span>
      </div>
      <div class="stat-item">
        <span class="stat-label">工時差額</span>
        <span class="stat-value" :class="day.eligibleDeltaMinutes < 0 ? 'bad' : day.eligibleDeltaMinutes > 0 ? 'good' : ''">
          {{ mins(day.eligibleDeltaMinutes) }} 分
        </span>
      </div>
      <div class="stat-item">
        <span class="stat-label">今日彈性</span>
        <span class="stat-value">{{ mins(day.flexDeltaMinutes) }} 分</span>
      </div>
      <button
        class="stat-item stat-punch-toggle"
        :class="{ active: showPunches }"
        @click="showPunches = !showPunches"
        title="展開打卡紀錄"
      >
        <span class="stat-label">打卡次數 {{ showPunches ? '▲' : '▼' }}</span>
        <span class="stat-value">{{ day.punches?.length ?? 0 }} 次</span>
      </button>
    </div>

    <div v-if="day && day.plannedMinutes === 0" class="hint">
      今日為休假日，工時不計入統計<span v-if="day.note">（{{ day.note }}）</span>
    </div>
    <div v-else-if="day && day.note" class="hint">{{ day.note }}</div>

    <Transition name="punch-reveal">
      <div v-if="day && showPunches" class="punch-expand">
        <PunchList :punches="day?.punches ?? []" />
      </div>
    </Transition>

    <div class="mt">
      <AttendanceForm
        :date="currentDate"
        :loading="loading"
        @submit="addAttendanceRequest"
      />
      <AttendanceList
        :requests="day?.attendanceRequests ?? []"
        @delete="deleteAttendanceRequest"
      />
    </div>
  </section>
</template>

<style scoped>
.stat-punch-toggle {
  cursor: pointer;
  background: transparent;
  border: 1px solid var(--line);
  border-radius: 10px;
  padding: 4px 8px;
  display: flex;
  flex-direction: column;
  gap: 4px;
  text-align: left;
  transition: all 0.15s;
  font-family: inherit;
}
.stat-punch-toggle:hover,
.stat-punch-toggle.active {
  background: rgba(37, 99, 235, 0.05);
  border-color: rgba(37, 99, 235, 0.25);
  filter: none;
}

.punch-expand {
  border-top: 1px solid var(--line);
  margin-top: 12px;
  padding-top: 4px;
}

.punch-reveal-enter-active,
.punch-reveal-leave-active {
  transition: opacity 0.2s ease, transform 0.2s ease;
  transform-origin: top;
}
.punch-reveal-enter-from,
.punch-reveal-leave-to {
  opacity: 0;
  transform: scaleY(0.95) translateY(-4px);
}
</style>
