<template>
  <Teleport to="body">
    <Transition name="activity-log-panel">
      <section
        v-if="open"
        class="activity-log-layer"
        role="dialog"
        aria-modal="true"
        aria-labelledby="activity-log-title"
      >
        <header class="activity-log-topbar">
          <div class="activity-log-title-block">
            <button
              type="button"
              class="activity-log-close"
              title="Đóng activity log"
              aria-label="Đóng activity log"
              @click="emit('close')"
            >
              <span class="material-symbols-outlined">close</span>
            </button>

            <div>
              <p>Workspace activity</p>
              <h2 id="activity-log-title">
                {{ workspaceName || 'Activity log' }}
              </h2>
            </div>
          </div>

          <div class="activity-log-top-actions">
            <button
              type="button"
              class="activity-log-ghost-btn"
              :disabled="!hasActiveFilters || isLoading"
              @click="clearFilters"
            >
              <span class="material-symbols-outlined">filter_alt_off</span>
              <span>Reset</span>
            </button>

            <button
              type="button"
              class="activity-log-ghost-btn"
              :disabled="isLoading"
              @click="refresh"
            >
              <span
                class="material-symbols-outlined"
                :class="{ spinning: isLoading }"
              >
                refresh
              </span>
              <span>Refresh</span>
            </button>
          </div>
        </header>

        <div class="activity-log-body">
          <aside class="activity-log-sidebar">
            <div class="activity-log-sidebar-card">
              <span class="activity-log-sidebar-icon">
                <span class="material-symbols-outlined">history</span>
              </span>

              <strong>Activity</strong>
              <p>
                {{ loadedCountLabel }}
              </p>
            </div>

            <nav class="activity-log-filter-list" aria-label="Activity filters">
              <button
                v-for="filter in entityFilters"
                :key="filter.value"
                type="button"
                :class="{ active: entityFilter === filter.value }"
                @click="setEntityFilter(filter.value)"
              >
                <span class="material-symbols-outlined">{{ filter.icon }}</span>
                <span>{{ filter.label }}</span>
                <small v-if="entityCount(filter.value) > 0">
                  {{ entityCount(filter.value) }}
                </small>
              </button>
            </nav>
          </aside>

          <main class="activity-log-main">
            <section class="activity-log-toolbar">
              <div>
                <strong>{{ totalCountLabel }}</strong>
                <span>{{ toolbarSubtitle }}</span>
              </div>

              <div class="activity-log-controls">
                <label class="activity-log-search">
                  <span class="material-symbols-outlined">search</span>
                  <input
                    v-model="search"
                    type="search"
                    placeholder="Search activity..."
                    :disabled="isLoading"
                    @keydown.enter.prevent="refresh"
                  />
                </label>

                <select
                  v-model="datePreset"
                  class="activity-log-select compact"
                  :disabled="isLoading"
                  aria-label="Filter by time"
                  @change="refresh"
                >
                  <option
                    v-for="option in datePresetOptions"
                    :key="option.value"
                    :value="option.value"
                  >
                    {{ option.label }}
                  </option>
                </select>

                <select
                  v-model="actionFilter"
                  class="activity-log-select"
                  :disabled="isLoading"
                  aria-label="Filter by action"
                  @change="refresh"
                >
                  <option
                    v-for="option in actionOptions"
                    :key="option.value"
                    :value="option.value"
                  >
                    {{ option.label }}
                  </option>
                </select>

                <select
                  v-model="actorFilter"
                  class="activity-log-select"
                  :disabled="isLoading || !actorOptions.length"
                  aria-label="Filter by actor"
                  @change="refresh"
                >
                  <option value="all">All people</option>
                  <option
                    v-for="actor in actorOptions"
                    :key="actor.userId"
                    :value="actor.userId"
                  >
                    {{ actor.name }}
                  </option>
                </select>
              </div>
            </section>

            <div
              v-if="!workspaceId"
              class="activity-log-empty-state"
            >
              <span class="material-symbols-outlined">workspaces</span>
              <strong>Chưa chọn workspace</strong>
              <p>Chọn workspace bên sidebar để xem activity log.</p>
            </div>

            <div
              v-else-if="isLoading && !items.length"
              class="activity-log-skeleton-list"
            >
              <div
                v-for="index in 7"
                :key="index"
                class="activity-log-skeleton-row"
              >
                <span></span>
                <div>
                  <i></i>
                  <i></i>
                </div>
              </div>
            </div>

            <div
              v-else-if="error"
              class="activity-log-empty-state error"
            >
              <span class="material-symbols-outlined">error</span>
              <strong>Không thể tải activity log</strong>
              <p>{{ error }}</p>

              <button type="button" @click="refresh">
                Thử lại
              </button>
            </div>

            <div
              v-else-if="!items.length"
              class="activity-log-empty-state"
            >
              <span class="material-symbols-outlined">hourglass_empty</span>
              <strong>Chưa có hoạt động nào</strong>
              <p>Các thay đổi mới trong workspace sẽ xuất hiện ở đây.</p>
            </div>

            <div
              v-else
              class="activity-log-timeline"
            >
              <section
                v-for="group in groupedItems"
                :key="group.label"
                class="activity-log-day-group"
              >
                <h3>{{ group.label }}</h3>

                <article
                  v-for="activity in group.items"
                  :key="activity.id"
                  class="activity-log-row"
                >
                  <span class="activity-log-row-line"></span>

                  <span class="activity-log-avatar">
                    <img
                      v-if="avatarSrc(activity)"
                      :src="avatarSrc(activity) || ''"
                      :alt="actorName(activity)"
                      referrerpolicy="no-referrer"
                      @error="markAvatarFailed(activity.userId)"
                    />

                    <span v-else>{{ actorInitial(activity) }}</span>
                  </span>

                  <div class="activity-log-card">
                    <div class="activity-log-card-top">
                      <span class="activity-log-pill">
                        <span class="material-symbols-outlined">
                          {{ entityIcon(activity.entityType) }}
                        </span>
                        {{ entityLabel(activity.entityType) }}
                      </span>

                      <time :title="formatFullDate(activity.occurredAt)">
                        {{ formatRelativeTime(activity.occurredAt) }}
                      </time>
                    </div>

                    <p>
                      <strong>{{ actorName(activity) }}</strong>
                      <span>{{ actionSentence(activity) }}</span>
                    </p>

                    <small v-if="activity.description">
                      {{ cleanDescription(activity.description, activity) }}
                    </small>

                    <button
                      v-if="metadataEntries(activity).length"
                      type="button"
                      class="activity-log-detail-toggle"
                      :aria-expanded="isExpanded(activity.id)"
                      @click="toggleExpanded(activity.id)"
                    >
                      <span class="material-symbols-outlined">
                        {{ isExpanded(activity.id) ? 'expand_less' : 'expand_more' }}
                      </span>
                      Details
                    </button>

                    <dl
                      v-if="isExpanded(activity.id)"
                      class="activity-log-details"
                    >
                      <template
                        v-for="entry in metadataEntries(activity)"
                        :key="`${activity.id}:${entry.key}`"
                      >
                        <dt>{{ entry.label }}</dt>
                        <dd>{{ entry.value }}</dd>
                      </template>
                    </dl>
                  </div>
                </article>
              </section>

              <div class="activity-log-load-more-wrap">
                <button
                  v-if="hasMore"
                  type="button"
                  class="activity-log-load-more"
                  :disabled="isLoadingMore"
                  @click="loadMore"
                >
                  <span
                    v-if="isLoadingMore"
                    class="activity-log-spinner"
                  ></span>
                  <span>{{ isLoadingMore ? 'Loading...' : 'Load more' }}</span>
                </button>

                <span v-else class="activity-log-end-note">
                  End of activity
                </span>
              </div>
            </div>
          </main>
        </div>
      </section>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import type { Guid } from '@/api/models/common.model'
