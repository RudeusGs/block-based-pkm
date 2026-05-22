import type { DateTimeString, Guid } from './common.model'

export interface StoredFileResponse {
  id: Guid
  uploadedByUserId: Guid
  storageProvider: string
  storageFileId: string
  fileName: string
  originalFileName: string
  contentType: string
  sizeBytes: number
  publicUrl: string
  webViewLink: string | null
  purpose: string | null
  createdDate: DateTimeString
}
