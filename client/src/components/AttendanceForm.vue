<script setup>
import { reactive, watch } from 'vue'

const props = defineProps({
  date: { type: String, default: '' },
})
const emit = defineEmits(['submit'])

const form = reactive({
  date: '',
  category: 'Leave',
  start: '',
  end: '',
  note: '',
})

watch(() => props.date, (val) => {
  form.date = val
}, { immediate: true })

function onSubmit() {
  emit('submit', {
    date: form.date,
    category: form.category.trim(),
    start: form.start,
    end: form.end,
    note: form.note.trim(),
  })
  form.start = ''
  form.end = ''
  form.note = ''
}
</script>

<template>
  <h3>出勤申請（請假 / 出差 / 假日 / 颱風假…）</h3>
  <form class="form" @submit.prevent="onSubmit">
    <label>
      日期
      <input type="date" v-model="form.date" required />
    </label>
    <label>
      類別
      <input type="text" v-model="form.category" placeholder="Leave / Trip / Holiday / Typhoon" required />
    </label>
    <label>
      起始時間
      <input type="time" v-model="form.start" required />
    </label>
    <label>
      結束時間
      <input type="time" v-model="form.end" required />
    </label>
    <label>
      註記
      <input type="text" v-model="form.note" placeholder="可留空" />
    </label>
    <div class="actions">
      <button type="submit" class="primary">新增出勤申請</button>
    </div>
  </form>
</template>
