<script setup>
import {ref, computed, onMounted, watch} from 'vue'
import {fmtMins, minsToHMLabeled, fmtMinsHMLabeled, fmtTime, categoryLabel, categoryClass} from '../utils.js'
import {useMonth} from '../composables/useMonth.js'
import {useMonthInvalidation} from '../composables/useMonthInvalidation.js'

const weekdays = ['日', '一', '二', '三', '四', '五', '六']

const {month, loading, loadMonth} = useMonth()
const {monthStale} = useMonthInvalidation()

const monthPick = ref('')

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

function load() {
    const match = /^(\d{4})-(\d{2})$/.exec(monthPick.value)
    if (!match) return
    const y = Number(match[1])
    const m = Number(match[2])
    loadMonth(y, m, true)
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

// ── Calendar helpers ──────────────────────────────────────────────

function startMinutesTW(isoStr) {
    const d = new Date(isoStr)
    return ((d.getUTCHours() + 8) % 24) * 60 + d.getUTCMinutes()
}

function fmtStartTimeTW(isoStr) {
    const d = new Date(isoStr)
    const h = (d.getUTCHours() + 8) % 24
    const m = d.getUTCMinutes()
    return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`
}

function getCellBadges(d) {
    // NonWorkingDay: no badges — cell is dimmed by CSS
    if (d.isNonWorkingDay) return []

    const all = []
    if (d.start) {
        const isLate = startMinutesTW(d.start) > 10 * 60  // AM 10:00
        all.push({ text: fmtStartTimeTW(d.start), cls: isLate ? 'punch-late' : 'punch-ok' })
    } else if (!d.attendanceRequests?.length) {
        all.push({ text: '缺勤', cls: 'absent' })
    }
    for (const r of (d.attendanceRequests || [])) {
        all.push({ text: categoryLabel(r.category), cls: categoryClass(r.category) || '' })
    }
    return all
}

// Map of date string → day data for O(1) lookup
const dayMap = computed(() => {
    if (!month.value) return {}
    return Object.fromEntries(month.value.days.map(d => [d.date, d]))
})

// Full month day sequence for the calendar (all days 1..N)
// inScope=true → day was returned by the API (may or may not have a WorkDay record)
const calendarCells = computed(() => {
    if (!month.value) return []
    const { year, month: m } = month.value
    const daysInMonth = new Date(year, m, 0).getDate()
    return Array.from({ length: daysInMonth }, (_, i) => {
        const day = String(i + 1).padStart(2, '0')
        const dateStr = `${year}-${String(m).padStart(2, '0')}-${day}`
        const fromApi = dayMap.value[dateStr]
        return fromApi
            ? { ...fromApi, inScope: true }
            : { date: dateStr, inScope: false, exists: false, isNonWorkingDay: false, note: '', start: null, attendanceRequests: [] }
    })
})

// Day-of-week (0=Sun) for the 1st of the displayed month
const firstDayOffset = computed(() => {
    if (!month.value) return 0
    return new Date(month.value.year, month.value.month - 1, 1).getDay()
})

function isToday(dateStr) {
    return !!month.value && dateStr === month.value.asOf
}
</script>

<template>
    <section class="card">
        <div class="row">
            <h2>月報表</h2>
            <div class="actions">
                <input type="month" v-model="monthPick"/>
            </div>
        </div>

        <div v-if="month" class="month-stats">
            <div class="month-stat-card">
                <span class="month-stat-label">彈性餘額</span>
                <span class="month-stat-value" :class="deltaCls(month.settledFlexBankMinutes)">{{
                        fmtMins(month.settledFlexBankMinutes)
                    }} 分</span>
            </div>
            <div class="month-stat-card">
                <span class="month-stat-label">累計不足</span>
                <span class="month-stat-value" :class="deficitCls(month.settledDeficitMinutes)">{{
                        month.settledDeficitMinutes ? `${month.settledDeficitMinutes} 分` : '\u2014'
                    }}</span>
            </div>
            <div class="month-stat-card">
                <span class="month-stat-label">出勤天數</span>
                <span class="month-stat-value">{{ attendedDays }} / {{ totalWorkDays }}</span>
            </div>
        </div>

        <!-- Calendar grid -->
        <div v-if="month" class="cal-grid">
            <div class="cal-header-cell" v-for="wd in ['日','一','二','三','四','五','六']" :key="wd">{{ wd }}</div>
            <div class="cal-empty" v-for="n in firstDayOffset" :key="'e'+n"></div>
            <div
                v-for="d in calendarCells"
                :key="d.date"
                class="cal-cell"
                :class="{
                    'cal-today': isToday(d.date),
                    'cal-future': isFuture(d.date),
                    'cal-nonworking': d.inScope && d.isNonWorkingDay
                }"
            >
                <div class="cal-day-num" :class="{ 'cal-today-num': isToday(d.date) }">
                    {{ Number(d.date.slice(8)) }}
                </div>
                <div v-if="d.inScope && !isFuture(d.date)" class="cal-badges">
                    <span
                        v-for="(b, i) in getCellBadges(d).slice(0, 2)"
                        :key="i"
                        class="cal-badge"
                        :class="b.cls"
                    >{{ b.text }}</span>
                    <span
                        v-if="getCellBadges(d).length > 2"
                        class="cal-badge overflow"
                    >+{{ getCellBadges(d).length - 2 }}</span>
                </div>
            </div>
        </div>

        <div v-if="month && !visibleDays.length" class="hint">這個月份目前沒有資料。</div>
        <div v-if="month && visibleDays.length">
            <div class="tableWrap">
                <table style="min-width: 560px;">
                    <thead>
                    <tr>
                        <th>日期</th>
                        <th>上班</th>
                        <th>下班</th>
                        <th>工時</th>
                        <th>彈性</th>
                        <th>不足</th>
                        <th>狀態</th>
                    </tr>
                    </thead>
                    <tbody>
                    <tr v-for="d in visibleDays" :key="d.date" :class="{ 'row-nonworking': d.isNonWorkingDay }">
                        <td class="mono">{{ fmtDateShort(d.date) }}</td>
                        <td class="mono">{{ fmtTime(d.start) }}</td>
                        <td class="mono">{{ fmtTime(d.end) }}</td>
                        <td class="mono">{{ d.eligibleMinutes ? minsToHMLabeled(d.eligibleMinutes) : '\u2014' }}</td>
                        <td class="mono" :class="deltaCls(d.flexDeltaMinutes)">{{ fmtMinsHMLabeled(d.flexDeltaMinutes) }}</td>
                        <td class="mono" :class="deficitCls(d.deficitMinutes)">{{ d.deficitMinutes ? minsToHMLabeled(d.deficitMinutes) : '' }}</td>
                        <td class="status-cell">
                            <template v-if="d.isNonWorkingDay">
                                <span class="st-tag st-dim"><span class="sym sym-diamond"></span>{{ d.note || '非工作日' }}</span>
                            </template>
                            <template v-else-if="d.attendanceRequests?.length">
                                <span v-for="r in d.attendanceRequests" :key="r.category" class="st-tag st-dim st-req"><span class="sym sym-diamond"></span>{{ categoryLabel(r.category) }}</span>
                            </template>
                            <template v-else-if="d.start">
                                <span class="st-tag st-dim"><span class="sym sym-circle"></span>正常</span>
                            </template>
                            <template v-else>
                                <span class="st-tag st-bad"><span class="sym sym-triangle"></span>缺勤</span>
                            </template>
                        </td>
                    </tr>
                    </tbody>
                </table>
            </div>
        </div>
    </section>
</template>
