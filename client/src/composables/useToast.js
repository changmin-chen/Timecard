import { reactive } from 'vue'

const toasts = reactive([])
let nextId = 0

const zhTwErrorByCode = {
  'workday.punch_not_found': '找不到打卡紀錄。',
  'workday.punch_too_fast': '操作太快，請稍後再試一次。',
  'workday.invalid_punch_date': '目前不支援修改打卡日期。',
  'workday.attendance_not_found': '找不到出勤申請紀錄。',
  'workday.category_required': '請選擇出勤申請類別。',
  'workday.start_before_end': '開始時間必須早於結束時間。',
  'workday.overlap': '出勤申請時段與既有紀錄重疊。',
  'workday.gap': '出勤申請時段必須與打卡時間連續，不可有空檔。'
}

const zhTwByTitle = {
  'Calendar data missing': '日曆資料缺失，請先匯入行事曆。',
  'Calendar import failed': '行事曆匯入失敗。',
  'Request failed': '請求失敗。'
}

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

  const errorCode = err.errorCode || err?.problemDetails?.errorCode || ''
  const title = err?.problemDetails?.title || ''
  const msg = err.message || 'Request failed'

  if (errorCode && zhTwErrorByCode[errorCode]) return zhTwErrorByCode[errorCode]
  if (title && zhTwByTitle[title]) return zhTwByTitle[title]

  if (!errorCode) return msg
  return `${msg} (${errorCode})`
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
