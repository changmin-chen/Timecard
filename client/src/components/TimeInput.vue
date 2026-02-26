<script setup>
import {ref, watch} from 'vue'

const props = defineProps({
    modelValue: {type: String, default: ''},
})
const emit = defineEmits(['update:modelValue'])

const hRef = ref(null)
const mRef = ref(null)
const hDisplay = ref('')
const mDisplay = ref('')

// Sync from parent (e.g. adjustTime writes directly to form field)
watch(() => props.modelValue, (val) => {
    if (val && /^\d{2}:\d{2}$/.test(val)) {
        const [h, m] = val.split(':')
        // Skip update if display already represents this value (prevents loop during typing)
        if (h === hDisplay.value.padStart(2, '0') && m === mDisplay.value.padStart(2, '0')) return
        hDisplay.value = h
        mDisplay.value = m
    } else if (!val) {
        // Don't clear while user is mid-typing (h has value, m is empty)
        // Only clear if both are already empty (initial state) or both have values
        if (hDisplay.value && !mDisplay.value) return
        hDisplay.value = ''
        mDisplay.value = ''
    }
}, {immediate: true})

function buildValue() {
    const h = hDisplay.value
    const m = mDisplay.value
    if (h !== '' && m !== '') {
        const hN = Math.min(23, Math.max(0, parseInt(h) || 0))
        const mN = Math.min(59, Math.max(0, parseInt(m) || 0))
        return `${String(hN).padStart(2, '0')}:${String(mN).padStart(2, '0')}`
    }
    return ''
}

function onHInput(e) {
    const val = e.target.value.replace(/\D/g, '').slice(0, 2)
    hDisplay.value = val
    emit('update:modelValue', buildValue())
    // Auto-advance: typed 2 digits, or first digit is 3-9 (can't be ≥ 24 with more digits)
    if (val.length === 2 || (val.length === 1 && parseInt(val) > 2)) {
        mRef.value?.focus()
        mRef.value?.select()
    }
}

function onMInput(e) {
    const val = e.target.value.replace(/\D/g, '').slice(0, 2)
    mDisplay.value = val
    emit('update:modelValue', buildValue())
}

function onHBlur() {
    if (hDisplay.value !== '') {
        hDisplay.value = String(Math.min(23, Math.max(0, parseInt(hDisplay.value) || 0))).padStart(2, '0')
        emit('update:modelValue', buildValue())
    }
}

function onMBlur() {
    if (mDisplay.value !== '') {
        mDisplay.value = String(Math.min(59, Math.max(0, parseInt(mDisplay.value) || 0))).padStart(2, '0')
        emit('update:modelValue', buildValue())
    }
}

function onHKeydown(e) {
    if (e.key === 'ArrowUp') {
        e.preventDefault()
        const cur = parseInt(hDisplay.value) || 0
        hDisplay.value = String(Math.min(23, cur + 1)).padStart(2, '0')
        emit('update:modelValue', buildValue())
    } else if (e.key === 'ArrowDown') {
        e.preventDefault()
        const cur = parseInt(hDisplay.value) || 0
        hDisplay.value = String(Math.max(0, cur - 1)).padStart(2, '0')
        emit('update:modelValue', buildValue())
    }
}

function onMKeydown(e) {
    if (e.key === 'ArrowUp') {
        e.preventDefault()
        const cur = parseInt(mDisplay.value) || 0
        mDisplay.value = String(Math.min(59, cur + 1)).padStart(2, '0')
        emit('update:modelValue', buildValue())
    } else if (e.key === 'ArrowDown') {
        e.preventDefault()
        const cur = parseInt(mDisplay.value) || 0
        mDisplay.value = String(Math.max(0, cur - 1)).padStart(2, '0')
        emit('update:modelValue', buildValue())
    } else if (e.key === 'Backspace' && mDisplay.value === '') {
        hRef.value?.focus()
        hRef.value?.select()
    }
}
</script>

<template>
    <div class="time-input-wrap">
        <input
            ref="hRef"
            class="time-segment"
            type="text"
            inputmode="numeric"
            placeholder="00"
            maxlength="2"
            autocomplete="off"
            :value="hDisplay"
            @input="onHInput"
            @blur="onHBlur"
            @keydown="onHKeydown"
            @focus="$event.target.select()"
        />
        <span class="time-colon">:</span>
        <input
            ref="mRef"
            class="time-segment"
            type="text"
            inputmode="numeric"
            placeholder="00"
            maxlength="2"
            autocomplete="off"
            :value="mDisplay"
            @input="onMInput"
            @blur="onMBlur"
            @keydown="onMKeydown"
            @focus="$event.target.select()"
        />
        <span class="time-suffix">24H</span>
    </div>
</template>

<style scoped>
.time-input-wrap {
    display: inline-flex;
    align-items: center;
    border: 1px solid var(--line);
    border-radius: 10px;
    padding: 9px 12px;
    background: #f9fafb;
    transition: border-color .15s, box-shadow .15s;
    width: 100%;
    gap: 0;
}
.time-input-wrap:focus-within {
    border-color: var(--primary);
    box-shadow: 0 0 0 3px rgba(37,99,235,.1);
}
.time-segment {
    width: 3ch;
    border: none;
    background: transparent;
    outline: none;
    font-size: 15px;
    font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace;
    font-weight: 600;
    color: var(--text);
    text-align: center;
    padding: 0 2px;
    box-sizing: content-box;
    caret-color: var(--primary);
}
.time-segment::placeholder {
    color: #cbd5e1;
    font-weight: 400;
}
.time-colon {
    font-size: 15px;
    font-weight: 700;
    font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace;
    color: var(--muted);
    padding: 0 3px;
    user-select: none;
    line-height: 1;
}
.time-suffix {
    margin-left: auto;
    font-size: 11px;
    font-weight: 600;
    color: #cbd5e1;
    letter-spacing: .04em;
    user-select: none;
}
</style>
