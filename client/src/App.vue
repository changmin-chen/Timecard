<script setup>
import TodayCard from './components/TodayCard.vue'
import MonthReport from './components/MonthReport.vue'
import ToastContainer from './components/ToastContainer.vue'
</script>

<template>
  <header class="wrap">
    <h1>Timecard MVP（Punch-only）</h1>
    <p class="sub">打卡只有「時間戳」。日工時用最早到最晚。中間亂走一律當不存在，因為你也不想記。</p>
  </header>

  <main class="wrap">
    <TodayCard />
    <MonthReport />
    <ToastContainer />

    <footer class="wrap small">
      <div class="hint">資料存在 PostgreSQL。刪掉資料庫或資料表會清空既有資料。</div>
    </footer>
  </main>
</template>
