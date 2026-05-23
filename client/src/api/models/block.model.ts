import type { DateTimeString, Guid } from './common.model'

export type BlockTypeRequest =
  | 'paragraph'
  | 'heading_1'
  | 'heading_2'
  | 'heading_3'
  | 'todo'
  | 'bulleted_list'
  | 'numbered_list'
  | 'quote'
  | 'code'
  | 'image'
  | string

export interface BlockResponse {
  id: Guid
  pageId: Guid
  parentBlockId: Guid | null
  type: string
  textContent: string | null
  propsJson: string | null
  schemaVersion: number
  orderKey: string
  createdBy: Guid
  lastModifiedBy: Guid | null
  createdDate: DateTimeString
  updatedDate: DateTimeString | null
}

export interface BlockLeaseResponse {
  blockId: Guid
  pageId: Guid
  granted: boolean
  status: string
  holderUserId: Guid | null
  holderDisplayName: string | null
  expiresAtUtc: DateTimeString | null
  isHeldByCurrentUser: boolean
}

export interface BlockMutationResponse {
  pageId: Guid
  blockId: Guid | null
  appliedRevision: number
  block: BlockResponse | null
}

export interface PageDocumentResponse {
  pageId: Guid
  currentRevision: number
  blocks: BlockResponse[]
}

export interface AcquireBlockLeaseRequest {
  editorSessionId: string
  holderDisplayName?: string | null
}

export interface RenewBlockLeaseRequest {
  editorSessionId: string
}

export interface ReleaseBlockLeaseRequest {
  editorSessionId: string
}

export interface CreateBlockRequest {
  expectedRevision: number
  type: BlockTypeRequest
  textContent?: string | null
  propsJson?: string | null
  parentBlockId?: Guid | null
  previousBlockId?: Guid | null
  nextBlockId?: Guid | null
  schemaVersion?: number
}

export interface UpdateBlockRequest {
  expectedRevision: number
  editorSessionId: string
  textContent?: string | null
  propsJson?: string | null
  type?: BlockTypeRequest | null
}

export interface MoveBlockRequest {
  expectedRevision: number
  editorSessionId: string
  newParentBlockId?: Guid | null
  previousBlockId?: Guid | null
  nextBlockId?: Guid | null
}

export interface DeleteBlockParams {
  expectedRevision: number
  note?: string | null
  editorSessionId: string
}
