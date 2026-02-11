<script setup>
import { onMounted } from 'vue'
import { mins } from '../utils.js'
import { useDay } from '../composables/useDay.js'
import PunchList from './PunchList.vue'
import AttendanceForm from './AttendanceForm.vue'
import NonWorkingForm from './NonWorkingForm.vue'
import AttendanceList from './AttendanceList.vue'

const { day, error, loading, refreshToday, punch, deletePunch, addAttendanceRequest, deleteAttendanceRequest, setNonWorking } = useDay()

onMounted(() => refreshToday())

function summary(d) {
  return `date=${d.date} start=${d.start ?? 'null'} end=${d.end ?? 'null'} punches=${d.punchCount} planned=${d.plannedMinutes} worked=${d.workedMinutes} extension=${d.extensionMinutes} effective=${d.effectiveMinutes} delta=${mins(d.deltaMinutes)} flexCandidate=${mins(d.flexCandidate)}`
}
</script>

<template>
  <section class="card">
    <div v-if="error" class="hint bad">{{ error }}</div>
    <div class="row">
      <div>
        <h2>今天</h2>
        <div v-if="day" class="mono small">{{ summary(day) }}</div>
        <div v-else class="mono small">載入中…</div>
      </div>
      <div class="actions">
        <button class="primary" @click="punch" :disabled="loading">打卡（Punch）</button>
        <button class="ghost" @click="refreshToday" :disabled="loading">重新整理</button>
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
          @delete="deleteAttendanceRequest"
        />
      </div>
    </div>
  </section>
</template>
