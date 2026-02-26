<script setup>
import { ref } from 'vue'
const open = ref(false)
</script>

<template>
  <div class="rules-wrap">
    <button class="rules-trigger" @click="open = !open">
      <span class="rt-left">
        <span class="rt-icon">◈</span>
        <span class="rt-label">計算規則說明</span>
      </span>
      <span class="rt-arrow" :class="{ rotated: open }">›</span>
    </button>

    <Transition name="rules-expand">
      <div v-if="open" class="rules-body">
        <div class="rule-section">
          <div class="rs-heading">工時計算基礎</div>
          <ul class="rs-list">
            <li>每日標準工時：<strong>9 小時（540 分鐘）</strong></li>
            <li>有效計時區間：<strong>07:30 ～ 19:00</strong>（超出此範圍的時間不予計入）</li>
            <li>計算方式：以當日<strong>最早打卡至最晚打卡</strong>的時間區間為準</li>
            <li>當日僅有 1 筆打卡紀錄時，視為尚未下班，工時計為 0</li>
          </ul>
        </div>
        <div class="rule-section">
          <div class="rs-heading">出勤申請</div>
          <ul class="rs-list">
            <li>請假、特休、出差等申請，計入當日有效工時</li>
            <li>出勤申請可與打卡紀錄並行計算，取聯集後加總</li>
          </ul>
        </div>
        <div class="rule-section">
          <div class="rs-heading">彈性時數制度</div>
          <ul class="rs-list">
            <li>每日彈性上限：差額最多累積或扣抵 <strong>±55 分鐘</strong></li>
            <li>超出 ±55 分鐘的部分不計入彈性銀行</li>
            <li>休假日、補假日等免上班日，不累積彈性時數</li>
            <li>彈性時數<strong>按月累積</strong>，每月底自動重置歸零</li>
          </ul>
        </div>
        <div class="rule-section">
          <div class="rs-heading">不足時數</div>
          <ul class="rs-list">
            <li>提領全部彈性時數後仍有缺口，記為「不足」時數</li>
            <li>建議以出勤申請（如請假、特休）補足不足時數</li>
          </ul>
        </div>
      </div>
    </Transition>
  </div>
</template>

<style scoped>
.rules-wrap {
  margin-top: 12px;
  background: var(--card);
  border: 1px solid var(--line);
  border-radius: 12px;
  overflow: hidden;
}

.rules-trigger {
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 13px 18px;
  background: transparent;
  border: none;
  cursor: pointer;
  transition: background 0.15s;
  font-family: inherit;
}
.rules-trigger:hover {
  background: rgba(0, 0, 0, 0.02);
  filter: none;
}

.rt-left {
  display: flex;
  align-items: center;
  gap: 8px;
}
.rt-icon {
  font-size: 13px;
  color: var(--muted);
  opacity: 0.7;
}
.rt-label {
  font-size: 12.5px;
  font-weight: 600;
  color: var(--muted);
  letter-spacing: 0.01em;
}
.rt-arrow {
  font-size: 16px;
  color: var(--muted);
  opacity: 0.5;
  display: inline-block;
  transition: transform 0.2s ease;
}
.rt-arrow.rotated {
  transform: rotate(90deg);
}

.rules-body {
  padding: 4px 18px 18px;
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 16px 24px;
  border-top: 1px solid var(--line);
}

.rs-heading {
  font-size: 10px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.1em;
  color: var(--muted);
  margin: 14px 0 8px;
}

.rs-list {
  margin: 0;
  padding-left: 14px;
  display: grid;
  gap: 5px;
}
.rs-list li {
  font-size: 12.5px;
  color: var(--text);
  line-height: 1.55;
}
.rs-list strong {
  font-weight: 700;
  color: var(--text);
}

/* Transition */
.rules-expand-enter-active,
.rules-expand-leave-active {
  transition: opacity 0.2s ease, transform 0.2s ease;
  transform-origin: top;
}
.rules-expand-enter-from,
.rules-expand-leave-to {
  opacity: 0;
  transform: scaleY(0.96) translateY(-6px);
}
</style>
