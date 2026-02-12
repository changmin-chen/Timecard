<script setup>
import { useToast } from '../composables/useToast.js'

const { toasts, dismiss } = useToast()

const icons = {
  error: '✕',
  success: '✓',
  info: 'ℹ',
}
</script>

<template>
  <Teleport to="body">
    <div class="toast-container" aria-live="assertive">
      <TransitionGroup name="toast">
        <div
          v-for="t in toasts"
          :key="t.id"
          :class="['toast', `toast--${t.type}`]"
          role="alert"
        >
          <span class="toast__icon">{{ icons[t.type] }}</span>
          <span class="toast__msg">{{ t.message }}</span>
          <button class="toast__close" @click="dismiss(t.id)" aria-label="關閉">×</button>
          <div v-if="t.type === 'error'" class="toast__accent"></div>
        </div>
      </TransitionGroup>
    </div>
  </Teleport>
</template>

<style scoped>
.toast-container {
  position: fixed;
  top: 20px;
  right: 20px;
  z-index: 9999;
  display: flex;
  flex-direction: column;
  gap: 10px;
  pointer-events: none;
  max-width: min(420px, calc(100vw - 40px));
}

.toast {
  pointer-events: auto;
  display: flex;
  align-items: flex-start;
  gap: 10px;
  padding: 14px 16px;
  border-radius: 12px;
  background: #fff;
  border: 1px solid var(--line, #e2e5ea);
  box-shadow:
    0 8px 30px rgba(0, 0, 0, 0.08),
    0 2px 8px rgba(0, 0, 0, 0.04);
  position: relative;
  overflow: hidden;
  backdrop-filter: blur(12px);
}

/* accent bar at bottom for errors */
.toast__accent {
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  height: 3px;
  background: var(--danger, #dc2626);
  animation: toast-shrink 4.5s linear forwards;
}

@keyframes toast-shrink {
  from { transform: scaleX(1); transform-origin: left; }
  to { transform: scaleX(0); transform-origin: left; }
}

.toast__icon {
  flex-shrink: 0;
  width: 24px;
  height: 24px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 13px;
  font-weight: 700;
  margin-top: 1px;
}

.toast--error .toast__icon {
  background: rgba(220, 38, 38, 0.1);
  color: var(--danger, #dc2626);
}
.toast--success .toast__icon {
  background: rgba(22, 163, 74, 0.1);
  color: #16a34a;
}
.toast--info .toast__icon {
  background: rgba(37, 99, 235, 0.1);
  color: var(--primary, #2563eb);
}

.toast__msg {
  flex: 1;
  font-size: 14px;
  line-height: 1.5;
  color: var(--text, #1a1a2e);
  word-break: break-word;
}

.toast__close {
  flex-shrink: 0;
  background: none;
  border: none;
  padding: 0;
  width: 22px;
  height: 22px;
  border-radius: 6px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 16px;
  color: var(--muted, #6b7280);
  cursor: pointer;
  transition: background 0.15s, color 0.15s;
  margin-top: 1px;
}
.toast__close:hover {
  background: rgba(0, 0, 0, 0.06);
  color: var(--text, #1a1a2e);
}

/* TransitionGroup animations */
.toast-enter-active {
  animation: toast-in 0.32s cubic-bezier(0.16, 1, 0.3, 1);
}
.toast-leave-active {
  animation: toast-out 0.28s cubic-bezier(0.4, 0, 1, 1) forwards;
}
.toast-move {
  transition: transform 0.28s cubic-bezier(0.16, 1, 0.3, 1);
}

@keyframes toast-in {
  from {
    opacity: 0;
    transform: translateX(100%) scale(0.95);
  }
  to {
    opacity: 1;
    transform: translateX(0) scale(1);
  }
}

@keyframes toast-out {
  from {
    opacity: 1;
    transform: translateX(0) scale(1);
  }
  to {
    opacity: 0;
    transform: translateX(30%) scale(0.95);
  }
}

@media (max-width: 480px) {
  .toast-container {
    top: auto;
    bottom: 16px;
    right: 12px;
    left: 12px;
    max-width: none;
    align-items: stretch;
  }
}
</style>
