import { ref } from 'vue'
import { timecardApi } from '../timecardApi.js'

export function useMonth() {
  const month = ref(null)
  const error = ref('')
  const loading = ref(false)

  async function loadMonth(year, m, includeEmpty) {
    error.value = ''
    loading.value = true
    try {
      month.value = await timecardApi.getMonth(year, m, includeEmpty)
    } catch (err) {
      error.value = err.message
    } finally {
      loading.value = false
    }
  }

  return { month, error, loading, loadMonth }
}
