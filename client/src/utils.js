export function fmtTime(dt) {
  if (!dt) return '\u2014'
  const d = new Date(dt)
  return new Intl.DateTimeFormat('zh-Hant', { hour: '2-digit', minute: '2-digit' }).format(d)
}

export function mins(m) {
  const sign = m > 0 ? '+' : ''
  return `${sign}${m}`
}
