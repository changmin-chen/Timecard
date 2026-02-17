<script setup>
import {reactive, watch, ref, computed} from 'vue'
import {categoryClass} from '../utils.js'

const props = defineProps({
    date: {type: String, default: ''},
    loading: {type: Boolean, default: false},
})
const emit = defineEmits(['submit'])

const categories = [
    {value: 'Leave', label: '請假'},
    {value: 'Overtime', label: '加班'},
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

watch(() => props.date, (val) => {
    form.date = val
}, {immediate: true})

const timeError = computed(() => {
    if (form.start && form.end && form.end <= form.start) {
        return '結束時間必須晚於起始時間'
    }
    return ''
})

const canSubmit = computed(() => {
    return !props.loading && !timeError.value
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
        overtime: {bg: 'rgba(37,99,235,.08)', color: '#2563eb', border: 'rgba(37,99,235,.25)'},
        annual: {bg: 'rgba(16,185,129,.08)', color: '#059669', border: 'rgba(16,185,129,.25)'},
        trip: {bg: 'rgba(22,163,74,.08)', color: '#15803d', border: 'rgba(22,163,74,.25)'},
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
        <div class="form-row">
            <div class="field">
                <span class="field-label">起始時間</span>
                <input type="time" v-model="form.start" required/>
                <div class="time-adjust">
                    <button type="button" @click="adjustTime('start', -30)">-30</button>
                    <button type="button" @click="adjustTime('start', -5)">-5</button>
                    <button type="button" @click="adjustTime('start', 5)">+5</button>
                    <button type="button" @click="adjustTime('start', 30)">+30</button>
                </div>
            </div>
            <div class="field">
                <span class="field-label">結束時間</span>
                <input type="time" v-model="form.end" required/>
                <div class="time-adjust">
                    <button type="button" @click="adjustTime('end', -30)">-30</button>
                    <button type="button" @click="adjustTime('end', -5)">-5</button>
                    <button type="button" @click="adjustTime('end', 5)">+5</button>
                    <button type="button" @click="adjustTime('end', 30)">+30</button>
                </div>
            </div>
        </div>
        <p v-if="timeError" class="field-error">{{ timeError }}</p>
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
</style>
