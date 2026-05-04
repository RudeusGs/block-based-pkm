import api from './base.api'
import type {
  TaskPerformanceQuery,
  RecalculateMetricsRequest,
} from '@/models/task-performance.model'

export const TaskPerformanceMetricAPI = {
  getMetric: (query: TaskPerformanceQuery) =>
    api.get(
      `task-performance-metrics/task/${query.taskId}/user/${query.userId}`
    ),

  getCompletionRate: (query: TaskPerformanceQuery) =>
    api.get(
      `task-performance-metrics/task/${query.taskId}/user/${query.userId}/completion-rate`
    ),

  getAverageDuration: (query: TaskPerformanceQuery) =>
    api.get(
      `task-performance-metrics/task/${query.taskId}/user/${query.userId}/average-duration`
    ),

  getOptimalCompletionHour: (query: TaskPerformanceQuery) =>
    api.get(
      `task-performance-metrics/task/${query.taskId}/user/${query.userId}/optimal-hour`
    ),

  getOptimalDayOfWeek: (query: TaskPerformanceQuery) =>
    api.get(
      `task-performance-metrics/task/${query.taskId}/user/${query.userId}/optimal-day`
    ),

  getCompletionTrend: (query: TaskPerformanceQuery) =>
    api.get(
      `task-performance-metrics/task/${query.taskId}/user/${query.userId}/trend`
    ),

  getDaysSinceLastCompletion: (query: TaskPerformanceQuery) =>
    api.get(
      `task-performance-metrics/task/${query.taskId}/user/${query.userId}/days-since-last-completion`
    ),

  recalculateAllMetrics: (data: RecalculateMetricsRequest) =>
    api.post(
      `task-performance-metrics/workspace/${data.workspaceId}/recalculate-all`
    ),

  getTopPerformingTasks: (
    userId: number | string,
    workspaceId: number | string,
    limit = 10
  ) =>
    api.get(
      `task-performance-metrics/user/${userId}/workspace/${workspaceId}/top-performing`,
      { limit }
    ),

  getLowPerformingTasks: (
    userId: number | string,
    workspaceId: number | string,
    limit = 10
  ) =>
    api.get(
      `task-performance-metrics/user/${userId}/workspace/${workspaceId}/low-performing`,
      { limit }
    ),
}