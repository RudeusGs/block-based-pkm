import api from '../base.api'
import type { ApiResult, Guid } from '../models/common.model'
import type { StoredFileResponse } from '../models/file.model'
import type { UserProfileResponse } from '../models/me.model'
import type { PageResponse } from '../models/page.model'

function appendImageFile(formData: FormData, file: File) {
  formData.append('file', file, file.name)
}

export const fileController = {
  uploadImage(file: File, purpose?: string | null) {
    const formData = new FormData()
    appendImageFile(formData, file)

    if (purpose?.trim()) {
      formData.append('purpose', purpose.trim())
    }

    return api.postForm<ApiResult<StoredFileResponse>>(
      'files/images',
      formData
    )
  },

  uploadMyAvatarImage(file: File) {
    const formData = new FormData()
    appendImageFile(formData, file)

    return api.postForm<ApiResult<UserProfileResponse>>(
      'me/avatar-image',
      formData
    )
  },

  uploadPageCoverImage(
    pageId: Guid,
    file: File,
    expectedRevision?: number | null
  ) {
    const formData = new FormData()
    appendImageFile(formData, file)

    if (expectedRevision !== null && expectedRevision !== undefined) {
      formData.append('expectedRevision', String(expectedRevision))
    }

    return api.postForm<ApiResult<PageResponse>>(
      `pages/${pageId}/cover-image`,
      formData
    )
  },
}
