<script setup>
import { ref, computed, onMounted } from 'vue'
import { fmtMins, fmtTime, fmtDate, minsToHMLabeled } from '../utils.js'
import { useDay } from '../composables/useDay.js'

const {
  day, loading, currentDate, isToday,
  refreshToday, goToday, goPrev, goNext,
  loadDay,
} = useDay()

const datePickerInput = ref(null)

function openDatePicker() {
  datePickerInput.value?.showPicker?.()
}

onMounted(() => refreshToday())

const workProgressPct = computed(() => {
  if (!day.value?.plannedMinutes) return 0
  return Math.min(100, Math.round((day.value.eligibleMinutes / day.value.plannedMinutes) * 100))
})

const isLate = computed(() => {
  if (!day.value?.start) return false
  const d = new Date(day.value.start)
  return d.getHours() * 60 + d.getMinutes() > 600 // strictly after 10:00
})

const deltaSign = computed(() => {
  const v = day.value?.eligibleDeltaMinutes ?? 0
  return v > 0 ? 'positive' : v < 0 ? 'negative' : 'neutral'
})

const flexSign = computed(() => {
  const v = day.value?.flexDeltaMinutes ?? 0
  return v > 0 ? 'positive' : v < 0 ? 'negative' : 'neutral'
})

const workDisplay = computed(() => day.value ? minsToHMLabeled(day.value.eligibleMinutes) : '—')
const plannedDisplay = computed(() => day.value ? minsToHMLabeled(day.value.plannedMinutes) : '—')

function punchLabel(idx, total) {
  if (idx === 0) return '上班'
  if (total > 1 && idx === total - 1) return '下班'
  return '中途'
}

function punchKind(idx, total) {
  if (idx === 0) return 'in'
  if (total > 1 && idx === total - 1) return 'out'
  return 'mid'
}
</script>

