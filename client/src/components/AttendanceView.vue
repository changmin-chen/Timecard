<script setup>
import { onMounted, ref } from 'vue'
import { fmtDate } from '../utils.js'
import { useDay } from '../composables/useDay.js'
import AttendanceForm from './AttendanceForm.vue'
import AttendanceList from './AttendanceList.vue'

const {
  day, loading, currentDate, isToday,
  loadDay, goToday, goPrev, goNext,
  addAttendanceRequest, deleteAttendanceRequest,
} = useDay()

const datePickerInput = ref(null)

function openDatePicker() {
  datePickerInput.value?.showPicker?.()
}

onMounted(() => goToday())
</script>

<template>
  <section class="card">
    <div class="row">
      <div class="date-nav">
        <button class="date-nav-btn" @click="goPrev" :disabled="loading" title="前一天">&#9664;</button>
        <div class="date-nav-date-wrapper" @click="openDatePicker" :class="{ disabled: loading }">
          <span class="date-nav-label">{{ fmtDate(currentDate) }}</span>
          <input
            ref="datePickerInput"
            type="date"
            class="date-nav-hidden-input"
            :value="currentDate"
            @change="loadDay($event.target.value)"
            :disabled="loading"
          />
        </div>
        <button class="date-nav-btn" @click="goNext" :disabled="loading" title="後一天">&#9654;</button>
        <span v-if="isToday" class="date-nav-pill today">今天</span>
        <button v-else class="date-nav-pill back" @click="goToday" :disabled="loading">回到今天</button>
      </div>
    </div>

    <h3>出勤申請</h3>
    <AttendanceForm
      :date="currentDate"
      :loading="loading"
      @submit="addAttendanceRequest"
    />
    <AttendanceList
      :requests="day?.attendanceRequests ?? []"
      @delete="deleteAttendanceRequest"
    />
  </section>
</template>

<style scoped>
.date-nav-date-wrapper {
  position: relative;
  display: inline-flex;
  align-items: center;
  cursor: pointer;
  border-radius: 6px;
  padding: 0 2px;
  transition: background 0.15s;
}
.date-nav-date-wrapper:hover {
  background: rgba(37, 99, 235, 0.05);
}
.date-nav-date-wrapper.disabled {
  cursor: default;
  opacity: 0.5;
}
.date-nav-hidden-input {
  position: absolute;
  opacity: 0;
  width: 0;
  height: 0;
  pointer-events: none;
}
</style>
