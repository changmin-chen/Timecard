<script setup>
import {reactive, watch, ref} from 'vue'

const props = defineProps({
    date: {type: String, default: ''},
})
const emit = defineEmits(['submit'])

const categories = [
    {value: 'Leave', label: '請假 (Leave)'},
    {value: 'Overtime', label: '加班 (Overtime)'},
    {value: 'AnnualLeave', label: '特休 (Annual Leave)'},
    {value: 'Trip', label: '出差 (Trip)'},
    {value: 'Other', label: '其他 (Other)'},
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

function onSubmit() {
    const cat = form.category === 'Other' ? customCategory.value.trim() : form.category
    emit('submit', {
        date: form.date,
        category: cat,
        start: form.start,
        end: form.end,
        note: form.note.trim(),
    })
    form.start = ''
    form.end = ''
    form.note = ''
    customCategory.value = ''
}
</script>

<template>
    <h3>出勤申請（請假 / 出差 / 假日 / 颱風假…）</h3>
    <form class="form" @submit.prevent="onSubmit">
        <label>
            日期
            <input type="date" v-model="form.date" required/>
        </label>
        <label>
            類別
            <select v-model="form.category">
                <option v-for="c in categories" :key="c.value" :value="c.value">
                    {{ c.label }}
                </option>
            </select>
        </label>
        <label v-if="form.category === 'Other'">
            自訂類別
            <input type="text" v-model="customCategory" placeholder="輸入自訂類別" required/>
        </label>
        <label>
            起始時間
            <input type="time" v-model="form.start" required/>
        </label>
        <label>
            結束時間
            <input type="time" v-model="form.end" required/>
        </label>
        <label>
            註記
            <input type="text" v-model="form.note" placeholder="可留空"/>
        </label>
        <div class="actions">
            <button type="submit" class="primary">新增出勤申請</button>
        </div>
    </form>
</template>
