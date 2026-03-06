<script setup>
import { ref } from 'vue'
import { timecardApi } from '../timecardApi.js'
import { useAuth } from '../composables/useAuth.js'

const { user } = useAuth()

const currentPassword = ref('')
const newPassword = ref('')
const confirmPassword = ref('')
const errorMsg = ref('')
const loading = ref(false)
const success = ref(false)

async function handleSubmit() {
  errorMsg.value = ''

  if (newPassword.value !== confirmPassword.value) {
    errorMsg.value = '兩次輸入的新密碼不一致。'
    return
  }
  if (newPassword.value.length < 8) {
    errorMsg.value = '新密碼至少需要 8 個字元。'
    return
  }

  loading.value = true
  try {
    await timecardApi.changePassword(currentPassword.value, newPassword.value)
    success.value = true
    setTimeout(() => {
      user.value = { ...user.value, mustChangePassword: false }
    }, 1500)
  } catch (err) {
    errorMsg.value = err.message || '密碼變更失敗，請稍後再試。'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="cpw-bg">
    <Transition name="fade" mode="out-in">
      <div v-if="success" class="cpw-card" key="success">
        <div class="cpw-icon success-icon" aria-hidden="true">
          <svg viewBox="0 0 24 24" width="32" height="32" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M20 6L9 17l-5-5"/>
          </svg>
        </div>
        <h1 class="cpw-title">密碼已成功變更！</h1>
        <p class="cpw-sub">正在進入系統…</p>
      </div>

      <div v-else class="cpw-card" key="form">
        <div class="cpw-icon" aria-hidden="true">
          <svg viewBox="0 0 24 24" width="32" height="32" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
            <rect x="3" y="11" width="18" height="11" rx="2" ry="2"/>
            <path d="M7 11V7a5 5 0 0 1 10 0v4"/>
          </svg>
        </div>

        <h1 class="cpw-title">請設定新密碼</h1>
        <p class="cpw-sub">這是您第一次登入。請設定一個專屬的個人密碼後再繼續。</p>

        <form class="cpw-form" @submit.prevent="handleSubmit" novalidate>
          <div class="field">
            <label for="cpw-current">目前的臨時密碼</label>
            <input
              id="cpw-current"
              v-model="currentPassword"
              type="password"
              autocomplete="current-password"
              placeholder="輸入您的臨時密碼"
              required
            />
          </div>
          <div class="field">
            <label for="cpw-new">新密碼</label>
            <input
              id="cpw-new"
              v-model="newPassword"
              type="password"
              autocomplete="new-password"
              placeholder="至少 8 個字元"
              required
            />
          </div>
          <div class="field">
            <label for="cpw-confirm">確認新密碼</label>
            <input
              id="cpw-confirm"
              v-model="confirmPassword"
              type="password"
              autocomplete="new-password"
              placeholder="再次輸入新密碼"
              required
            />
          </div>

          <p v-if="errorMsg" class="error-msg" role="alert">{{ errorMsg }}</p>

          <button type="submit" class="submit-btn" :disabled="loading">
            <span v-if="loading" class="btn-spinner" aria-hidden="true" />
            {{ loading ? '儲存中…' : '儲存新密碼' }}
          </button>
        </form>
      </div>
    </Transition>
  </div>
</template>

<style scoped>
.cpw-bg {
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

.cpw-card {
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
  .cpw-card { padding: 36px 28px 32px; margin: 16px; }
}

.cpw-icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 64px;
  height: 64px;
  border-radius: 50%;
  background: rgba(37, 99, 235, 0.07);
  color: var(--primary);
  margin-bottom: 20px;
}

.cpw-title {
  font-size: 22px;
  font-weight: 700;
  color: var(--text);
  margin: 0 0 8px;
  letter-spacing: 0.02em;
}

.cpw-sub {
  font-size: 13px;
  color: var(--muted);
  margin: 0 0 28px;
  line-height: 1.7;
}

.cpw-form {
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

.success-icon {
  background: rgba(22, 163, 74, 0.08);
  color: #16a34a;
}

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease, transform 0.2s ease;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
  transform: translateY(6px);
}
</style>
