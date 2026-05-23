import api from '../base.api'
import type { ApiResult, Guid } from '../models/common.model'
import type { PagePresenceResponse } from '../models/page-presence.model'

export const pagePresenceController = {
  getPresence(pageId: Guid) {
    return api.get<ApiResult<PagePresenceResponse>>(`pages/${pageId}/presence`)
  },
}
