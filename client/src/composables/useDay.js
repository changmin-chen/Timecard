import { ref } from 'vue'
import { timecardApi } from '../timecardApi.js'
import { useToast } from './useToast.js'

export function useDay() {
  const day = ref(null)
  const loading = ref(false)
  const toast = useToast()

  async function withError(fn) {
    loading.value = true
    try {
      await fn()
    } catch (err) {
      toast.error(err.message)
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

  return {
    day,
    loading,
    refreshToday,
    punch,
    deletePunch,
    addAttendanceRequest,
    deleteAttendanceRequest,
  }
}