import type { ActivityLogResponse } from '@/api/models/activity-log.model'
import { activityLogController } from '@/api/services/activity-log.api'
import {
  getApiErrorMessage,
  getApiResultErrorMessage,
} from '@/api/utils/api-error.util'
import { normalizeImageUrl } from '@/utils/image-url.util'

const props = withDefaults(
  defineProps<{
    open: boolean
    workspaceId: Guid | null
    workspaceName?: string | null
    canReadAudit?: boolean
  }>(),
  {
    canReadAudit: false,
  }
)

const emit = defineEmits<{
  close: []
}>()

type EntityFilterValue =
  | 'all'
  | 'Workspace'
  | 'WorkspaceMember'
  | 'Page'
  | 'Block'
  | 'WorkTask'
  | 'TaskComment'
  | 'TaskAssignee'

type ActionFilterValue =
  | 'all'
  | 'Create'
  | 'Update'
  | 'Delete'
  | 'Assign'
  | 'Unassign'
  | 'Complete'
  | 'ChangePermissions'
  | 'Archive'
  | 'Restore'
  | 'Move'
  | 'Reopen'

type DatePresetValue = 'all' | '24h' | '7d' | '30d'

const pageSize = 30
const pageNumber = ref(1)
const totalCount = ref(0)
const items = ref<ActivityLogResponse[]>([])
const isLoading = ref(false)
const isLoadingMore = ref(false)
const error = ref<string | null>(null)
const entityFilter = ref<EntityFilterValue>('all')
const actionFilter = ref<ActionFilterValue>('all')
const actorFilter = ref<Guid | 'all'>('all')
const datePreset = ref<DatePresetValue>('all')
const search = ref('')
const failedAvatarUserIds = ref<Set<Guid>>(new Set())
const expandedActivityIds = ref<Set<Guid>>(new Set())

