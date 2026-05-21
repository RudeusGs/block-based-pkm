import api from '../base.api'
import type { ApiResult, Guid } from '../models/common.model'
import type {
  AcquireBlockLeaseRequest,
  BlockLeaseResponse,
  BlockMutationResponse,
  BlockResponse,
  CreateBlockRequest,
  DeleteBlockParams,
  MoveBlockRequest,
  PageDocumentResponse,
  ReleaseBlockLeaseRequest,
  RenewBlockLeaseRequest,
  UpdateBlockRequest,
} from '../models/block.model'

export const blockController = {
  getById(blockId: Guid) {
    return api.get<ApiResult<BlockResponse>>(`blocks/${blockId}`)
  },

  getLease(blockId: Guid) {
    return api.get<ApiResult<BlockLeaseResponse>>(
      `blocks/${blockId}/edit-lease`
    )
  },

  acquireLease(blockId: Guid, payload: AcquireBlockLeaseRequest) {
    return api.post<ApiResult<BlockLeaseResponse>>(
      `blocks/${blockId}:acquire-edit-lease`,
      payload
    )
  },

  renewLease(blockId: Guid, payload: RenewBlockLeaseRequest) {
    return api.post<ApiResult<BlockLeaseResponse>>(
      `blocks/${blockId}:renew-edit-lease`,
      payload
    )
  },

  releaseLease(blockId: Guid, payload: ReleaseBlockLeaseRequest) {
    return api.post<ApiResult<BlockLeaseResponse>>(
      `blocks/${blockId}:release-edit-lease`,
      payload
    )
  },

  listByPage(pageId: Guid) {
    return api.get<ApiResult<PageDocumentResponse>>(
      `pages/${pageId}/blocks`
    )
  },

  create(pageId: Guid, payload: CreateBlockRequest) {
    return api.post<ApiResult<BlockMutationResponse>>(
      `pages/${pageId}/blocks`,
      {
        schemaVersion: 1,
        ...payload,
      }
    )
  },

  update(blockId: Guid, payload: UpdateBlockRequest) {
    return api.patch<ApiResult<BlockMutationResponse>>(
      `blocks/${blockId}`,
      payload
    )
  },

  move(blockId: Guid, payload: MoveBlockRequest) {
    return api.post<ApiResult<BlockMutationResponse>>(
      `blocks/${blockId}:move`,
      payload
    )
  },

  delete(blockId: Guid, params: DeleteBlockParams) {
    const { editorSessionId, expectedRevision, note } = params

    // Gửi editorSessionId ở cả query và header để tương thích với backend
    // đang bind theo [FromQuery] hoặc [FromHeader]. Trước đó chỉ gửi header
    // nên một số API DELETE không nhận session => delete không chạy.
    return api.delete<ApiResult<BlockMutationResponse>>(
      `blocks/${blockId}`,
      {
        expectedRevision,
        editorSessionId,
        note,
      },
      {
        headers: {
          'X-Editor-Session-Id': editorSessionId,
        },
      }
    )
  },
}
