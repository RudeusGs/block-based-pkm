import Cookies from 'js-cookie'

const TOKEN_COOKIE = 'token'
const TOKEN_EXPIRES_DAYS = 7

interface JwtPayload {
  exp?: number
  [key: string]: unknown
}

function decodeBase64Url(input: string): string {
  const base64 = input.replace(/-/g, '+').replace(/_/g, '/')
  const padded = base64.padEnd(
    base64.length + ((4 - (base64.length % 4)) % 4),
    '='
  )

  return decodeURIComponent(
    atob(padded)
      .split('')
      .map((char) => {
        return `%${char.charCodeAt(0).toString(16).padStart(2, '0')}`
      })
      .join('')
  )
}

export function decodeJwtPayload(token: string): JwtPayload | null {
  try {
    const payloadPart = token.split('.')[1]

    if (!payloadPart) return null

    return JSON.parse(decodeBase64Url(payloadPart)) as JwtPayload
  } catch {
    return null
  }
}

export function isTokenExpired(token: string): boolean {
  const payload = decodeJwtPayload(token)

  if (!payload?.exp) {
    return true
  }

  const expiresAtMs = payload.exp * 1000

  return Date.now() >= expiresAtMs
}

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

export function getRawAuthToken(): string | undefined {
  return Cookies.get(TOKEN_COOKIE)
}

export function getAuthToken(): string | undefined {
  const token = getRawAuthToken()

  if (!token) return undefined

  if (isTokenExpired(token)) {
    clearAuthToken()
    return undefined
  }

  return token
}

export function isAuthenticated(): boolean {
  return !!getAuthToken()
}