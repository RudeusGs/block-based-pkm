import type { ApiResult } from '@/api/models/common.model'

/** Shape rejected by `base.api` response interceptor */
export interface ApiClientError {
  message?: string
  status?: number
  data?: ApiResult<unknown> & {
    title?: string
    error?: string
    errors?: Record<string, string[]>
  }
}

function firstDetail(details: string[] | null | undefined): string | null {
  const detail = details?.[0]
  return detail && detail.trim() ? detail : null
}

function firstValidationError(
  errors: Record<string, string[]> | undefined
): string | null {
  if (!errors) return null

  for (const messages of Object.values(errors)) {
    const message = messages?.[0]
    if (message?.trim()) return message
  }

  return null
}

function messageFromApiPayload(
  data: ApiClientError['data'] | undefined
): string | null {
  if (!data) return null

  const detail = firstDetail(data.error?.details)
  const validation = firstValidationError(data.errors)

  return (
    data.message ||
    detail ||
    validation ||
    data.title ||
    (typeof data.error === 'string' ? data.error : null) ||
    null
  )
}

export function getApiErrorMessage(
  error: unknown,
  fallback = 'Đã xảy ra lỗi. Vui lòng thử lại.'
): string {
  const apiError = error as ApiClientError & {
    response?: { data?: ApiClientError['data'] }
  }

  const data = apiError.data ?? apiError.response?.data
  const fromPayload = messageFromApiPayload(data)

  if (fromPayload) return fromPayload
  if (apiError.message) return apiError.message

  return fallback
}

export function getApiResultErrorMessage<T>(
  result: ApiResult<T>,
  fallback = 'Thao tác không thành công.'
): string {
  const detail = firstDetail(result.error?.details)

  if (result.message) return result.message
  if (detail) return detail

  return fallback
}
