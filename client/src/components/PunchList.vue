<script setup>
import { fmtTime } from '../utils.js'

defineProps({
  punches: { type: Array, required: true },
})
</script>

<template>
  <div class="punch-list-header">
    <h3>打卡紀錄</h3>
    <span class="sync-badge">外部同步</span>
  </div>
  <div v-if="!punches.length" class="hint">尚無打卡紀錄（等待外部系統同步）。</div>
  <div v-for="p in punches" :key="p.id" class="item">
    <div class="meta">
      <div class="title">{{ fmtTime(p.at) }}</div>
      <div v-if="p.note" class="note-text">{{ p.note }}</div>
    </div>
  </div>
</template>

<style scoped>
.punch-list-header {
  display: flex;
  align-items: center;
  gap: 8px;
}

.punch-list-header h3 {
  margin: 14px 0 8px;
}

.sync-badge {
  font-size: 11px;
  font-weight: 600;
  color: var(--muted);
  border: 1px solid var(--line);
  border-radius: 999px;
  padding: 1px 8px;
  background: #f9fafb;
  letter-spacing: 0.02em;
  margin-top: 6px;
}
</style>
