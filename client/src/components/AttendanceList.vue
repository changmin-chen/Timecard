<script setup>
import { fmtTimeStr, categoryLabel, categoryClass } from '../utils.js'

const props = defineProps({
  requests: { type: Array, required: true },
  date: { type: String, default: '' },
})
const emit = defineEmits(['delete'])
</script>

<template>
  <h3 class="mt">出勤申請列表</h3>
  <div v-if="date" class="hint">{{ date }}</div>
  <div v-if="!requests.length" class="hint">尚無出勤申請。</div>
  <div v-for="a in requests" :key="a.id" class="item">
    <div class="meta">
      <div class="title">
        <span :class="['badge-category', categoryClass(a.category)]">{{ categoryLabel(a.category) }}</span>
        {{ fmtTimeStr(a.start) }} ~ {{ fmtTimeStr(a.end) }}
      </div>
      <div v-if="a.note" class="note-text">{{ a.note }}</div>
    </div>
    <div class="right">
      <button class="danger" @click="emit('delete', a.id)">刪除</button>
    </div>
  </div>
</template>
