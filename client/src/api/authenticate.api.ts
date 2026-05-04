import api from './base.api';

export const AuthenticateAPI = {
  register: (payload: any) => api.post('auth/register', payload),
  login: (payload: any) => api.post('auth/login', payload),
};