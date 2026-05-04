export interface TaskPerformanceQuery {
  taskId?: number | string
  userId?: number | string
  workspaceId?: number | string
  fromDate?: string
  toDate?: string
}

export interface RecalculateMetricsRequest {
  workspaceId: number | string
}

export interface PerformanceMetric {
  completionRate: number
  averageDuration: number
  optimalHour: number
  optimalDay: string
  daysSinceLastCompletion: number
}