let searchDebounceId: number | null = null

const entityFilters: Array<{
  value: EntityFilterValue
  label: string
  icon: string
}> = [
  { value: 'all', label: 'All activity', icon: 'all_inclusive' },
  { value: 'Page', label: 'Pages', icon: 'description' },
  { value: 'Block', label: 'Blocks', icon: 'view_agenda' },
  { value: 'WorkTask', label: 'Tasks', icon: 'checklist' },
  { value: 'TaskComment', label: 'Comments', icon: 'mode_comment' },
  { value: 'WorkspaceMember', label: 'Members', icon: 'group' },
  { value: 'TaskAssignee', label: 'Assignees', icon: 'assignment_ind' },
  { value: 'Workspace', label: 'Workspace', icon: 'workspaces' },
]

const actionOptions: Array<{
  value: ActionFilterValue
  label: string
}> = [
  { value: 'all', label: 'All actions' },
  { value: 'Create', label: 'Created' },
  { value: 'Update', label: 'Updated' },
  { value: 'Delete', label: 'Deleted' },
  { value: 'Move', label: 'Moved' },
  { value: 'Assign', label: 'Assigned' },
  { value: 'Unassign', label: 'Unassigned' },
  { value: 'Complete', label: 'Completed' },
  { value: 'Reopen', label: 'Reopened' },
  { value: 'Restore', label: 'Restored' },
  { value: 'Archive', label: 'Archived' },
  { value: 'ChangePermissions', label: 'Permissions changed' },
]

const datePresetOptions: Array<{
  value: DatePresetValue
  label: string
}> = [
  { value: 'all', label: 'All time' },
  { value: '30d', label: 'Last 30 days' },
  { value: '7d', label: 'Last 7 days' },
  { value: '24h', label: 'Last 24 hours' },
]

const hasMore = computed(() => items.value.length < totalCount.value)

const hasActiveFilters = computed(() => {
  return Boolean(
    entityFilter.value !== 'all' ||
    actionFilter.value !== 'all' ||
    actorFilter.value !== 'all' ||
    datePreset.value !== 'all' ||
    search.value.trim()
  )
})

const totalCountLabel = computed(() => {
  if (!props.workspaceId) return 'No workspace'
  if (totalCount.value === 0) return 'No activity'

  return `${totalCount.value} event${totalCount.value > 1 ? 's' : ''}`
})

const loadedCountLabel = computed(() => {
  if (!props.workspaceId) return 'No workspace selected'
  if (!items.value.length) return 'No visible activity'

  return `Showing ${items.value.length} of ${totalCount.value} events`
})

const toolbarSubtitle = computed(() => {
  if (!props.workspaceId) return 'Select a workspace first'
  if (hasActiveFilters.value) return 'Filtered workspace events'

  return 'Latest workspace events'
})

const actorOptions = computed(() => {
  const actorMap = new Map<Guid, string>()

  for (const item of items.value) {
    if (!item.userId) continue

    actorMap.set(item.userId, actorName(item))
  }

  return Array.from(actorMap.entries())
    .map(([userId, name]) => ({ userId, name }))
    .sort((left, right) => left.name.localeCompare(right.name))
})

const entityCounts = computed(() => {
  const counts = new Map<string, number>()

  for (const item of items.value) {
    counts.set(item.entityType, (counts.get(item.entityType) ?? 0) + 1)
  }

  return counts
})

const groupedItems = computed(() => {
  const groups = new Map<string, ActivityLogResponse[]>()

  for (const item of items.value) {
    const label = dayLabel(item.occurredAt)
    const groupItems = groups.get(label) ?? []
    groupItems.push(item)
    groups.set(label, groupItems)
  }

  return Array.from(groups.entries()).map(([label, groupItems]) => ({
    label,
    items: groupItems,
  }))
})

watch(
  () => props.open,
  (open) => {
    if (open) {
      document.body.classList.add('activity-log-scroll-lock')

      if (props.canReadAudit) {
        void refresh()
      } else {
        resetList()
        error.value = 'Bạn không có quyền xem activity log của workspace này.'
      }

      window.addEventListener('keydown', handleKeydown)
      return
    }

    document.body.classList.remove('activity-log-scroll-lock')
    window.removeEventListener('keydown', handleKeydown)
  },
  { immediate: true }
)

watch(
  () => [props.workspaceId, props.canReadAudit] as const,
  () => {
    if (!props.open) return

    if (!props.canReadAudit) {
      resetList()
      error.value = 'Bạn không có quyền xem activity log của workspace này.'
      return
    }

    void refresh()
  }
)

