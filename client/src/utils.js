export function fmtTime(dt) {
  if (!dt) return '\u2014'
  const d = new Date(dt)
  return new Intl.DateTimeFormat('zh-Hant', { hour: '2-digit', minute: '2-digit' }).format(d)
}

export function mins(m) {
  const sign = m > 0 ? '+' : ''
  return `${sign}${m}`
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

const categoryMap = {
  Leave: '請假',
  Trip: '出差',
  Holiday: '假日',
  Typhoon: '颱風假',
}

export function categoryLabel(cat) {
  return categoryMap[cat] || cat
}

export function categoryClass(cat) {
  const map = { Leave: 'leave', Trip: 'trip', Holiday: 'holiday', Typhoon: 'typhoon' }
  return map[cat] || ''
}
