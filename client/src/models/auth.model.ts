export interface RegisterPayload {
  userName: string
  email: string
  password: string
  fullName: string
}

export interface LoginPayload {
  userName: string
  password: string
}