onBeforeUnmount(() => {
  if (searchDebounceId) {
    window.clearTimeout(searchDebounceId)
  }

  document.body.classList.remove('activity-log-scroll-lock')
  window.removeEventListener('keydown', handleKeydown)
})

watch(search, () => {
  if (!props.open) return

  if (searchDebounceId) {
    window.clearTimeout(searchDebounceId)
  }

  searchDebounceId = window.setTimeout(() => {
    void refresh()
  }, 350)
})

function handleKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape') {
    emit('close')
  }
}

function setEntityFilter(value: EntityFilterValue) {
  if (entityFilter.value === value) return

  entityFilter.value = value
  void refresh()
}

function clearFilters() {
  entityFilter.value = 'all'
  actionFilter.value = 'all'
  actorFilter.value = 'all'
  datePreset.value = 'all'
  search.value = ''
  expandedActivityIds.value = new Set()
  void refresh()
}

function resetList() {
  items.value = []
  totalCount.value = 0
  pageNumber.value = 1
  expandedActivityIds.value = new Set()
}

async function refresh() {
  if (!props.workspaceId) {
    resetList()
    error.value = null
    return
  }

  if (!props.canReadAudit) {
    resetList()
    error.value = 'Bạn không có quyền xem activity log của workspace này.'
    return
  }

  isLoading.value = true
  error.value = null
  pageNumber.value = 1
  expandedActivityIds.value = new Set()

  try {
    const result = await activityLogController.listByWorkspace(props.workspaceId, {
      pageNumber: pageNumber.value,
      pageSize,
      action: actionFilter.value === 'all' ? null : actionFilter.value,
      entityType: entityFilter.value === 'all' ? null : entityFilter.value,
      userId: actorFilter.value === 'all' ? null : actorFilter.value,
      search: search.value.trim() || null,
      ...resolveDateRange(),
    })

    if (!result.isSuccess || !result.data) {
      error.value = getApiResultErrorMessage(
        result,
        'Không thể tải activity log.'
      )
      items.value = []
      totalCount.value = 0
      return
    }

    items.value = result.data.items
    totalCount.value = result.data.totalCount
  } catch (requestError) {
    error.value = getApiErrorMessage(
      requestError,
      'Không thể tải activity log.'
    )
    items.value = []
    totalCount.value = 0
  } finally {
    isLoading.value = false
  }
}

async function loadMore() {
  if (
    !props.workspaceId ||
    !props.canReadAudit ||
    isLoadingMore.value ||
    !hasMore.value
  ) {
    return
  }

  isLoadingMore.value = true
  error.value = null

  try {
    const nextPage = pageNumber.value + 1
    const result = await activityLogController.listByWorkspace(props.workspaceId, {
      pageNumber: nextPage,
      pageSize,
      action: actionFilter.value === 'all' ? null : actionFilter.value,
      entityType: entityFilter.value === 'all' ? null : entityFilter.value,
      userId: actorFilter.value === 'all' ? null : actorFilter.value,
      search: search.value.trim() || null,
      ...resolveDateRange(),
    })

    if (!result.isSuccess || !result.data) {
      error.value = getApiResultErrorMessage(
        result,
        'Không thể tải thêm activity log.'
      )
      return
    }

    pageNumber.value = nextPage
    items.value = [...items.value, ...result.data.items]
    totalCount.value = result.data.totalCount
  } catch (requestError) {
    error.value = getApiErrorMessage(
      requestError,
      'Không thể tải thêm activity log.'
    )
  } finally {
    isLoadingMore.value = false
  }
}

function actorName(activity: ActivityLogResponse) {
  return (
    activity.userFullName?.trim() ||
    activity.userName?.trim() ||
    'Someone'
  )
}

function actorInitial(activity: ActivityLogResponse) {
  return actorName(activity).charAt(0).toUpperCase()
}

function avatarSrc(activity: ActivityLogResponse) {
  if (failedAvatarUserIds.value.has(activity.userId)) return null

  return normalizeImageUrl(activity.userAvatarUrl)
}

function markAvatarFailed(userId: Guid) {
  failedAvatarUserIds.value = new Set(failedAvatarUserIds.value).add(userId)
}

function entityCount(value: EntityFilterValue) {
  if (value === 'all') return items.value.length

  return entityCounts.value.get(value) ?? 0
}

function normalize(value: string | null | undefined) {
  return value?.trim().toLowerCase() ?? ''
}

function entityLabel(entityType: string) {
  const value = normalize(entityType)

  if (value === 'workspace') return 'Workspace'
  if (value === 'workspacemember') return 'Member'
  if (value === 'page') return 'Page'
  if (value === 'block') return 'Block'
  if (value === 'worktask') return 'Task'
  if (value === 'taskcomment') return 'Comment'
  if (value === 'taskassignee') return 'Assignee'
  if (value === 'userpreference') return 'AI preference'

  return entityType || 'Item'
}

