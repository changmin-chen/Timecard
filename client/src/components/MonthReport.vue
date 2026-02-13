<script setup>
import { ref, onMounted } from 'vue'
import { mins } from '../utils.js'
import { useMonth } from '../composables/useMonth.js'

const { month, loading, loadMonth } = useMonth()

const monthPick = ref('')
const includeEmpty = ref(false)

onMounted(() => {
  const now = new Date()
  const yyyy = now.getFullYear()
  const mm = String(now.getMonth() + 1).padStart(2, '0')
  monthPick.value = `${yyyy}-${mm}`
  load()
})

function load() {
  const match = /^(\d{4})-(\d{2})$/.exec(monthPick.value)
  if (!match) return
  const y = Number(match[1])
  const m = Number(match[2])
  loadMonth(y, m, includeEmpty.value)
}

function monthSummary(m) {
  const parts = [`${m.year} 年 ${m.month} 月`, `彈性餘額：${mins(m.flexBankBalance)}`]
  if (m.totalDeficitMinutes > 0) parts.push(`累計不足：${mins(m.totalDeficitMinutes)}`)
  parts.push(`共 ${m.days.length} 天`)
  return parts.join(' ｜ ')
}

function deltaCls(d) {
  return d < 0 ? 'bad' : d > 0 ? 'good' : ''
}

function deficitCls(d) {
  return d > 0 ? 'bad' : ''
}
</script>

<template>
  <section class="card">
    <div class="row">
      <div>
        <h2>月報表</h2>
        <div class="hint">預設不含空白日，避免週末被當成欠工時。勾選「顯示所有日期」才會列出整個月。</div>
      </div>
      <div class="actions">
        <input type="month" v-model="monthPick" />
        <label class="inline small">
          <input type="checkbox" v-model="includeEmpty" />
          顯示所有日期
        </label>
        <button class="ghost" @click="load" :disabled="loading">載入</button>
      </div>
    </div>

    <div v-if="month" class="hint" style="margin-top: 12px; font-size: 15px; font-weight: 600; color: var(--text);">
      {{ monthSummary(month) }}
    </div>
    <div v-if="month && !month.days.length" class="hint">這個月份目前沒有資料。</div>
    <div v-if="month && month.days.length" class="tableWrap">
      <table>
        <thead>
          <tr>
            <th>日期</th>
            <th>Punch數</th>
            <th>規定</th>
            <th>已記錄</th>
            <th>延伸</th>
            <th>有效</th>
            <th>差額</th>
            <th>彈性候選</th>
            <th>實際套用</th>
            <th>彈性餘額</th>
            <th>不足</th>
            <th>備註</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="d in month.days" :key="d.date">
            <td class="mono">{{ d.date }}<span v-if="d.isNonWorkingDay" class="badge"> OFF</span></td>
            <td class="mono">{{ d.punchCount }}</td>
            <td class="mono">{{ d.plannedMinutes }}</td>
            <td class="mono">{{ d.workedMinutes }}</td>
            <td class="mono">{{ mins(d.extensionMinutes) }}</td>
            <td class="mono">{{ d.effectiveMinutes }}</td>
            <td class="mono" :class="deltaCls(d.deltaMinutes)">{{ mins(d.deltaMinutes) }}</td>
            <td class="mono">{{ mins(d.flexDeltaMinutes) }}</td>
            <td class="mono">{{ mins(d.flexUsedMinutes) }}</td>
            <td class="mono">{{ d.flexBankBalance }}</td>
            <td class="mono" :class="deficitCls(d.deficitMinutes)">{{ d.deficitMinutes ? d.deficitMinutes : '' }}</td>
            <td>{{ d.note || '' }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</template>
