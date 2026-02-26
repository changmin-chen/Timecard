<script setup>
import {reactive, watch, ref, computed} from 'vue'
import {categoryClass} from '../utils.js'
import TimeInput from './TimeInput.vue'

const props = defineProps({
    date: {type: String, default: ''},
    loading: {type: Boolean, default: false},
})
const emit = defineEmits(['submit'])

const categories = [
    {value: 'Leave', label: '請假'},
    {value: 'AnnualLeave', label: '特休'},
    {value: 'Trip', label: '出差'},
    {value: 'Other', label: '其他'},
]

const form = reactive({
    date: '',
    category: 'Leave',
    start: '',
    end: '',
    note: '',
})
const customCategory = ref('')

const presets = [
    {label: '整天', start: '07:30', end: '16:30'},
    {label: '上午', start: '07:30', end: '12:00'},
    {label: '下午', start: '13:00', end: '16:30'},
]

function applyPreset(p) {
    form.start = p.start
    form.end = p.end
}

watch(() => props.date, (val) => {
    form.date = val
}, {immediate: true})

const timeError = computed(() => {
    if (form.start && form.end && form.end <= form.start) {
        return '結束時間必須晚於起始時間'
    }
    return ''
})

const duration = computed(() => {
    if (!form.start || !form.end) return ''
    const [sh, sm] = form.start.split(':').map(Number)
    const [eh, em] = form.end.split(':').map(Number)
    const mins = (eh * 60 + em) - (sh * 60 + sm)
    if (mins <= 0) return ''
    const h = Math.floor(mins / 60)
    const m = mins % 60
    if (h === 0) return `${m} 分鐘`
    if (m === 0) return `${h} 小時`
    return `${h} 小時 ${m} 分`
})

const canSubmit = computed(() => {
    return !props.loading && !timeError.value && !!form.start && !!form.end
})

function adjustTime(field, deltaMinutes) {
    const current = form[field]
    let h = 0, m = 0
    if (current) {
        const parts = current.split(':').map(Number)
        h = parts[0]; m = parts[1]
    } else {
        const now = new Date()
        h = now.getHours(); m = now.getMinutes()
    }
    let total = h * 60 + m + deltaMinutes
    total = Math.max(0, Math.min(total, 23 * 60 + 59))
    const nh = Math.floor(total / 60)
    const nm = total % 60
    form[field] = String(nh).padStart(2, '0') + ':' + String(nm).padStart(2, '0')
}

function pillStyle(cat) {
    const cls = categoryClass(cat.value)
    const colorMap = {
        leave: {bg: 'rgba(234,179,8,.1)', color: '#a16207', border: 'rgba(234,179,8,.3)'},
        annual: {bg: 'rgba(16,185,129,.08)', color: '#059669', border: 'rgba(16,185,129,.25)'},
        trip: {bg: 'rgba(14,165,233,.08)', color: '#0369a1', border: 'rgba(14,165,233,.25)'},
    }
    const isSelected = form.category === cat.value
    const colors = colorMap[cls]
    if (isSelected && colors) {
        return {background: colors.bg, color: colors.color, borderColor: colors.border}
    }
    if (isSelected) {
        return {background: 'rgba(37,99,235,.08)', color: 'var(--primary)', borderColor: 'rgba(37,99,235,.3)'}
    }
    return {}
}

function onSubmit() {
    const cat = form.category === 'Other' ? customCategory.value.trim() : form.category
    emit('submit', {
        date: form.date,
        category: cat,
        start: form.start,
        end: form.end,
        note: form.note.trim(),
    })
    form.note = ''
    customCategory.value = ''
}
</script>

<template>
    <h3>新增出勤申請</h3>
    <form class="form" @submit.prevent="onSubmit">
        <div class="field">
            <span class="field-label">類別</span>
            <div class="category-pills">
                <button
                    v-for="c in categories"
                    :key="c.value"
                    type="button"
                    class="category-pill"
                    :class="{selected: form.category === c.value}"
                    :style="pillStyle(c)"
                    @click="form.category = c.value"
                >{{ c.label }}</button>
            </div>
        </div>
        <Transition name="fade">
            <label v-if="form.category === 'Other'">
                自訂類別
                <input type="text" v-model="customCategory" placeholder="輸入自訂類別" required/>
            </label>
        </Transition>
        <div class="field">
            <span class="field-label">快速填入</span>
            <div class="preset-pills">
                <button
                    v-for="p in presets"
                    :key="p.label"
                    type="button"
                    class="preset-pill"
                    @click="applyPreset(p)"
                >
                    <span class="preset-name">{{ p.label }}</span>
                    <span class="preset-range">{{ p.start }}–{{ p.end }}</span>
                </button>
            </div>
        </div>
        <div class="form-row">
            <div class="field">
                <span class="field-label">起始時間</span>
                <TimeInput v-model="form.start"/>
                <div class="time-adjust">
                    <button type="button" @click="adjustTime('start', -30)">-30</button>
                    <button type="button" @click="adjustTime('start', -5)">-5</button>
                    <button type="button" @click="adjustTime('start', 5)">+5</button>
                    <button type="button" @click="adjustTime('start', 30)">+30</button>
                </div>
            </div>
            <div class="field">
                <span class="field-label">結束時間</span>
                <TimeInput v-model="form.end"/>
                <div class="time-adjust">
                    <button type="button" @click="adjustTime('end', -30)">-30</button>
                    <button type="button" @click="adjustTime('end', -5)">-5</button>
                    <button type="button" @click="adjustTime('end', 5)">+5</button>
                    <button type="button" @click="adjustTime('end', 30)">+30</button>
                </div>
            </div>
        </div>
        <p v-if="timeError" class="field-error">{{ timeError }}</p>
        <p v-else-if="duration" class="duration-hint">共 {{ duration }}</p>
        <label>
            註記
            <input type="text" v-model="form.note" placeholder="可留空"/>
        </label>
        <div class="actions">
            <button type="submit" class="primary" :disabled="!canSubmit">
                {{ loading ? '送出中…' : '新增出勤申請' }}
            </button>
        </div>
    </form>
</template>

<style scoped>
.fade-enter-active, .fade-leave-active {
    transition: opacity .2s ease, transform .2s ease;
}
.fade-enter-from, .fade-leave-to {
    opacity: 0;
    transform: translateY(-4px);
}

.preset-pills {
    display: flex;
    gap: 6px;
    flex-wrap: wrap;
}
.preset-pill {
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    gap: 2px;
    padding: 6px 10px;
    border-radius: 8px;
    border: 1px solid var(--line);
    background: transparent;
    cursor: pointer;
    transition: border-color .15s, background .15s;
}
.preset-pill:hover {
    border-color: rgba(37,99,235,.35);
    background: rgba(37,99,235,.05);
    filter: none;
}
.preset-name {
    font-size: 13px;
    font-weight: 600;
    color: var(--text);
}
.preset-range {
    font-size: 11px;
    color: var(--muted);
    font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace;
}

.duration-hint {
    margin: 0;
    font-size: 12px;
    color: var(--muted);
    text-align: right;
}
</style>
