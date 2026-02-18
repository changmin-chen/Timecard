<script setup>
import { onMounted } from 'vue'
import { mins, fmtDate, fmtTime } from '../utils.js'
import { useDay } from '../composables/useDay.js'
import PunchList from './PunchList.vue'
import AttendanceForm from './AttendanceForm.vue'
import AttendanceList from './AttendanceList.vue'

const {
  day, loading, currentDate, isToday,
  refreshToday, goToday, goPrev, goNext,
  punch, deletePunch, addAttendanceRequest, deleteAttendanceRequest
} = useDay()

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
        <button v-if="isToday" class="primary" @click="punch" :disabled="loading">打卡（Punch）</button>
        <button class="ghost" @click="isToday ? refreshToday() : goToday()" :disabled="loading">
          {{ isToday ? '重新整理' : '回到今天' }}
        </button>
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
        <span class="stat-label">已工作 / 規定</span>
        <span class="stat-value">{{ day.effectiveMinutes }} / {{ day.plannedMinutes }} 分鐘</span>
      </div>
      <div class="stat-item">
        <span class="stat-label">差額</span>
        <span class="stat-value" :class="day.deltaMinutes < 0 ? 'bad' : day.deltaMinutes > 0 ? 'good' : ''">
          {{ mins(day.deltaMinutes) }} 分鐘
        </span>
      </div>
      <div class="stat-item">
        <span class="stat-label">彈性候選</span>
        <span class="stat-value">{{ mins(day.flexDeltaMinutes) }}</span>
      </div>
    </div>
    <div v-if="day" class="hint">
      日曆來源：{{ day.calendarSource }} / {{ day.calendarKind }}
      <span v-if="day.note">（{{ day.note }}）</span>
    </div>

    <div class="grid2">
      <div>
        <PunchList
          :punches="day?.punches ?? []"
          @delete="deletePunch"
        />
      </div>

      <div>
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
    </div>
  </section>
</template>
