import { ref, computed } from 'vue'
import { timecardApi } from '../timecardApi.js'
import { useToast } from './useToast.js'
import { invalidateMonth } from './useMonthInvalidation.js'

function todayStr() {
  const d = new Date()
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}

function addDays(dateStr, delta) {
  const [y, m, d] = dateStr.split('-').map(Number)
  const dt = new Date(y, m - 1, d)
  dt.setDate(dt.getDate() + delta)
  return `${dt.getFullYear()}-${String(dt.getMonth() + 1).padStart(2, '0')}-${String(dt.getDate()).padStart(2, '0')}`
}

export function useDay() {
  const day = ref(null)
  const loading = ref(false)
  const toast = useToast()
  const currentDate = ref(todayStr())

  const isToday = computed(() => currentDate.value === todayStr())

  async function withError(fn) {
    loading.value = true
    try {
      await fn()
    } catch (err) {
      toast.errorFrom(err)
    } finally {
      loading.value = false
    }
  }

  async function loadDay(date) {
    currentDate.value = date
    await withError(async () => {
      day.value = await timecardApi.getDay(date)
    })
  }

  async function refreshToday() {
    currentDate.value = todayStr()
    await withError(async () => {
      day.value = await timecardApi.getToday()
    })
  }

  async function goToday() {
    await refreshToday()
  }

  async function goPrev() {
    await loadDay(addDays(currentDate.value, -1))
  }

  async function goNext() {
    await loadDay(addDays(currentDate.value, 1))
  }

  async function punch() {
    if (!isToday.value) return
    await withError(async () => {
      day.value = await timecardApi.punch()
      invalidateMonth()
    })
  }

  async function deletePunch(id) {
    await withError(async () => {
      day.value = await timecardApi.deletePunch(id)
      invalidateMonth()
    })
  }

  async function addAttendanceRequest(payload) {
    await withError(async () => {
      day.value = await timecardApi.addAttendanceRequest(payload)
      invalidateMonth()
      toast.success('出勤申請已新增')
    })
  }

  async function deleteAttendanceRequest(id) {
    await withError(async () => {
      day.value = await timecardApi.deleteAttendanceRequest(id)
      invalidateMonth()
    })
  }

  return {
    day,
    loading,
    currentDate,
    isToday,
    refreshToday,
    loadDay,
    goToday,
    goPrev,
    goNext,
    punch,
    deletePunch,
    addAttendanceRequest,
    deleteAttendanceRequest,
  }
}
