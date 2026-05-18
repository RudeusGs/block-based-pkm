import Cookies from 'js-cookie'

const TOKEN_COOKIE = 'token'
const TOKEN_EXPIRES_DAYS = 7

export function saveAuthToken(token: string): void {
  Cookies.set(TOKEN_COOKIE, token, {
    expires: TOKEN_EXPIRES_DAYS,
    path: '/',
    sameSite: 'Lax',
    secure: window.location.protocol === 'https:',
  })
}

export function clearAuthToken(): void {
  Cookies.remove(TOKEN_COOKIE, { path: '/' })
}
