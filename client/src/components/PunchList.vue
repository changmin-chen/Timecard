<script setup>
import { fmtTime } from '../utils.js'

const props = defineProps({
  punches: { type: Array, required: true },
})
const emit = defineEmits(['delete'])
</script>

<template>
  <h3>Punches</h3>
  <div v-if="!punches.length" class="hint">尚無 punch。按「打卡」新增一筆。</div>
  <div v-for="p in punches" :key="p.id" class="item">
    <div class="meta">
      <div class="title">#{{ p.id }} {{ fmtTime(p.at) }}</div>
      <div class="sub">{{ p.at }}{{ p.note ? ` | ${p.note}` : '' }}</div>
    </div>
    <div class="right">
      <button class="danger" @click="emit('delete', p.id)">刪除</button>
    </div>
  </div>
  <div class="hint">日 span = 最早 punch → 最晚 punch。若只有 1 筆 punch，工時 = 0（因為你還沒下班）。</div>
</template>
