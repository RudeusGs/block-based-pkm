export interface ActivityLog {
  id: number
  userId: number
  workspaceId: number
  action: string
  entityType: string
  entityId: number
  createdAt: string
}

export interface DateRangePayload {
  workspaceId: number
  startDate: string
  endDate: string
}