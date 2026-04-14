import api from './base.api'
import type {
  HeartbeatResponse,
  LockResponse,
  PresenceUserModel,
} from '@/models/presence.model'

export const PresenceAPI = {
  heartbeat: (pageId: number | string) =>
    api.post<HeartbeatResponse>(`presence/heartbeat/${pageId}`),

  getActiveUsers: (pageId: number | string) =>
    api.get<PresenceUserModel[]>(`presence/active-users/${pageId}`),

  acquireLock: (blockId: number | string) =>
    api.post<LockResponse>(`presence/lock-block/${blockId}`),

  releaseLock: (blockId: number | string) =>
    api.post<boolean>(`presence/unlock-block/${blockId}`),
}