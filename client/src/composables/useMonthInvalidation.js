import { ref } from 'vue'

const monthStale = ref(0)

export function invalidateMonth() {
  monthStale.value++
}

export function useMonthInvalidation() {
  return { monthStale }
}
