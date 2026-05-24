export function getInitial(value: string | null | undefined, fallback = 'U') {
  const normalized = value?.trim()

  if (!normalized) return fallback

  return normalized.charAt(0).toUpperCase()
}

export function statusLabel(status: string) {
  const value = status.toLowerCase()

  if (value === 'todo' || value === 'to_do') return 'Cần làm'
  if (value === 'doing' || value === 'inprogress') return 'Đang làm'
  if (value === 'done') return 'Đã xong'

  return status
}

export function priorityLabel(priority: string) {
  const value = priority.toLowerCase()

  if (value === 'high') return 'Cao'
  if (value === 'medium') return 'Vừa'
  if (value === 'low') return 'Thấp'

  return priority
}

export function taskStatusTone(status: string) {
  const value = status.toLowerCase()

  if (value === 'done') return 'done'
  if (value === 'doing' || value === 'inprogress') return 'doing'

  return 'todo'
}

export function taskPriorityTone(priority: string) {
  const value = priority.toLowerCase()

  if (value === 'high') return 'high'
  if (value === 'low') return 'low'

  return 'medium'
}

export function notificationIcon(type: string) {
  const value = type.toLowerCase()

  if (value.includes('task')) return 'bi-check2-square'
  if (value.includes('workspace')) return 'bi-people'
  if (value.includes('recommendation')) return 'bi-magic'
  if (value.includes('page')) return 'bi-file-earmark-text'

  return 'bi-bell'
}

export function formatDateTime(value: string | null | undefined) {
  if (!value) return ''

  const date = new Date(value)

  if (Number.isNaN(date.getTime())) return ''

  return new Intl.DateTimeFormat('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  }).format(date)
}
