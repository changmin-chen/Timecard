export function fmtTime(dt) {
  if (!dt) return '\u2014'
  const d = new Date(dt)
  return new Intl.DateTimeFormat('zh-Hant', { hour: '2-digit', minute: '2-digit', hour12: false }).format(d)
}

export function fmtMins(m) {
  const sign = m > 0 ? '+' : ''
  return `${sign}${m}`
}

// 520 → "8:40" | 9 → "0:09"
export function minsToHM(m) {
  const abs = Math.abs(m)
  return `${Math.floor(abs / 60)}:${String(abs % 60).padStart(2, '0')}`
}

// signed: +55 → "+0:55" | -30 → "-0:30" | 0 → "0:00"
export function fmtMinsHM(m) {
  if (m === 0) return '0:00'
  return (m > 0 ? '+' : '-') + minsToHM(m)
}

const weekdays = ['日', '一', '二', '三', '四', '五', '六']

export function fmtDate(dateStr) {
  if (!dateStr) return '\u2014'
  const [y, m, d] = dateStr.split('-').map(Number)
  const dt = new Date(y, m - 1, d)
  const wd = weekdays[dt.getDay()]
  return `${y}/${String(m).padStart(2, '0')}/${String(d).padStart(2, '0')} (${wd})`
}

export function fmtTimeStr(timeStr) {
  if (!timeStr) return '\u2014'
  return timeStr.substring(0, 5)
}

export function durationBetween(startStr, endStr) {
  if (!startStr || !endStr) return ''
  const [sh, sm] = startStr.split(':').map(Number)
  const [eh, em] = endStr.split(':').map(Number)
  const diff = (eh * 60 + em) - (sh * 60 + sm)
  if (diff <= 0) return ''
  const h = Math.floor(diff / 60)
  const m = diff % 60
  if (h > 0 && m > 0) return `${h}h${m}m`
  if (h > 0) return `${h}h`
  return `${m}m`
}

const categoryMap = {
  Leave: '請假',
  Trip: '出差',
  Holiday: '假日',
  Typhoon: '颱風假',
  Overtime: '加班',
  AnnualLeave: '特休',
}

export function categoryLabel(cat) {
  return categoryMap[cat] || cat
}

export function categoryClass(cat) {
  const map = { Leave: 'leave', Trip: 'trip', Holiday: 'holiday', Typhoon: 'typhoon', Overtime: 'overtime', AnnualLeave: 'annual' }
  return map[cat] || ''
}
