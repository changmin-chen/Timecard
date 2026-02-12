<script setup>
import { fmtTime } from '../utils.js'

const props = defineProps({
  punches: { type: Array, required: true },
})
const emit = defineEmits(['delete'])
</script>

<template>
  <h3>打卡紀錄</h3>
  <div v-if="!punches.length" class="hint">尚無打卡紀錄。按「打卡」新增一筆。</div>
  <div v-for="p in punches" :key="p.id" class="item">
    <div class="meta">
      <div class="title">{{ fmtTime(p.at) }}</div>
      <div v-if="p.note" class="note-text">{{ p.note }}</div>
    </div>
    <div class="right">
      <button class="danger" @click="emit('delete', p.id)">刪除</button>
    </div>
  </div>
  <div class="hint">日 span = 最早打卡 → 最晚打卡。若只有 1 筆，工時 = 0（尚未下班）。</div>
</template>
