import { reactive } from 'vue'

const toasts = reactive([])
let nextId = 0

/**
 * @param {'error'|'success'|'info'} type
 * @param {string} message
 * @param {number} duration - auto-dismiss in ms (0 = manual only)
 */
function show(type, message, duration = 4500) {
  const id = nextId++
  toasts.push({ id, type, message })

  if (duration > 0) {
    setTimeout(() => dismiss(id), duration)
  }
}

function dismiss(id) {
  const idx = toasts.findIndex(t => t.id === id)
  if (idx !== -1) toasts.splice(idx, 1)
}

function formatErrorMessage(err) {
  if (!err || typeof err !== 'object') return String(err || 'Request failed')

  const msg = err.message || 'Request failed'
  const code = err.code || err?.problemDetails?.extensions?.code || ''
  if (!code) return msg
  return `${msg} (${code})`
}

export function useToast() {
  return {
    toasts,
    show,
    dismiss,
    error: (msg, duration) => show('error', msg, duration),
    errorFrom: (err, duration) => show('error', formatErrorMessage(err), duration),
    success: (msg, duration) => show('success', msg, duration ?? 3000),
    info: (msg, duration) => show('info', msg, duration ?? 3500),
  }
}
