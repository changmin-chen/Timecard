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

onMounted(() => {
    const now = new Date()
    const yyyy = now.getFullYear()
    const mm = String(now.getMonth() + 1).padStart(2, '0')
    monthPick.value = `${yyyy}-${mm}`
    // load() is triggered by the monthPick watcher below
})

watch(monthPick, () => load())

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

// Only show past/present rows; future dates have no data worth displaying
const visibleDays = computed(() => {
    if (!month.value) return []
    return month.value.days.filter(d => !isFuture(d.date))
})

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
                <div class="hint">預設僅顯示今日（含）以前的有紀錄日期。勾選「顯示所有日期」可列出當月完整記錄（含未出勤日）。</div>
            </div>
            <div class="actions">
                <input type="month" v-model="monthPick"/>
                <label class="inline small">
                    <input type="checkbox" v-model="includeEmpty"/>
                    顯示所有日期
                </label>
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
                <span class="month-stat-label">累計不足</span>
                <span class="month-stat-value" :class="deficitCls(month.settledDeficitMinutes)">{{
                        month.settledDeficitMinutes || '\u2014'
                    }}{{ month.settledDeficitMinutes ? ' 分' : '' }}</span>
            </div>
            <div class="month-stat-card">
                <span class="month-stat-label">出勤天數</span>
                <span class="month-stat-value">{{ attendedDays }} / {{ totalWorkDays }}</span>
            </div>
        </div>

        <div v-if="month && !visibleDays.length" class="hint">這個月份目前沒有資料。</div>
        <div v-if="month && visibleDays.length">
            <div class="tableWrap">
                <table style="min-width: 560px;">
                    <thead>
                    <tr>
                        <th>日期</th>
                        <th>工時</th>
                        <th>彈性增減</th>
                        <th>不足</th>
                        <th>備註</th>
                    </tr>
                    </thead>
                    <tbody>
                    <tr v-for="d in visibleDays" :key="d.date">
                        <td class="mono">{{ fmtDateShort(d.date) }}<span v-if="d.isNonWorkingDay"
                                                                         class="badge"> OFF</span></td>
                        <td class="mono">{{ d.eligibleMinutes || '\u2014' }}</td>
                        <td class="mono" :class="deltaCls(d.flexDeltaMinutes)">{{
                                d.flexDeltaMinutes !== 0 ? mins(d.flexDeltaMinutes) : '\u2014'
                            }}</td>
                        <td class="mono" :class="deficitCls(d.deficitMinutes)">{{
                                d.deficitMinutes || ''
                            }}</td>
                        <td>{{ d.note || '' }}</td>
                    </tr>
                    </tbody>
                </table>
            </div>
        </div>
    </section>
</template>
