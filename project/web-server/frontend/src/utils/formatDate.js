export function formatDate(value) {
  if (!value) return '—';
  try {
    const dt = new Date(value);
    return new Intl.DateTimeFormat('en-GB', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      timeZone: 'Asia/Jerusalem'
    }).format(dt);
  } catch {
    return value;
  }
}

export function formatTime(value) {
  if (!value) return '—';
  try {
    const dt = new Date(value);
    return new Intl.DateTimeFormat('en-GB', {
      hour: '2-digit',
      minute: '2-digit',
      hour12: false,
      timeZone: 'Asia/Jerusalem'
    }).format(dt);
  } catch {
    return value;
  }
}

export function formatDateTime(value) {
  if (!value) return '—';
  try {
    const d = formatDate(value);
    const t = formatTime(value);
    // If either returned raw value (fallback), try to combine safely
    if (!d || !t) return d || t || value;
    return `${d} ${t}`;
  } catch {
    return value;
  }
}

export function getIsraelDateStamp(value = new Date()) {
  const parts = new Intl.DateTimeFormat('en-GB', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    timeZone: 'Asia/Jerusalem'
  }).formatToParts(value instanceof Date ? value : new Date(value));

  const values = Object.fromEntries(parts.map((part) => [part.type, part.value]));
  return `${values.year}-${values.month}-${values.day}`;
}

export default { formatDate, formatTime, formatDateTime, getIsraelDateStamp };
