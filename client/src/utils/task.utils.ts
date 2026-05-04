// client/src/utils/task.utils.ts

/**
 * Chuyển đổi mã Priority thành tên đầy đủ
 */
export function getPriorityLabel(priorityCode: number): string {
  if (priorityCode === 3) return 'High'
  if (priorityCode === 2) return 'Medium'
  if (priorityCode === 1) return 'Low'
  return 'Medium' // Mặc định
}

/**
 * Chuyển đổi mã Priority thành tên viết tắt
 */
export function getShortPriority(priorityCode: number): string {
  if (priorityCode === 3) return 'High'
  if (priorityCode === 2) return 'Med'
  if (priorityCode === 1) return 'Low'
  return 'Med'
}

/**
 * Chuyển đổi mã Priority thành class CSS tương ứng
 */
export function getPriorityClass(priorityCode: number): string {
  if (priorityCode === 3) return 'high'
  if (priorityCode === 2) return 'medium'
  if (priorityCode === 1) return 'low'
  return 'medium'
}

/**
 * Chuyển đổi mã Status thành tên đầy đủ
 */
export function getStatusLabel(statusCode: number): string {
  if (statusCode === 1) return 'To Do'
  if (statusCode === 2) return 'Doing'
  if (statusCode === 3) return 'Done'
  return 'To Do'
}

/**
 * Chuyển đổi mã Status thành class CSS tương ứng
 */
export function getStatusClass(statusCode: number): string {
  if (statusCode === 1) return 'todo'
  if (statusCode === 2) return 'doing'
  if (statusCode === 3) return 'done'
  return 'unknown'
}