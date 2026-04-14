import { RoomRole } from './enums/room-role.enum'

export interface WorkspaceMember {
    workspaceId: number
    userId: number
    role: RoomRole
    joinedAt: string
}

export interface AddMemberRequest {
    workspaceId: number
    userId: number
    role?: RoomRole
}

export interface UpdateMemberRoleRequest {
    workspaceId: number
    userId: number
    role: RoomRole
}

export interface RemoveMemberRequest {
    workspaceId: number
    userId: number
}

export interface GetMembersQuery {
    workspaceId: number
    page?: number
    pageSize?: number
}