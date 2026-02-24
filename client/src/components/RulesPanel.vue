<script setup>
import { ref } from 'vue'
const open = ref(false)
</script>

<template>
  <section class="card rules-panel">
    <button class="rules-toggle" @click="open = !open">
      <span>計算規則說明</span>
      <span class="rules-arrow" :class="{ open }">▾</span>
    </button>
    <Transition name="rules-expand">
      <div v-if="open" class="rules-body">
        <div class="rules-section">
          <h4>工時計算基礎</h4>
          <ul>
            <li>每日標準工時：<strong>9 小時（540 分鐘）</strong></li>
            <li>有效計時區間：<strong>07:30 ～ 19:00</strong>（超出此範圍的時間不予計入）</li>
            <li>計算方式：以當日<strong>最早打卡至最晚打卡</strong>的時間區間為準</li>
            <li>當日僅有 1 筆打卡紀錄時，視為尚未下班，工時計為 0</li>
          </ul>
        </div>
        <div class="rules-section">
          <h4>出勤申請</h4>
          <ul>
            <li>請假、特休、出差等申請，計入當日有效工時</li>
            <li>出勤申請可與打卡紀錄並行計算，取聯集後加總</li>
          </ul>
        </div>
        <div class="rules-section">
          <h4>彈性時數制度</h4>
          <ul>
            <li>每日彈性上限：實際工時與標準工時的差額，最多累積或扣抵 <strong>±55 分鐘</strong></li>
            <li>超出 ±55 分鐘的部分不計入彈性銀行</li>
            <li>休假日、補假日等免上班日，不累積彈性時數</li>
            <li>彈性時數<strong>按月累積</strong>，每月底自動重置歸零</li>
          </ul>
        </div>
        <div class="rules-section">
          <h4>不足時數</h4>
          <ul>
            <li>提領全部彈性時數後仍有缺口，記為「不足」時數</li>
            <li>建議以出勤申請（如請假、特休）補足不足時數</li>
          </ul>
        </div>
      </div>
    </Transition>
  </section>
</template>

<style scoped>
.rules-panel {
  padding: 0;
  overflow: hidden;
}

.rules-toggle {
  width: 100%;
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 14px 16px;
  background: transparent;
  border: none;
  font-size: 14px;
  font-weight: 600;
  color: var(--muted);
  cursor: pointer;
  border-radius: 14px;
  transition: background 0.15s;
  font-family: inherit;
}
.rules-toggle:hover {
  background: rgba(0, 0, 0, 0.02);
  filter: none;
}

.rules-arrow {
  font-size: 16px;
  transition: transform 0.2s ease;
  display: inline-block;
}
.rules-arrow.open {
  transform: rotate(180deg);
}

.rules-body {
  padding: 0 16px 16px;
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 20px;
}

.rules-section h4 {
  font-size: 11px;
  font-weight: 700;
  color: var(--muted);
  text-transform: uppercase;
  letter-spacing: 0.07em;
  margin: 0 0 8px;
}

.rules-section ul {
  margin: 0;
  padding-left: 16px;
  display: grid;
  gap: 6px;
}

.rules-section li {
  font-size: 13px;
  color: var(--text);
  line-height: 1.55;
}

.rules-expand-enter-active,
.rules-expand-leave-active {
  transition: opacity 0.2s ease, transform 0.2s ease;
  transform-origin: top;
}
.rules-expand-enter-from,
.rules-expand-leave-to {
  opacity: 0;
  transform: scaleY(0.95) translateY(-6px);
}
</style>
