<script setup>
import { onMounted } from 'vue'
import { mins, fmtDate, fmtTime } from '../utils.js'
import { useDay } from '../composables/useDay.js'
import PunchList from './PunchList.vue'
import AttendanceForm from './AttendanceForm.vue'
import NonWorkingForm from './NonWorkingForm.vue'
import AttendanceList from './AttendanceList.vue'

const { day, error, loading, refreshToday, punch, deletePunch, addAttendanceRequest, deleteAttendanceRequest, setNonWorking } = useDay()

onMounted(() => refreshToday())
</script>

<template>
  <section class="card">
    <div v-if="error" class="hint bad">{{ error }}</div>
    <div class="row">
      <div>
        <h2>今天</h2>
        <div v-if="day" style="font-size: 15px; color: var(--muted); margin-top: 4px;">
          {{ fmtDate(day.date) }}
        </div>
        <div v-else class="hint">載入中…</div>
      </div>
      <div class="actions">
        <button class="primary" @click="punch" :disabled="loading">打卡（Punch）</button>
        <button class="ghost" @click="refreshToday" :disabled="loading">重新整理</button>
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
        <span class="stat-value">{{ mins(day.flexCandidate) }}</span>
      </div>
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
          :date="day?.date ?? ''"
          @submit="addAttendanceRequest"
        />

        <NonWorkingForm
          :date="day?.date ?? ''"
          :is-non-working-day="day?.isNonWorkingDay ?? false"
          :note="day?.note ?? ''"
          @submit="setNonWorking"
        />

        <AttendanceList
          :requests="day?.attendanceRequests ?? []"
          :date="fmtDate(day?.date ?? '')"
          @delete="deleteAttendanceRequest"
        />
      </div>
    </div>
  </section>
</template>
