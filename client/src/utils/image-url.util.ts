export function normalizeImageUrl(input: string | null | undefined): string | null {
  const value = input?.trim()

  if (!value) return null

  if (value.startsWith('data:image/') || value.startsWith('blob:')) {
    return value
  }

  const withProtocol = value.startsWith('//')
    ? `https:${value}`
    : /^https?:\/\//i.test(value)
      ? value
      : `https://${value}`

  try {
    const url = new URL(withProtocol)
    const host = url.hostname.toLowerCase()

    if (host === 'drive.google.com') {
      const fileIdFromPath = url.pathname.match(/\/file\/d\/([^/]+)/)?.[1]
      const fileIdFromQuery = url.searchParams.get('id')
      const fileId = fileIdFromPath || fileIdFromQuery

      if (fileId) {
        return `https://drive.google.com/thumbnail?id=${encodeURIComponent(fileId)}&sz=w1000`
      }
    }

    if (host === 'www.dropbox.com' || host === 'dropbox.com') {
      url.searchParams.delete('dl')
      url.searchParams.set('raw', '1')
      return url.toString()
    }

    if (host === 'github.com') {
      const parts = url.pathname.split('/').filter(Boolean)

      if (parts.length >= 5 && parts[2] === 'blob') {
        const [owner, repo, , branch, ...pathParts] = parts

        return `https://raw.githubusercontent.com/${owner}/${repo}/${branch}/${pathParts.join('/')}`
      }
    }

    return url.toString()
  } catch {
    return value
  }
}

export function hasImageUrl(input: string | null | undefined): boolean {
  return normalizeImageUrl(input) !== null
}