import { StatusWorkTask } from './enums/status-work-task.enum'
import { PriorityWorkTask } from './enums/priority-work-task.enum'

export interface WorkTaskQuery {
    taskId?: number
    workspaceId?: number
    status?: StatusWorkTask
    page?: number
    pageSize?: number
}

export interface CreateWorkTaskRequest {
    title: string
    workspaceId: number
    createdById: number
    pageId?: number
    priority?: PriorityWorkTask
}

export interface UpdateWorkTaskRequest {
    taskId: number
    title: string
    description?: string
    priority: PriorityWorkTask
    dueDate?: string | null
}

export interface WorkTaskActionRequest {
    taskId: number
}

export interface WorkTask {
    id: number

    title: string
    description?: string | null

    status: StatusWorkTask
    priority: PriorityWorkTask

    dueDate?: string | null

    workspaceId: number
    pageId?: number | null

    createdById: number

    createdAt: string
    updatedAt?: string | null
}