function entityIcon(entityType: string) {
  const value = normalize(entityType)

  if (value === 'workspace') return 'workspaces'
  if (value === 'workspacemember') return 'group'
  if (value === 'page') return 'description'
  if (value === 'block') return 'view_agenda'
  if (value === 'worktask') return 'checklist'
  if (value === 'taskcomment') return 'mode_comment'
  if (value === 'taskassignee') return 'assignment_ind'
  if (value === 'userpreference') return 'auto_awesome'

  return 'history'
}

function actionLabel(action: string) {
  const value = normalize(action)

  if (value === 'create') return 'created'
  if (value === 'update') return 'updated'
  if (value === 'delete') return 'deleted'
  if (value === 'archive') return 'archived'
  if (value === 'restore') return 'restored'
  if (value === 'move') return 'moved'
  if (value === 'assign') return 'assigned'
  if (value === 'unassign') return 'unassigned'
  if (value === 'complete') return 'completed'
  if (value === 'reopen') return 'reopened'
  if (value === 'changepermissions') return 'changed permissions for'

  return action || 'changed'
}

function actionSentence(activity: ActivityLogResponse) {
  return `${actionLabel(activity.action)} ${entityLabel(activity.entityType).toLowerCase()}`
}

function cleanDescription(description: string, activity: ActivityLogResponse) {
  const actor = actorName(activity)
  const value = description.trim()

  if (!value) return ''

  return value.replace(actor, '').trim() || value
}

function toggleExpanded(activityId: Guid) {
  const next = new Set(expandedActivityIds.value)

  if (next.has(activityId)) {
    next.delete(activityId)
  } else {
    next.add(activityId)
  }

  expandedActivityIds.value = next
}

function isExpanded(activityId: Guid) {
  return expandedActivityIds.value.has(activityId)
}

function metadataEntries(activity: ActivityLogResponse) {
  const metadata = parseMetadata(activity.metadataJson)

  if (!metadata || typeof metadata !== 'object' || Array.isArray(metadata)) {
    return []
  }

  return Object.entries(metadata)
    .filter(([, value]) => value !== null && value !== undefined && value !== '')
    .map(([key, value]) => ({
      key,
      label: metadataLabel(key),
      value: metadataValue(value),
    }))
}

function parseMetadata(metadataJson: string | null) {
  if (!metadataJson?.trim()) return null

  try {
    return JSON.parse(metadataJson) as unknown
  } catch {
    return null
  }
}

function metadataLabel(key: string) {
  return key
    .replace(/([A-Z])/g, ' $1')
    .replace(/Id$/i, ' ID')
    .replace(/^./, (value) => value.toUpperCase())
}

function metadataValue(value: unknown): string {
  if (Array.isArray(value)) {
    return value.length ? value.map(metadataValue).join(', ') : 'None'
  }

  if (value instanceof Date) {
    return value.toLocaleString()
  }

  if (typeof value === 'object' && value !== null) {
    return JSON.stringify(value)
  }

  if (typeof value === 'boolean') {
    return value ? 'Yes' : 'No'
  }

  return String(value)
}

function resolveDateRange() {
  if (datePreset.value === 'all') {
    return {
      fromUtc: null,
      toUtc: null,
    }
  }

  const now = new Date()
  const from = new Date(now)

  if (datePreset.value === '24h') {
    from.setHours(now.getHours() - 24)
  } else if (datePreset.value === '7d') {
    from.setDate(now.getDate() - 7)
  } else {
    from.setDate(now.getDate() - 30)
  }

  return {
    fromUtc: from.toISOString(),
    toUtc: now.toISOString(),
  }
}

function dayLabel(value: string) {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return 'Unknown date'

  const today = new Date()
  const yesterday = new Date()
  yesterday.setDate(today.getDate() - 1)

  if (isSameDate(date, today)) return 'Today'
  if (isSameDate(date, yesterday)) return 'Yesterday'

  return date.toLocaleDateString(undefined, {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
  })
}

function isSameDate(left: Date, right: Date) {
  return (
    left.getFullYear() === right.getFullYear() &&
    left.getMonth() === right.getMonth() &&
    left.getDate() === right.getDate()
  )
}

function formatRelativeTime(value: string) {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''

  const diffMs = Date.now() - date.getTime()
  const diffMinutes = Math.max(0, Math.floor(diffMs / 60000))

  if (diffMinutes < 1) return 'Just now'
  if (diffMinutes < 60) return `${diffMinutes}m ago`

  const diffHours = Math.floor(diffMinutes / 60)
  if (diffHours < 24) return `${diffHours}h ago`

  const diffDays = Math.floor(diffHours / 24)
  if (diffDays < 7) return `${diffDays}d ago`

  return date.toLocaleDateString()
}

