<script setup>
import { computed, onMounted, onUnmounted, ref } from 'vue'

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

const loginUrl = '/api/auth/login?returnUrl=' + encodeURIComponent('/')
</script>

<template>
  <div class="login-bg">
    <div class="login-card">

      <!-- Live clock -->
      <div class="clock-wrap">
        <div class="clock" role="img" aria-label="目前時間">
          <!-- Tick marks (12 dots) -->
          <span
            v-for="i in 12" :key="i"
            class="tick"
            :style="{ transform: `rotate(${(i / 12) * 360}deg) translateY(-30px)` }"
          />
          <!-- Hands -->
          <div class="hand hour"   :style="{ transform: `translateX(-50%) rotate(${hourAngle}deg)` }" />
          <div class="hand minute" :style="{ transform: `translateX(-50%) rotate(${minuteAngle}deg)` }" />
          <div class="clock-center" />
        </div>
        <time class="clock-time">{{ currentTime }}</time>
      </div>

      <!-- Title -->
      <h1 class="login-title">出勤記錄系統</h1>
      <p class="login-sub">請使用公司 Google Workspace 帳號登入</p>

      <!-- Google sign-in button -->
      <a :href="loginUrl" class="google-btn">
        <svg viewBox="0 0 24 24" width="20" height="20" aria-hidden="true" focusable="false">
          <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4"/>
          <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853"/>
          <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z" fill="#FBBC05"/>
          <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#EA4335"/>
        </svg>
        使用 Google 帳號登入
      </a>

      <p class="login-footer">僅限公司內部使用</p>
    </div>
  </div>
</template>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=Noto+Serif+TC:wght@600;700&display=swap');

/* ── Layout ─────────────────────────────────────── */
.login-bg {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  /* Subtle ruled-paper texture — nods to handwritten time records */
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

/* 12 tick dots */
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
  font-family: 'Noto Serif TC', 'Georgia', 'Times New Roman', serif;
  font-size: 24px;
  font-weight: 700;
  color: var(--text);
  margin: 0 0 8px;
  letter-spacing: 0.04em;
}

.login-sub {
  font-size: 13px;
  color: var(--muted);
  margin: 0 0 28px;
  line-height: 1.65;
}

/* ── Google button ───────────────────────────────── */
.google-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 10px;
  width: 100%;
  padding: 11px 20px;
  background: #fff;
  border: 1px solid #dadce0;
  border-radius: 10px;
  color: #3c4043;
  font-size: 14px;
  font-weight: 600;
  text-decoration: none;
  cursor: pointer;
  transition: background 0.15s ease, box-shadow 0.15s ease, transform 0.1s ease;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
}
.google-btn:hover {
  background: #f8f9fa;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.13);
  transform: translateY(-1px);
}
.google-btn:active {
  transform: translateY(0);
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
}

/* ── Footer ──────────────────────────────────────── */
.login-footer {
  margin: 22px 0 0;
  font-size: 12px;
  color: var(--muted);
  opacity: 0.6;
}
</style>
