// src/api/controllers/auth.controller.ts

import api from '../base.api'
import type { ApiResult } from '../models/common.model'
import type {
  AuthTokenResponse,
  AuthUserResponse,
  LoginRequest,
  LogoutRequest,
  RefreshTokenRequest,
  RegisterRequest,
} from '../models/auth.model'

export const authController = {
  login(payload: LoginRequest) {
    return api.post<ApiResult<AuthTokenResponse>>('auth/login', payload)
  },

  register(payload: RegisterRequest) {
    return api.post<ApiResult<AuthUserResponse>>('auth/register', payload)
  },

  refreshToken(payload: RefreshTokenRequest) {
    return api.post<ApiResult<AuthTokenResponse>>('auth/refresh', payload)
  },

  logout(payload: LogoutRequest) {
    return api.post<ApiResult>('auth/logout', payload)
  },

  logoutAll() {
    return api.post<ApiResult>('auth/logout-all')
  },
}