function formatFullDate(value: string) {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''

  return date.toLocaleString()
}
</script>

<style scoped>
:global(.activity-log-scroll-lock) {
  overflow: hidden;
}

.activity-log-layer {
  position: fixed;
  inset: 0;
  z-index: 2600;
  display: flex;
  flex-direction: column;
  color: #f1f1f1;
  background: #191919;
  font-family:
    Inter, ui-sans-serif, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
}

.activity-log-topbar {
  min-height: 58px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 10px 18px;
  border-bottom: 1px solid #2b2b2b;
  background: rgba(25, 25, 25, 0.94);
  backdrop-filter: blur(18px);
}

.activity-log-title-block,
.activity-log-top-actions,
.activity-log-ghost-btn,
.activity-log-close,
.activity-log-sidebar-icon,
.activity-log-filter-list button,
.activity-log-pill,
.activity-log-avatar,
.activity-log-load-more {
  display: inline-flex;
  align-items: center;
}

.activity-log-title-block {
  min-width: 0;
  gap: 12px;
}

.activity-log-close {
  width: 32px;
  height: 32px;
  border: 0;
  border-radius: 7px;
  justify-content: center;
  color: #8a8a8a;
  background: transparent;
}

.activity-log-close:hover {
  color: #f1f1f1;
  background: #242424;
}

.activity-log-close .material-symbols-outlined {
  font-size: 19px;
}

.activity-log-title-block p,
.activity-log-title-block h2 {
  margin: 0;
}

.activity-log-title-block p {
  color: #8a8a8a;
  font-size: 12px;
  line-height: 1.2;
}

.activity-log-title-block h2 {
  max-width: min(520px, 50vw);
  overflow: hidden;
  color: #f1f1f1;
  font-size: 16px;
  font-weight: 720;
  white-space: nowrap;
  text-overflow: ellipsis;
}

.activity-log-ghost-btn {
  min-height: 32px;
  border: 0;
  border-radius: 7px;
  gap: 7px;
  padding: 6px 10px;
  color: #a3a3a3;
  background: transparent;
  font-size: 13px;
  font-weight: 650;
}

.activity-log-ghost-btn:hover:not(:disabled) {
  color: #f1f1f1;
  background: #242424;
}

.activity-log-ghost-btn:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

.activity-log-ghost-btn .material-symbols-outlined {
  font-size: 18px;
}

.activity-log-body {
  min-height: 0;
  flex: 1;
  display: grid;
  grid-template-columns: 260px minmax(0, 1fr);
}

.activity-log-sidebar {
  min-height: 0;
  padding: 18px 12px;
  border-right: 1px solid #2b2b2b;
  background: #151515;
}

.activity-log-sidebar-card {
  padding: 10px 8px 16px;
  border-bottom: 1px solid #2b2b2b;
}

.activity-log-sidebar-icon {
  width: 38px;
  height: 38px;
  justify-content: center;
  border-radius: 10px;
  color: #d4d4d4;
  background: #242424;
}

.activity-log-sidebar-icon .material-symbols-outlined {
  font-size: 22px;
}

.activity-log-sidebar-card strong {
  display: block;
  margin-top: 10px;
  color: #f1f1f1;
  font-size: 14px;
  font-weight: 720;
}

.activity-log-sidebar-card p {
  margin: 4px 0 0;
  color: #8a8a8a;
  font-size: 12px;
  line-height: 1.5;
}

.activity-log-filter-list {
  display: flex;
  flex-direction: column;
  gap: 1px;
  margin-top: 12px;
}

.activity-log-filter-list button {
  min-height: 30px;
  border: 0;
  border-radius: 6px;
  gap: 8px;
  padding: 5px 8px;
  color: #a3a3a3;
  background: transparent;
  text-align: left;
  font-size: 13px;
}

.activity-log-filter-list button small {
  margin-left: auto;
  color: #737373;
  font-size: 11px;
  font-weight: 700;
}

.activity-log-filter-list button:hover,
.activity-log-filter-list button.active {
  color: #f1f1f1;
  background: #242424;
}

.activity-log-filter-list .material-symbols-outlined {
  color: #737373;
  font-size: 18px;
}

.activity-log-main {
  min-width: 0;
  min-height: 0;
  overflow-y: auto;
  background: #191919;
}

.activity-log-toolbar {
  position: sticky;
  top: 0;
  z-index: 1;
  min-height: 58px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 14px 32px;
  border-bottom: 1px solid #242424;
  background: rgba(25, 25, 25, 0.94);
  backdrop-filter: blur(18px);
}

