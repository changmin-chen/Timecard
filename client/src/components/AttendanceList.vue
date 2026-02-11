<script setup>
const props = defineProps({
  requests: { type: Array, required: true },
})
const emit = defineEmits(['delete'])
</script>

<template>
  <h3 class="mt">出勤申請列表哈哈</h3>
  <div v-if="!requests.length" class="hint">尚無出勤申請。</div>
  <div v-for="a in requests" :key="a.id" class="item">
    <div class="meta">
      <div class="title">#{{ a.id }} [{{ a.category }}] {{ a.start }} ~ {{ a.end }}</div>
      <div class="sub">{{ a.note || '' }}</div>
    </div>
    <div class="right">
      <button class="danger" @click="emit('delete', a.id)">刪除</button>
    </div>
  </div>
</template>
