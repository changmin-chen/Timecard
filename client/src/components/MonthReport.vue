<script setup>
import {ref, computed, onMounted, watch} from 'vue'
import {mins} from '../utils.js'
import {useMonth} from '../composables/useMonth.js'
import {useMonthInvalidation} from '../composables/useMonthInvalidation.js'

const weekdays = ['日', '一', '二', '三', '四', '五', '六']

const {month, loading, loadMonth} = useMonth()
const {monthStale} = useMonthInvalidation()

const monthPick = ref('')
const includeEmpty = ref(false)
const showDetail = ref(false)

onMounted(() => {
    const now = new Date()
    const yyyy = now.getFullYear()
    const mm = String(now.getMonth() + 1).padStart(2, '0')
    monthPick.value = `${yyyy}-${mm}`
    load()
})

watch(monthStale, () => {
    if (month.value) load()
})

watch(includeEmpty, () => {
    if (month.value) load()
})

function load() {
    const match = /^(\d{4})-(\d{2})$/.exec(monthPick.value)
    if (!match) return
    const y = Number(match[1])
    const m = Number(match[2])
    loadMonth(y, m, includeEmpty.value)
}

function isFuture(dateStr) {
    return !!month.value && dateStr > month.value.asOf
}

function fmtDateShort(dateStr) {
    if (!dateStr) return '\u2014'
    const [y, m, d] = dateStr.split('-').map(Number)
    const dt = new Date(y, m - 1, d)
    const wd = weekdays[dt.getDay()]
    return `${String(m).padStart(2, '0')}/${String(d).padStart(2, '0')} (${wd})`
}

function deltaCls(d) {
    return d < 0 ? 'bad' : d > 0 ? 'good' : ''
}

function deficitCls(d) {
    return d > 0 ? 'bad' : ''
}

const attendedDays = computed(() => {
    if (!month.value) return 0
    return month.value.days.filter(d => d.punchCount > 0).length
})

const totalWorkDays = computed(() => {
    if (!month.value) return 0
    return month.value.days.filter(d => !d.isNonWorkingDay).length
})
</script>

<template>
    <section class="card">
        <div class="row">
            <div>
                <h2>月報表</h2>
                <div class="hint">預設不含空白日，避免週末被當成欠工時。勾選「顯示所有日期」才會列出整個月。</div>
            </div>
            <div class="actions">
                <input type="month" v-model="monthPick"/>
                <label class="inline small">
                    <input type="checkbox" v-model="includeEmpty"/>
                    顯示所有日期
                </label>
                <button class="ghost" @click="load" :disabled="loading">重新整理</button>
            </div>
        </div>

        <div v-if="month" class="month-stats">
            <div class="month-stat-card">
                <span class="month-stat-label">彈性餘額</span>
                <span class="month-stat-value" :class="deltaCls(month.settledFlexBankMinutes)">{{
                        mins(month.settledFlexBankMinutes)
                    }} 分</span>
            </div>
            <div class="month-stat-card">
                <span class="month-stat-label">已結算不足</span>
                <span class="month-stat-value" :class="deficitCls(month.settledDeficitMinutes)">{{ month.settledDeficitMinutes }} 分</span>
            </div>
            <div class="month-stat-card">
                <span class="month-stat-label">出勤天數</span>
                <span class="month-stat-value">{{ attendedDays }} / {{ totalWorkDays }} 天</span>
            </div>
        </div>

        <div v-if="month && !month.days.length" class="hint">這個月份目前沒有資料。</div>
        <div v-if="month && month.days.length">
            <label class="inline small" style="margin-top: 12px;">
                <input type="checkbox" v-model="showDetail"/>
                顯示計算明細
            </label>
            <div class="tableWrap">
                <table :style="{ minWidth: showDetail ? '1100px' : '680px' }">
                    <thead>
                    <tr>
                        <th>日期</th>
                        <th>出勤</th>
                        <th>差額</th>
                        <th>彈性套用</th>
                        <th>不足</th>
                        <th v-if="showDetail">打卡</th>
                        <th>備註</th>
                    </tr>
                    </thead>
                    <tbody>
                    <tr v-for="d in month.days" :key="d.date" :class="{ future: isFuture(d.date) }">
                        <td class="mono">{{ fmtDateShort(d.date) }}<span v-if="d.isNonWorkingDay"
                                                                         class="badge"> OFF</span></td>
                        <td class="mono">{{ d.eligibleMinutes }}/{{ d.plannedMinutes }}</td>
                        <td class="mono" :class="deltaCls(d.eligibleDeltaMinutes)">{{
                                mins(d.eligibleDeltaMinutes)
                            }}
                        </td>
                        <td class="mono">{{ mins(d.flexDeltaMinutes) }}</td>
                        <td class="mono" :class="deficitCls(d.deficitMinutes)">
                            {{ isFuture(d.date) ? '\u2014' : (d.deficitMinutes ? d.deficitMinutes : '') }}
                        </td>
                        <td v-if="showDetail" class="mono">{{ d.punchCount }}</td>
                        <td>{{ d.note || '' }}</td>
                    </tr>
                    </tbody>
                </table>
            </div>
        </div>
    </section>
</template>