.activity-log-toolbar > div:first-child {
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.activity-log-toolbar strong {
  color: #f1f1f1;
  font-size: 14px;
  font-weight: 720;
}

.activity-log-toolbar span {
  color: #8a8a8a;
  font-size: 12px;
}

.activity-log-controls {
  min-width: 0;
  display: flex;
  flex-direction: row;
  align-items: center;
  justify-content: flex-end;
  flex-wrap: wrap;
  gap: 8px;
}

.activity-log-search {
  width: min(280px, 34vw);
  min-height: 32px;
  border: 1px solid #303030;
  border-radius: 7px;
  display: inline-flex;
  align-items: center;
  gap: 7px;
  padding: 0 9px;
  color: #737373;
  background: #202020;
}

.activity-log-search .material-symbols-outlined {
  font-size: 17px;
}

.activity-log-search input {
  min-width: 0;
  flex: 1;
  border: 0;
  outline: 0;
  color: #d4d4d4;
  background: transparent;
  font-size: 13px;
}

.activity-log-search input::placeholder {
  color: #666;
}

.activity-log-select {
  width: 170px;
  min-height: 32px;
  border: 1px solid #303030;
  border-radius: 7px;
  outline: 0;
  padding: 5px 8px;
  color: #d4d4d4;
  background: #202020;
  font-size: 13px;
}

.activity-log-select.compact {
  width: 132px;
}

.activity-log-select:focus {
  border-color: #525252;
}

.activity-log-timeline {
  max-width: 860px;
  margin: 0 auto;
  padding: 28px 34px 44px;
}

.activity-log-day-group + .activity-log-day-group {
  margin-top: 28px;
}

.activity-log-day-group h3 {
  margin: 0 0 12px;
  color: #737373;
  font-size: 12px;
  font-weight: 720;
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

.activity-log-row {
  position: relative;
  display: grid;
  grid-template-columns: 36px minmax(0, 1fr);
  gap: 12px;
  padding-bottom: 12px;
}

.activity-log-row-line {
  position: absolute;
  top: 37px;
  bottom: 0;
  left: 17px;
  width: 1px;
  background: #2b2b2b;
}

.activity-log-day-group .activity-log-row:last-child .activity-log-row-line {
  display: none;
}

.activity-log-avatar {
  position: relative;
  z-index: 1;
  width: 36px;
  height: 36px;
  overflow: hidden;
  justify-content: center;
  border: 1px solid #303030;
  border-radius: 999px;
  color: #f1f1f1;
  background: #242424;
  font-size: 13px;
  font-weight: 750;
}

.activity-log-avatar img {
  width: 100%;
  height: 100%;
  display: block;
  object-fit: cover;
}

.activity-log-card {
  min-width: 0;
  border: 1px solid #242424;
  border-radius: 10px;
  padding: 10px 12px 11px;
  background: #1d1d1d;
}

.activity-log-card:hover {
  border-color: #303030;
  background: #202020;
}

.activity-log-card-top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  margin-bottom: 7px;
}

.activity-log-pill {
  min-width: 0;
  max-width: 220px;
  height: 22px;
  border-radius: 999px;
  gap: 5px;
  padding: 0 8px;
  color: #a3a3a3;
  background: #262626;
  font-size: 11px;
  font-weight: 680;
}

.activity-log-pill .material-symbols-outlined {
  font-size: 14px;
}

.activity-log-card-top time {
  flex-shrink: 0;
  color: #737373;
  font-size: 12px;
}

.activity-log-card p {
  margin: 0;
  color: #d4d4d4;
  font-size: 13px;
  line-height: 1.5;
}

.activity-log-card p strong {
  margin-right: 4px;
  color: #f1f1f1;
  font-weight: 720;
}

.activity-log-card small {
  display: block;
  margin-top: 5px;
  color: #8a8a8a;
  font-size: 12px;
  line-height: 1.45;
}

.activity-log-detail-toggle {
  min-height: 26px;
  border: 0;
  border-radius: 6px;
  margin-top: 8px;
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 0 7px;
  color: #a3a3a3;
  background: #262626;
  font-size: 11.5px;
  font-weight: 680;
}

.activity-log-detail-toggle:hover {
  color: #f1f1f1;
  background: #303030;
}

.activity-log-detail-toggle .material-symbols-outlined {
  font-size: 16px;
}

.activity-log-details {
  margin: 9px 0 0;
  border-top: 1px solid #2b2b2b;
  padding-top: 9px;
  display: grid;
  grid-template-columns: minmax(92px, 0.34fr) minmax(0, 1fr);
  gap: 7px 10px;
}

.activity-log-details dt,
.activity-log-details dd {
  margin: 0;
  min-width: 0;
  font-size: 11.5px;
  line-height: 1.4;
}

.activity-log-details dt {
  color: #737373;
  font-weight: 700;
}

.activity-log-details dd {
  overflow-wrap: anywhere;
  color: #bdbdbd;
}

.activity-log-empty-state,
.activity-log-skeleton-list {
  max-width: 620px;
  margin: 78px auto 0;
  padding: 0 34px;
}

.activity-log-empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
  color: #8a8a8a;
  text-align: center;
}

