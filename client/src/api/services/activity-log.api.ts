import api from '../base.api'
import type { ApiResult, Guid } from '../models/common.model'
import type {
  ActivityLogPagedResultResponse,
  ListWorkspaceActivityLogsParams,
} from '../models/activity-log.model'

export const activityLogController = {
  listByWorkspace(workspaceId: Guid, params?: ListWorkspaceActivityLogsParams) {
    return api.get<ApiResult<ActivityLogPagedResultResponse>>(
      `workspaces/${workspaceId}/activity-logs`,
      params
    )
  },
}
