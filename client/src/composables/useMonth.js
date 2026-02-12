import { ref } from 'vue'
import { timecardApi } from '../timecardApi.js'
import { useToast } from './useToast.js'

export function useMonth() {
  const month = ref(null)
  const loading = ref(false)
  const toast = useToast()

  async function loadMonth(year, m, includeEmpty) {
    loading.value = true
    try {
      month.value = await timecardApi.getMonth(year, m, includeEmpty)
    } catch (err) {
      toast.error(err.message)
    } finally {
      loading.value = false
    }
  }

  return { month, loading, loadMonth }
}