.activity-log-empty-state > .material-symbols-outlined {
  width: 48px;
  height: 48px;
  border-radius: 12px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: #d4d4d4;
  background: #242424;
  font-size: 25px;
}

.activity-log-empty-state strong {
  color: #f1f1f1;
  font-size: 16px;
}

.activity-log-empty-state p {
  max-width: 420px;
  margin: 0;
  font-size: 13px;
  line-height: 1.55;
}

.activity-log-empty-state.error > .material-symbols-outlined {
  color: #f2a6a6;
}

.activity-log-empty-state button {
  min-height: 31px;
  border: 0;
  border-radius: 7px;
  margin-top: 8px;
  padding: 6px 11px;
  color: #111;
  background: #f1f1f1;
  font-size: 13px;
  font-weight: 680;
}

.activity-log-skeleton-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.activity-log-skeleton-row {
  display: grid;
  grid-template-columns: 36px minmax(0, 1fr);
  gap: 12px;
}

.activity-log-skeleton-row > span,
.activity-log-skeleton-row i {
  display: block;
  border-radius: 999px;
  background: linear-gradient(90deg, #202020, #2a2a2a, #202020);
  background-size: 180% 100%;
  animation: activity-skeleton 1.1s ease-in-out infinite;
}

.activity-log-skeleton-row > span {
  width: 36px;
  height: 36px;
}

.activity-log-skeleton-row div {
  border: 1px solid #242424;
  border-radius: 10px;
  padding: 12px;
  background: #1d1d1d;
}

.activity-log-skeleton-row i:first-child {
  width: 42%;
  height: 12px;
}

.activity-log-skeleton-row i:last-child {
  width: 72%;
  height: 10px;
  margin-top: 10px;
}

.activity-log-load-more-wrap {
  display: flex;
  justify-content: center;
  padding-top: 8px;
}

.activity-log-load-more {
  min-height: 34px;
  border: 1px solid #303030;
  border-radius: 8px;
  justify-content: center;
  gap: 8px;
  padding: 7px 13px;
  color: #d4d4d4;
  background: #202020;
  font-size: 13px;
  font-weight: 680;
}

.activity-log-load-more:hover:not(:disabled) {
  color: #f1f1f1;
  background: #262626;
}

.activity-log-load-more:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

.activity-log-end-note {
  color: #686868;
  font-size: 12px;
}

.activity-log-spinner {
  width: 13px;
  height: 13px;
  border-radius: 999px;
  border: 2px solid #3a3a3a;
  border-top-color: #f1f1f1;
  animation: activity-spin 0.75s linear infinite;
}

.spinning {
  animation: activity-spin 0.8s linear infinite;
}

.activity-log-panel-enter-active,
.activity-log-panel-leave-active {
  transition: opacity 0.16s ease;
}

.activity-log-panel-enter-from,
.activity-log-panel-leave-to {
  opacity: 0;
}

.activity-log-panel-enter-active .activity-log-body,
.activity-log-panel-leave-active .activity-log-body {
  transition: transform 0.18s ease;
}

.activity-log-panel-enter-from .activity-log-body,
.activity-log-panel-leave-to .activity-log-body {
  transform: translateY(8px);
}

@keyframes activity-spin {
  to {
    transform: rotate(360deg);
  }
}

@keyframes activity-skeleton {
  0% {
    background-position: 100% 0;
  }

  100% {
    background-position: -100% 0;
  }
}

@media (max-width: 820px) {
  .activity-log-body {
    grid-template-columns: 1fr;
  }

  .activity-log-sidebar {
    display: none;
  }

  .activity-log-toolbar,
  .activity-log-timeline {
    padding-left: 18px;
    padding-right: 18px;
  }

  .activity-log-controls {
    justify-content: flex-start;
  }

  .activity-log-search {
    width: 100%;
  }
}

@media (max-width: 560px) {
  .activity-log-topbar,
  .activity-log-toolbar {
    align-items: flex-start;
    flex-direction: column;
  }

  .activity-log-select {
    width: 100%;
  }

  .activity-log-controls {
    width: 100%;
  }

  .activity-log-search {
    width: 100%;
  }

  .activity-log-details {
    grid-template-columns: 1fr;
    gap: 3px;
  }
}

@media (prefers-reduced-motion: reduce) {
  .activity-log-panel-enter-active,
  .activity-log-panel-leave-active,
  .activity-log-panel-enter-active .activity-log-body,
  .activity-log-panel-leave-active .activity-log-body,
  .activity-log-spinner,
  .spinning,
  .activity-log-skeleton-row > span,
  .activity-log-skeleton-row i {
    transition: none;
    animation: none;
  }
}
</style>
