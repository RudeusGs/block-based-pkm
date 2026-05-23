import type { Guid } from '@/api/models/common.model'
import type { PageResponse } from '@/api/models/page.model'

export interface PageTreeItem extends PageResponse {
  children: PageTreeItem[]
}

export interface SidebarWorkspaceLike {
  id: Guid
  name: string
  visibility?: string | null
  currentUserRole?: string | null
}



