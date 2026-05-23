const DEFAULT_AUTH_REDIRECT = '/app'

export function getSafeAuthRedirect(
  redirect: unknown,
  fallback = DEFAULT_AUTH_REDIRECT
): string {
  if (Array.isArray(redirect)) {
    return getSafeAuthRedirect(redirect[0], fallback)
  }

  if (typeof redirect !== 'string') {
    return fallback
  }

  const trimmed = redirect.trim()

  if (!trimmed || !trimmed.startsWith('/') || trimmed.startsWith('//')) {
    return fallback
  }

  return trimmed
}
