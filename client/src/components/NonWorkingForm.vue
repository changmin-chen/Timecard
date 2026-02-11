<script setup>
import { reactive, watch } from 'vue'

const props = defineProps({
  date: { type: String, default: '' },
  isNonWorkingDay: { type: Boolean, default: false },
  note: { type: String, default: '' },
})
const emit = defineEmits(['submit'])

const form = reactive({
  date: '',
  isNonWorkingDay: false,
  note: '',
})

watch(() => props.date, (val) => { form.date = val }, { immediate: true })
watch(() => props.isNonWorkingDay, (val) => { form.isNonWorkingDay = val }, { immediate: true })
watch(() => props.note, (val) => { form.note = val ?? '' }, { immediate: true })

function onSubmit() {
  emit('submit', form.date, {
    isNonWorkingDay: form.isNonWorkingDay,
    note: form.note.trim(),
  })
}
</script>

<template>
  <h3 class="mt">免上班日（手動）</h3>
  <form class="form" @submit.prevent="onSubmit">
    <label>
      日期
      <input type="date" v-model="form.date" required />
    </label>
    <label class="inline">
      <input type="checkbox" v-model="form.isNonWorkingDay" />
      這天不用上班
    </label>
    <label>
      註記
      <input type="text" v-model="form.note" placeholder="例如：端午 / 颱風假" />
    </label>
    <div class="actions">
      <button type="submit" class="primary">儲存</button>
    </div>
  </form>
</template>