<template>
  <!-- ── Date navigation bar ── -->
  <div class="dnav">
    <div class="dnav-core">
      <button class="nav-arrow" @click="goPrev" :disabled="loading">‹</button>
      <div class="dnav-date-trigger" @click="openDatePicker" :class="{ faded: loading }">
        <span class="dnav-date-text">{{ fmtDate(currentDate) }}</span>
        <input
          ref="datePickerInput"
          type="date"
          class="hidden-picker"
          :value="currentDate"
          @change="loadDay($event.target.value)"
          :disabled="loading"
        />
      </div>
      <button class="nav-arrow" @click="goNext" :disabled="loading">›</button>
      <span v-if="isToday" class="pill-today">今天</span>
      <button v-else class="pill-back" @click="goToday" :disabled="loading">回到今天</button>
    </div>
    <button v-if="isToday" class="btn-refresh" @click="refreshToday" :disabled="loading">
      ↺ 重新整理
    </button>
  </div>

  <!-- ── Non-working day ── -->
  <div v-if="day && day.plannedMinutes === 0" class="nw-banner">
    <div class="nw-glyph">休</div>
    <div>
      <div class="nw-title">非工作日</div>
      <div class="nw-sub">{{ day.note || '今日工時不計入統計' }}</div>
    </div>
  </div>

  <!-- ── Working day: 2-column layout ── -->
  <div v-else class="day-layout">

    <!-- LEFT: Work summary -->
    <div class="work-card">
      <template v-if="day">
        <!-- Times hero: IN — progress — OUT -->
        <div class="times-hero">
          <div class="time-col">
            <div class="tc-eyebrow">上班</div>
            <div class="tc-time" :class="isLate ? 'time-late' : day.start ? 'time-ok' : 'time-empty'">
              {{ fmtTime(day.start) }}
            </div>
            <div class="tc-note" :class="{ 'note-late': isLate }">
              {{ isLate ? '遲到' : day.start ? '準時' : '待打卡' }}
            </div>
          </div>

          <div class="progress-col">
            <div class="pbar-track">
              <div class="pbar-fill" :style="{ width: workProgressPct + '%' }"></div>
            </div>
            <div class="pbar-legend">
              <span class="pbl-actual">{{ workDisplay }}</span>
              <span class="pbl-divider">/</span>
              <span class="pbl-planned">{{ plannedDisplay }}</span>
              <span class="pbl-pct">{{ workProgressPct }}%</span>
            </div>
          </div>

          <div class="time-col time-col-right">
            <div class="tc-eyebrow">下班</div>
            <div class="tc-time" :class="day.end ? '' : 'time-empty'">
              {{ fmtTime(day.end) }}
            </div>
            <div class="tc-note">{{ day.end ? '' : '未下班' }}</div>
          </div>
        </div>

        <!-- Stats tiles -->
        <div class="stats-strip">
          <div class="stat-tile" :class="deltaSign">
            <div class="st-label">工時差額</div>
            <div class="st-val">
              {{ fmtMins(day.eligibleDeltaMinutes) }}<span class="st-unit">分</span>
            </div>
          </div>
          <div class="stat-tile" :class="flexSign">
            <div class="st-label">今日彈性</div>
            <div class="st-val">
              {{ fmtMins(day.flexDeltaMinutes) }}<span class="st-unit">分</span>
            </div>
          </div>
          <div class="stat-tile neutral">
            <div class="st-label">實際工時</div>
            <div class="st-val st-val-mono">{{ workDisplay }}</div>
          </div>
        </div>

        <p v-if="day.note" class="day-note">{{ day.note }}</p>
      </template>

      <!-- Skeleton while loading -->
      <div v-else class="skeleton">
        <div class="sk-row">
          <div class="sk-block" style="height:56px;width:72px;"></div>
          <div class="sk-block" style="height:8px;flex:1;margin:24px 16px;"></div>
          <div class="sk-block" style="height:56px;width:72px;"></div>
        </div>
        <div class="sk-block" style="height:44px;margin-top:18px;"></div>
      </div>
    </div>

    <!-- RIGHT: Punch timeline -->
    <div class="punch-card">
      <div class="pc-head">
        <span class="pc-title">打卡紀錄</span>
        <span class="pc-badge">外部同步</span>
      </div>

      <div v-if="!day?.punches?.length" class="pc-empty">
        <div class="pce-ring">○</div>
        <div class="pce-msg">{{ loading ? '載入中…' : '尚無打卡紀錄' }}</div>
        <div v-if="!loading && day" class="pce-sub">等待外部系統同步</div>
      </div>

      <div v-else class="punch-timeline">
        <div v-for="(p, i) in day.punches" :key="p.id" class="pt-row">
          <div class="pt-track">
            <div class="pt-dot" :class="`dot-${punchKind(i, day.punches.length)}`"></div>
            <div v-if="i < day.punches.length - 1" class="pt-line"></div>
          </div>
          <div class="pt-body">
            <span class="pt-time">{{ fmtTime(p.at) }}</span>
            <span class="pt-tag" :class="`ptag-${punchKind(i, day.punches.length)}`">
              {{ punchLabel(i, day.punches.length) }}
            </span>
            <span v-if="p.note" class="pt-note">{{ p.note }}</span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* ── Date nav ─────────────────────────────── */
