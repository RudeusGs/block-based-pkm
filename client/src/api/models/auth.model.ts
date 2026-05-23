import type { DateTimeString, Guid } from './common.model'

export interface LoginRequest {
  userName: string
  password: string
}

export interface RegisterRequest {
  userName: string
  email: string
  fullName: string
  password: string
  avatarUrl?: string | null
}

export interface RefreshTokenRequest {
  refreshToken: string
}

export interface LogoutRequest {
  refreshToken: string
}

export interface AuthUserResponse {
  id: Guid
  userName: string
  email: string
  fullName: string
  avatarUrl: string | null
  status: string
  isAuthenticated: boolean
  createdDate: DateTimeString
  updatedDate: DateTimeString | null
}

export interface AuthTokenResponse {
  accessToken: string
  refreshToken: string
  tokenType: string
  expiresIn: number
  refreshTokenExpiresAtUtc: DateTimeString
  user: AuthUserResponse
}
