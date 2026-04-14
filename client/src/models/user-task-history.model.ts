import { StatusUserTaskHistory } from './enums/user-task-history.enum'

export interface UserTaskHistoryQuery {
    historyId?: number
    userId?: number
    taskId?: number
    workspaceId?: number
    fromDate?: string
    toDate?: string
    page?: number
    pageSize?: number
}

export interface UserTaskHistoryDateRangeRequest {
    userId?: number
    taskId?: number
    workspaceId?: number
    fromDate: string
    toDate: string
}

export interface UserTaskHistory {
    id: number

    taskId: number
    userId: number

    startedAt: string
    completedAt?: string | null
    durationMinutes: number

    status: StatusUserTaskHistory

    notes?: string | null
}