.dnav {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 18px;
  gap: 12px;
}
.dnav-core {
  display: flex;
  align-items: center;
  gap: 8px;
}
.nav-arrow {
  width: 32px;
  height: 32px;
  padding: 0;
  border-radius: 8px;
  border: 1px solid var(--line);
  background: transparent;
  color: var(--muted);
  font-size: 20px;
  line-height: 1;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  transition: all 0.15s;
}
.nav-arrow:hover {
  background: rgba(37, 99, 235, 0.06);
  color: var(--primary);
  border-color: rgba(37, 99, 235, 0.3);
  filter: none;
}
.nav-arrow:disabled { opacity: 0.4; cursor: default; }
.dnav-date-trigger {
  position: relative;
  cursor: pointer;
  padding: 4px 10px;
  border-radius: 8px;
  transition: background 0.15s;
}
.dnav-date-trigger:hover { background: rgba(37, 99, 235, 0.05); }
.dnav-date-trigger.faded { opacity: 0.5; cursor: default; }
.dnav-date-text {
  font-size: 20px;
  font-weight: 700;
  color: var(--text);
  letter-spacing: -0.02em;
}
.hidden-picker {
  position: absolute;
  opacity: 0;
  pointer-events: none;
  width: 0;
  height: 0;
}
.pill-today {
  display: inline-block;
  padding: 3px 11px;
  border-radius: 999px;
  font-size: 11px;
  font-weight: 700;
  background: rgba(37, 99, 235, 0.08);
  color: var(--primary);
  border: 1px solid rgba(37, 99, 235, 0.2);
  letter-spacing: 0.02em;
}
.pill-back {
  padding: 3px 12px;
  border-radius: 999px;
  font-size: 12px;
  font-weight: 600;
  background: transparent;
  color: var(--primary);
  border: 1px solid rgba(37, 99, 235, 0.3);
  cursor: pointer;
  transition: all 0.15s;
  font-family: inherit;
}
.pill-back:hover { background: rgba(37, 99, 235, 0.08); filter: none; }
.btn-refresh {
  padding: 6px 14px;
  border-radius: 8px;
  font-size: 12px;
  font-weight: 500;
  background: transparent;
  color: var(--muted);
  border: 1px solid var(--line);
  cursor: pointer;
  transition: all 0.15s;
  font-family: inherit;
}
.btn-refresh:hover { color: var(--text); border-color: #c8cdd5; filter: none; }
.btn-refresh:disabled { opacity: 0.4; cursor: default; }

/* ── Non-working day ──────────────────────── */
.nw-banner {
  display: flex;
  align-items: center;
  gap: 20px;
  padding: 28px 24px;
  background: var(--card);
  border: 1px solid var(--line);
  border-radius: 14px;
  box-shadow: 0 2px 10px rgba(0,0,0,0.04);
}
.nw-glyph {
  width: 52px;
  height: 52px;
  border-radius: 12px;
  background: rgba(107, 114, 128, 0.08);
  color: var(--muted);
  font-size: 20px;
  font-weight: 800;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}
.nw-title {
  font-size: 16px;
  font-weight: 700;
  color: var(--text);
}
.nw-sub {
  font-size: 13px;
  color: var(--muted);
  margin-top: 3px;
}

/* ── 2-column layout ──────────────────────── */
.day-layout {
  display: grid;
  grid-template-columns: 3fr 2fr;
  gap: 16px;
  align-items: start;
}
@media (max-width: 720px) {
  .day-layout { grid-template-columns: 1fr; }
}

/* ── Card base ────────────────────────────── */
.work-card,
.punch-card {
  background: var(--card);
  border: 1px solid var(--line);
  border-radius: 14px;
  padding: 22px;
  box-shadow: 0 2px 10px rgba(0,0,0,0.04);
}

/* ── Times hero ───────────────────────────── */
.times-hero {
  display: flex;
  align-items: flex-end;
  gap: 16px;
  margin-bottom: 20px;
}
.time-col {
  display: flex;
  flex-direction: column;
  gap: 3px;
  flex-shrink: 0;
}
.time-col-right { text-align: right; }
.tc-eyebrow {
  font-size: 10px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.12em;
  color: var(--muted);
}
.tc-time {
  font-size: 38px;
  font-weight: 700;
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
  letter-spacing: -0.03em;
  line-height: 1;
  color: var(--text);
}
.time-ok   { color: #059669; }
.time-late { color: var(--danger); }
.time-empty { color: var(--muted); opacity: 0.45; }
.tc-note {
  font-size: 11px;
  font-weight: 500;
  color: var(--muted);
}
.note-late { color: var(--danger); }

/* Progress bar */
.progress-col {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 7px;
  padding-bottom: 6px;
}
.pbar-track {
  height: 5px;
  background: var(--line);
  border-radius: 3px;
  overflow: hidden;
}
.pbar-fill {
  height: 100%;
  background: linear-gradient(90deg, rgba(37,99,235,0.55) 0%, var(--primary) 100%);
  border-radius: 3px;
  transition: width 0.6s cubic-bezier(0.4, 0, 0.2, 1);
}
.pbar-legend {
  display: flex;
  align-items: baseline;
  gap: 4px;
  font-size: 12px;
}
.pbl-actual {
  font-weight: 700;
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
  color: var(--text);
  font-size: 13px;
}
.pbl-divider,
.pbl-planned {
  color: var(--muted);
  font-size: 11px;
}
.pbl-pct {
  margin-left: auto;
  font-weight: 700;
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
  color: var(--primary);
  font-size: 11px;
}

/* ── Stats strip ──────────────────────────── */
.stats-strip {
  display: grid;
  grid-template-columns: 1fr 1fr 1fr;
  gap: 10px;
}
.stat-tile {
  border-radius: 10px;
  padding: 12px 14px;
  border: 1px solid var(--line);
  background: #f9fafb;
  display: flex;
  flex-direction: column;
  gap: 4px;
  transition: border-color 0.15s;
}
.stat-tile.positive {
  background: rgba(5, 150, 105, 0.05);
  border-color: rgba(5, 150, 105, 0.22);
}
.stat-tile.negative {
  background: rgba(220, 38, 38, 0.04);
  border-color: rgba(220, 38, 38, 0.2);
}
.st-label {
  font-size: 10px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.1em;
  color: var(--muted);
}
.st-val {
  font-size: 22px;
  font-weight: 700;
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
  color: var(--text);
  line-height: 1.1;
}
.st-val-mono { letter-spacing: -0.02em; }
.positive .st-val { color: #059669; }
.negative .st-val { color: var(--danger); }
.st-unit {
  font-size: 12px;
  font-weight: 400;
  margin-left: 2px;
  opacity: 0.65;
  font-family: inherit;
}
.day-note {
  margin: 14px 0 0;
  padding: 9px 14px;
  background: rgba(37, 99, 235, 0.04);
  border-left: 3px solid rgba(37, 99, 235, 0.28);
  border-radius: 0 8px 8px 0;
  font-size: 12.5px;
  color: var(--muted);
}

/* ── Skeleton ─────────────────────────────── */
.skeleton { display: flex; flex-direction: column; gap: 12px; }
.sk-row { display: flex; align-items: center; gap: 12px; }
.sk-block {
  background: var(--line);
  border-radius: 8px;
  animation: sk-pulse 1.6s ease infinite;
}
@keyframes sk-pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.45; }
}

/* ── Punch card ───────────────────────────── */
.pc-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 16px;
  padding-bottom: 12px;
  border-bottom: 1px solid var(--line);
}
.pc-title {
  font-size: 12px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.08em;
  color: var(--text);
}
.pc-badge {
  font-size: 10px;
  font-weight: 600;
  color: var(--muted);
  border: 1px solid var(--line);
  border-radius: 999px;
  padding: 2px 8px;
  background: #f9fafb;
  letter-spacing: 0.03em;
}

