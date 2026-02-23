<script setup>
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { useAuth } from '../composables/useAuth.js'

const { login } = useAuth()

const now = ref(new Date())
let timer

onMounted(() => {
  timer = setInterval(() => { now.value = new Date() }, 1000)
})
onUnmounted(() => clearInterval(timer))

const hourAngle = computed(() => {
  const h = now.value.getHours() % 12
  const m = now.value.getMinutes()
  return (h / 12) * 360 + (m / 60) * 30
})

const minuteAngle = computed(() => {
  const m = now.value.getMinutes()
  const s = now.value.getSeconds()
  return (m / 60) * 360 + (s / 60) * 6
})

const currentTime = computed(() =>
  now.value.toLocaleTimeString('zh-TW', { hour: '2-digit', minute: '2-digit', second: '2-digit' })
)

const email = ref('')
const password = ref('')
const errorMsg = ref('')
const loading = ref(false)

async function handleLogin() {
  errorMsg.value = ''
  loading.value = true
  try {
    await login(email.value, password.value)
  } catch (err) {
    errorMsg.value = err.message || '登入失敗，請確認帳號與密碼。'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="login-bg">
    <div class="login-card">

      <!-- Live clock -->
      <div class="clock-wrap">
        <div class="clock" role="img" aria-label="目前時間">
          <span
            v-for="i in 12" :key="i"
            class="tick"
            :style="{ transform: `rotate(${(i / 12) * 360}deg) translateY(-30px)` }"
          />
          <div class="hand hour"   :style="{ transform: `translateX(-50%) rotate(${hourAngle}deg)` }" />
          <div class="hand minute" :style="{ transform: `translateX(-50%) rotate(${minuteAngle}deg)` }" />
          <div class="clock-center" />
        </div>
        <time class="clock-time">{{ currentTime }}</time>
      </div>

      <!-- Title -->
      <h1 class="login-title">出勤記錄系統</h1>
      <p class="login-sub">請輸入帳號與密碼登入</p>

      <!-- Login form -->
      <form class="login-form" @submit.prevent="handleLogin" novalidate>
        <div class="field">
          <label for="login-email">電子郵件</label>
          <input
            id="login-email"
            v-model="email"
            type="email"
            autocomplete="username"
            placeholder="user@example.com"
            required
          />
        </div>
        <div class="field">
          <label for="login-password">密碼</label>
          <input
            id="login-password"
            v-model="password"
            type="password"
            autocomplete="current-password"
            placeholder="••••••••"
            required
          />
        </div>

        <p v-if="errorMsg" class="error-msg" role="alert">{{ errorMsg }}</p>

        <button type="submit" class="submit-btn" :disabled="loading">
          <span v-if="loading" class="btn-spinner" aria-hidden="true" />
          {{ loading ? '登入中…' : '登入' }}
        </button>
      </form>

      <p class="login-footer">僅限公司內部使用</p>
    </div>
  </div>
</template>

<style scoped>
/* ── Layout ─────────────────────────────────────── */
.login-bg {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background-color: var(--bg);
  background-image: repeating-linear-gradient(
    to bottom,
    transparent,
    transparent 39px,
    rgba(0, 0, 0, 0.045) 39px,
    rgba(0, 0, 0, 0.045) 40px
  );
}

.login-card {
  background: var(--card);
  border: 1px solid var(--line);
  border-top: 3px solid var(--primary);
  border-radius: 16px;
  padding: 48px 52px 40px;
  width: 100%;
  max-width: 388px;
  text-align: center;
  box-shadow: 0 8px 48px rgba(0, 0, 0, 0.09);
}

@media (max-width: 480px) {
  .login-card { padding: 36px 28px 32px; margin: 16px; }
}

/* ── Clock ───────────────────────────────────────── */
.clock-wrap {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 10px;
  margin-bottom: 28px;
}

.clock {
  width: 76px;
  height: 76px;
  border-radius: 50%;
  border: 2px solid var(--primary);
  background: rgba(37, 99, 235, 0.04);
  position: relative;
}

.tick {
  display: block;
  width: 3px;
  height: 3px;
  border-radius: 50%;
  background: rgba(37, 99, 235, 0.35);
  position: absolute;
  top: 50%;
  left: 50%;
  margin: -1.5px 0 0 -1.5px;
  transform-origin: 0 0;
}

.hand {
  position: absolute;
  bottom: 50%;
  left: 50%;
  border-radius: 2px;
  transform-origin: bottom center;
}
.hand.hour {
  width: 2.5px;
  height: 22px;
  background: var(--text);
  margin-left: -1.25px;
}
.hand.minute {
  width: 1.5px;
  height: 31px;
  background: var(--primary);
  margin-left: -0.75px;
}
.clock-center {
  width: 5px;
  height: 5px;
  border-radius: 50%;
  background: var(--primary);
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
}

.clock-time {
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas,
    'Liberation Mono', 'Courier New', monospace;
  font-size: 12px;
  color: var(--muted);
  letter-spacing: 0.1em;
}

/* ── Typography ──────────────────────────────────── */
.login-title {
  font-size: 24px;
  font-weight: 700;
  color: var(--text);
  margin: 0 0 8px;
  letter-spacing: 0.04em;
}

.login-sub {
  font-size: 13px;
  color: var(--muted);
  margin: 0 0 24px;
  line-height: 1.65;
}

/* ── Form ────────────────────────────────────────── */
.login-form {
  display: flex;
  flex-direction: column;
  gap: 14px;
  text-align: left;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 5px;
}

.field label {
  font-size: 12px;
  font-weight: 600;
  color: var(--muted);
  letter-spacing: 0.03em;
}

.field input {
  border: 1px solid var(--line);
  border-radius: 10px;
  padding: 10px 12px;
  font-size: 14px;
  color: var(--text);
  background: var(--bg);
  outline: none;
  transition: border-color 0.15s ease, box-shadow 0.15s ease;
  width: 100%;
}

.field input:focus {
  border-color: var(--primary);
  box-shadow: 0 0 0 3px rgba(37, 99, 235, 0.12);
}

.error-msg {
  font-size: 13px;
  color: var(--danger);
  background: rgba(220, 38, 38, 0.06);
  border: 1px solid rgba(220, 38, 38, 0.2);
  border-radius: 8px;
  padding: 8px 12px;
  margin: 0;
}

.submit-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  width: 100%;
  padding: 11px 20px;
  background: var(--primary);
  border: none;
  border-radius: 10px;
  color: #fff;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  transition: opacity 0.15s ease, transform 0.1s ease;
}
.submit-btn:hover:not(:disabled) {
  opacity: 0.9;
  transform: translateY(-1px);
}
.submit-btn:active:not(:disabled) {
  transform: translateY(0);
}
.submit-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-spinner {
  width: 14px;
  height: 14px;
  border: 2px solid rgba(255, 255, 255, 0.4);
  border-top-color: #fff;
  border-radius: 50%;
  animation: spin 0.7s linear infinite;
  flex-shrink: 0;
}
@keyframes spin {
  to { transform: rotate(360deg); }
}

/* ── Footer ──────────────────────────────────────── */
.login-footer {
  margin: 22px 0 0;
  font-size: 12px;
  color: var(--muted);
  opacity: 0.6;
}
</style>
