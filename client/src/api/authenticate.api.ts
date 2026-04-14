import api from './base.api'
import type { LoginPayload, RegisterPayload } from '@/models/auth.model'

export const AuthenticateAPI = {
  register: (payload: RegisterPayload) =>
    api.post('auth/register', payload),

  login: (payload: LoginPayload) =>
    api.post('auth/login', payload),
}