export type Guid = string
export type DateTimeString = string

export interface ApiError {
  code: string
  type: string
  details: string[]
}

export interface ApiResult<T = void> {
  isSuccess: boolean
  message: string | null
  data?: T | null
  error: ApiError | null
  statusCode: number
  traceId: string | null
}

export interface PagingParams {
  pageNumber?: number
  pageSize?: number
}
