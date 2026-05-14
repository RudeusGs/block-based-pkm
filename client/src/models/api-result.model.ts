export interface ApiError {
  code: string
  type: string
  details: string[]
}

export interface ApiResultBase {
  isSuccess: boolean
  message?: string | null
  error?: ApiError | null
  statusCode: number
  traceId?: string | null
}

export interface ApiResult<T = unknown> extends ApiResultBase {
  data?: T | null
}

export type ApiEmptyResult = ApiResultBase