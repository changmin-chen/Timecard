import { ref } from 'vue'
import { timecardApi } from '../timecardApi.js'

export function useDay() {
  const day = ref(null)
  const error = ref('')
  const loading = ref(false)

  async function withError(fn) {
    error.value = ''
    loading.value = true
    try {
      await fn()
    } catch (err) {
      error.value = err.message
    } finally {
      loading.value = false
    }
  }

  async function refreshToday() {
    await withError(async () => {
      day.value = await timecardApi.getToday()
    })
  }

  async function punch() {
    await withError(async () => {
      day.value = await timecardApi.punch()
    })
  }

  async function deletePunch(id) {
    await withError(async () => {
      day.value = await timecardApi.deletePunch(id)
    })
  }

  async function addAttendanceRequest(payload) {
    await withError(async () => {
      day.value = await timecardApi.addAttendanceRequest(payload)
    })
  }

  async function deleteAttendanceRequest(id) {
    await withError(async () => {
      day.value = await timecardApi.deleteAttendanceRequest(id)
    })
  }

  async function setNonWorking(date, payload) {
    await withError(async () => {
      day.value = await timecardApi.setNonWorking(date, payload)
    })
  }

  return {
    day,
    error,
    loading,
    refreshToday,
    punch,
    deletePunch,
    addAttendanceRequest,
    deleteAttendanceRequest,
    setNonWorking,
  }
}
