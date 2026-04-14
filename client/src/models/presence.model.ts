export interface PresenceUserModel {
  userId: number
  userName: string
  lastActiveAt: string
}

export interface HeartbeatResponse {
  success: boolean
  message?: string
}

export interface LockResponse {
  blockId: number
}