.pc-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 28px 16px;
  gap: 5px;
  text-align: center;
}
.pce-ring {
  font-size: 26px;
  color: var(--line);
  margin-bottom: 6px;
  line-height: 1;
}
.pce-msg {
  font-size: 13px;
  font-weight: 500;
  color: var(--muted);
}
.pce-sub {
  font-size: 11px;
  color: var(--muted);
  opacity: 0.7;
}

/* ── Punch timeline ───────────────────────── */
.punch-timeline { display: flex; flex-direction: column; }
.pt-row {
  display: flex;
  gap: 12px;
}
.pt-track {
  display: flex;
  flex-direction: column;
  align-items: center;
  flex-shrink: 0;
  width: 14px;
  padding-top: 3px;
}
.pt-dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  border: 2px solid var(--line);
  background: var(--card);
  flex-shrink: 0;
  transition: border-color 0.15s;
}
.dot-in  { border-color: #059669; background: rgba(5, 150, 105, 0.12); }
.dot-out { border-color: var(--primary); background: rgba(37, 99, 235, 0.1); }
.dot-mid { border-color: #c8cdd5; background: rgba(107, 114, 128, 0.08); }

.pt-line {
  width: 1.5px;
  flex: 1;
  min-height: 20px;
  background: var(--line);
  margin: 3px 0;
}
.pt-body {
  display: flex;
  align-items: baseline;
  gap: 7px;
  padding-bottom: 14px;
  flex-wrap: wrap;
}
.pt-time {
  font-size: 15px;
  font-weight: 700;
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
  color: var(--text);
  letter-spacing: -0.02em;
}
.pt-tag {
  font-size: 10px;
  font-weight: 700;
  padding: 2px 7px;
  border-radius: 4px;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}
.ptag-in  { background: rgba(5, 150, 105, 0.1);  color: #059669; }
.ptag-out { background: rgba(37, 99, 235, 0.08); color: var(--primary); }
.ptag-mid { background: rgba(107, 114, 128, 0.08); color: var(--muted); }
.pt-note {
  font-size: 11px;
  color: var(--muted);
  font-style: italic;
  width: 100%;
  margin-top: -6px;
}
</style>
