<template>
  <Teleport to="body">
    <Transition name="task-detail-backdrop">
      <div
        v-if="open && task"
        class="task-detail-backdrop"
        @click.self="emit('close')"
      ></div>
    </Transition>

    <Transition name="task-detail-drawer">
      <aside
        v-if="open && task"
        class="task-detail-drawer"
        role="dialog"
        aria-modal="true"
      >
        <header class="task-detail-header">
          <div class="task-detail-header-main">
            <p class="task-detail-kicker mb-1">Task detail</p>
            <h2 class="task-detail-title mb-0">
              {{ task.title }}
            </h2>
          </div>

          <button
            type="button"
            class="task-detail-close"
            title="Close"
            @click="emit('close')"
          >
            <span class="material-symbols-outlined">close</span>
          </button>
        </header>

        <section class="task-detail-body">
          <div class="task-detail-status-row">
            <button
              v-for="option in statusOptions"
              :key="option.value"
              class="status-pill task-status-option d-inline-flex align-items-center gap-2 rounded-pill px-2 py-1"
              :class="[
                statusClass(option.value),
                { active: task.status === option.value },
              ]"
              type="button"
              :disabled="isMutatingTask || task.status === option.value"
              @click="emit('change-status', option.value)"
            >
              <span class="status-pill-dot"></span>
              {{ option.label }}
            </button>

            <span
              class="priority-pill d-inline-flex align-items-center gap-2 rounded-pill px-2 py-1"
              :class="priorityClass(task.priority)"
            >
              <span class="priority-pill-dot"></span>
              {{ priorityLabel(task.priority) }}
            </span>

            <span
              v-if="task.overdue"
              class="task-detail-overdue"
            >
              Overdue
            </span>
          </div>

          <div
            v-if="taskActionError"
            class="task-detail-inline-error"
          >
            <i class="bi bi-exclamation-triangle"></i>
            <span>{{ taskActionError }}</span>
          </div>

          <div class="task-detail-grid">
            <div
              v-if="canManageAssignees"
              class="task-detail-field task-detail-field--wide"
            >
              <span class="task-detail-field-label">Assignees</span>

              <div
                v-if="task.assignees.length"
                class="task-assignee-chip-list"
              >
                <span
                  v-for="assignee in task.assignees"
                  :key="assignee.userId"
                  class="task-assignee-chip"
                >
                  <img
                    :src="assignee.avatarUrl"
                    :alt="assignee.name"
                  />
                  <span>{{ assignee.name }}</span>
                  <button
                    type="button"
                    title="Unassign"
                    :disabled="isMutatingTask"
                    @click="emit('unassign-member', assignee.userId)"
                  >
                    <i class="bi bi-x"></i>
                  </button>
                </span>
              </div>

              <p
                v-else
                class="task-detail-muted-line"
              >
                Chưa assign ai.
              </p>

              <div class="task-assign-control">
                <select
                  v-model="selectedMemberToAssign"
                  :disabled="isMutatingTask || !availableMembers.length"
                >
                  <option value="">
                    {{ availableMembers.length ? 'Thêm assignee...' : 'Không còn member để thêm' }}
                  </option>
                  <option
                    v-for="member in availableMembers"
                    :key="member.userId"
                    :value="member.userId"
                  >
                    {{ member.displayName }} · {{ member.role }}
                  </option>
                </select>

                <button
                  type="button"
                  :disabled="!selectedMemberToAssign || isMutatingTask"
                  @click="assignSelectedMember"
                >
                  Add
                </button>
              </div>
            </div>

            <div class="task-detail-field">
              <span class="task-detail-field-label">Due date</span>

              <span
                class="task-detail-value"
                :class="{ 'task-detail-value-danger': task.overdue }"
              >
                {{ task.dueDateLabel }}
              </span>
            </div>

            <div class="task-detail-field">
              <span class="task-detail-field-label">Linked page</span>

              <span class="task-detail-value">
                {{ pageTitle || 'Current page' }}
              </span>
            </div>
          </div>

          <section class="task-detail-section">
            <h3 class="task-detail-section-title">
              Description
            </h3>

            <p class="task-detail-description">
              {{ task.description || 'No description yet.' }}
            </p>
          </section>

          <section class="task-detail-section task-comment-section">
            <div class="task-comment-heading">
              <h3 class="task-detail-section-title mb-0">
                Comments
              </h3>

              <span class="task-comment-count">
                {{ totalCommentCount }}
              </span>
            </div>

            <div
              v-if="commentsError"
              class="task-comment-error"
            >
              <i class="bi bi-exclamation-triangle"></i>
              <span>{{ commentsError }}</span>
            </div>

            <div
              v-else-if="isLoadingComments"
              class="task-comment-loading"
            >
              <span></span>
              <span></span>
            </div>

            <div
              v-else-if="!comments.length"
              class="task-comment-empty"
            >
              Chưa có comment. Hãy là người đầu tiên trao đổi về task này.
            </div>

            <div
              v-else
              class="task-comment-list"
            >
              <TaskCommentThread
                v-for="comment in comments"
                :key="comment.id"
                :comment="comment"
                :is-adding-comment="isAddingComment"
                @add-reply="submitReply"
              />
            </div>
          </section>
        </section>

        <footer class="task-detail-composer">
          <img
            class="task-comment-avatar"
            :src="composerAvatarUrl"
            alt="You"
          />

          <div class="task-comment-input-wrap">
            <textarea
              v-model="draftComment"
              class="task-comment-input"
              rows="1"
              placeholder="Viết comment cho task này..."
              :disabled="isAddingComment"
              @keydown.enter.exact.prevent="submitComment"
            ></textarea>

            <div class="task-comment-composer-actions">
              <button
                type="button"
                title="Attach"
                disabled
              >
                <span class="material-symbols-outlined">attach_file</span>
              </button>

              <button
                type="button"
                title="Mention"
                disabled
              >
                <span class="material-symbols-outlined">alternate_email</span>
              </button>

              <button
                type="button"
                class="send-comment-btn"
                :disabled="!draftComment.trim() || isAddingComment"
                @click="submitComment"
              >
                {{ isAddingComment ? 'Sending...' : 'Send' }}
              </button>
            </div>
          </div>
        </footer>
      </aside>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import TaskCommentThread from './TaskCommentThread.vue'
import type { Guid } from '@/api/models/common.model'
import type { WorkTaskStatusRequest } from '@/api/models/task.model'
import type {
  NormalizedTaskPriority,
  NormalizedTaskStatus,
  TaskCommentView,
  TaskMemberOption,
  WorkTaskView,
} from './task.types'

const props = withDefaults(
  defineProps<{
    open: boolean
    task: WorkTaskView | null
    comments: TaskCommentView[]
    members: TaskMemberOption[]
    canManageAssignees?: boolean
    pageTitle?: string | null
    isLoadingComments?: boolean
    commentsError?: string | null
    isAddingComment?: boolean
    isMutatingTask?: boolean
    taskActionError?: string | null
  }>(),
  {
    canManageAssignees: false,
    pageTitle: '',
    isLoadingComments: false,
    commentsError: null,
    isAddingComment: false,
    isMutatingTask: false,
    taskActionError: null,
  }
)

const emit = defineEmits<{
  close: []
  'add-comment': [content: string]
  'add-reply': [content: string, parentId: Guid]
  'assign-member': [userId: Guid]
  'unassign-member': [userId: Guid]
  'change-status': [status: WorkTaskStatusRequest]
}>()

const draftComment = ref('')
const selectedMemberToAssign = ref<Guid | ''>('')

const statusOptions: Array<{ value: WorkTaskStatusRequest; label: string }> = [
  { value: 'todo', label: 'To Do' },
  { value: 'doing', label: 'Doing' },
  { value: 'done', label: 'Done' },
]

const assignedUserIds = computed(() => {
  return new Set(props.task?.assignees.map((assignee) => assignee.userId) ?? [])
})

const availableMembers = computed(() => {
  if (!props.canManageAssignees) return []

  return props.members.filter(
    (member) => !member.isCurrentUser && !assignedUserIds.value.has(member.userId)
  )
})

const currentUser = computed(() => {
  return props.members.find((member) => member.isCurrentUser) ?? null
})

const composerAvatarUrl = computed(() => {
  return currentUser.value?.avatarUrl ?? props.task?.assignees[0]?.avatarUrl ?? ''
})

const totalCommentCount = computed(() => {
  return props.comments.reduce((total, comment) => total + countCommentTree(comment), 0)
})

watch(
  () => props.open,
  (isOpen) => {
    document.body.classList.toggle('task-detail-lock-scroll', isOpen)

    if (!isOpen) {
      draftComment.value = ''
      selectedMemberToAssign.value = ''
    }
  }
)

watch(
  () => props.task?.id,
  () => {
    draftComment.value = ''
    selectedMemberToAssign.value = ''
  }
)

function submitComment() {
  const content = draftComment.value.trim()

  if (!content || props.isAddingComment) return

  emit('add-comment', content)
  draftComment.value = ''
}

function submitReply(content: string, parentId: Guid) {
  if (!content.trim() || props.isAddingComment) return

  emit('add-reply', content.trim(), parentId)
}

function countCommentTree(comment: TaskCommentView): number {
  return 1 + comment.replies.reduce(
    (total, reply) => total + countCommentTree(reply),
    0
  )
}

function assignSelectedMember() {
  if (
    !selectedMemberToAssign.value ||
    props.isMutatingTask ||
    !props.canManageAssignees
  ) {
    return
  }

  emit('assign-member', selectedMemberToAssign.value)
  selectedMemberToAssign.value = ''
}

function statusLabel(status: NormalizedTaskStatus) {
  return {
    todo: 'To Do',
    doing: 'Doing',
    done: 'Done',
  }[status]
}

function statusClass(status: NormalizedTaskStatus | WorkTaskStatusRequest) {
  return {
    todo: 'status-todo',
    doing: 'status-doing',
    done: 'status-done',
  }[status]
}

function priorityLabel(priority: NormalizedTaskPriority) {
  return {
    low: 'Low',
    medium: 'Medium',
    high: 'High',
  }[priority]
}

function priorityClass(priority: NormalizedTaskPriority) {
  return {
    low: 'priority-low',
    medium: 'priority-medium',
    high: 'priority-high',
  }[priority]
}

function handleEscape(event: KeyboardEvent) {
  if (event.key === 'Escape' && props.open) {
    emit('close')
  }
}

onMounted(() => {
  window.addEventListener('keydown', handleEscape)
})

onBeforeUnmount(() => {
  window.removeEventListener('keydown', handleEscape)
  document.body.classList.remove('task-detail-lock-scroll')
})
</script>

<style scoped>
.task-detail-backdrop {
  position: fixed;
  inset: 0;
  z-index: 2600;
  background: rgba(0, 0, 0, 0.42);
  backdrop-filter: blur(2px);
}

.task-detail-drawer {
  position: fixed;
  top: 0;
  right: 0;
  z-index: 2601;
  width: min(520px, 100vw);
  height: 100vh;
  display: flex;
  flex-direction: column;
  color: #f1f1f1;
  background: #101010;
  border-left: 1px solid #2f2f2f;
  box-shadow: -24px 0 80px rgba(0, 0, 0, 0.48);
}

.task-detail-header {
  min-height: 92px;
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  padding: 22px 22px 18px;
  border-bottom: 1px solid #242424;
  background: #101010;
}

.task-detail-header-main {
  min-width: 0;
}

.task-detail-kicker {
  color: #a3a3a3;
  font-size: 11px;
  font-weight: 800;
  letter-spacing: 0.12em;
  text-transform: uppercase;
}

.task-detail-title {
  color: #f1f1f1;
  font-size: 22px;
  line-height: 1.22;
  font-weight: 750;
  letter-spacing: -0.03em;
}

.task-detail-close {
  width: 32px;
  height: 32px;
  border: 1px solid transparent;
  border-radius: 9px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: #a3a3a3;
  background: transparent;
  transition:
    color 0.16s ease,
    background 0.16s ease,
    border-color 0.16s ease,
    transform 0.16s ease;
}

.task-detail-close:hover {
  color: #f1f1f1;
  background: #202020;
  border-color: #2f2f2f;
  transform: rotate(4deg);
}

.task-detail-close .material-symbols-outlined {
  font-size: 18px;
}

.task-detail-body {
  flex: 1;
  overflow-y: auto;
  padding: 20px 22px 26px;
}

.task-detail-body::-webkit-scrollbar {
  width: 10px;
}

.task-detail-body::-webkit-scrollbar-track {
  background: transparent;
}

.task-detail-body::-webkit-scrollbar-thumb {
  border: 3px solid transparent;
  border-radius: 999px;
  background: #3a3a3a;
  background-clip: content-box;
}

.task-detail-status-row {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
  margin-bottom: 14px;
}

.task-status-option {
  cursor: pointer;
  opacity: 0.74;
}

.task-status-option.active,
.task-status-option:hover:not(:disabled) {
  opacity: 1;
}

.task-status-option:disabled {
  cursor: default;
}

.task-detail-overdue {
  padding: 4px 8px;
  border: 1px solid rgba(248, 113, 113, 0.28);
  border-radius: 999px;
  color: #fecaca;
  background: rgba(239, 68, 68, 0.12);
  font-size: 12px;
  font-weight: 700;
}

.task-detail-inline-error,
.task-comment-error {
  margin-bottom: 14px;
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 11px;
  border: 1px solid rgba(248, 113, 113, 0.22);
  border-radius: 10px;
  color: #fecaca;
  background: rgba(239, 68, 68, 0.09);
  font-size: 12.5px;
  line-height: 1.45;
}

.task-detail-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 14px;
  margin-bottom: 24px;
}

.task-detail-field {
  min-width: 0;
  padding: 12px;
  border: 1px solid #242424;
  border-radius: 12px;
  background: #151515;
}

.task-detail-field--wide {
  grid-column: 1 / -1;
}

.task-detail-field-label {
  display: block;
  margin-bottom: 8px;
  color: #737373;
  font-size: 10px;
  font-weight: 800;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.task-detail-value {
  overflow: hidden;
  display: block;
  color: #d4d4d4;
  font-size: 13px;
  font-weight: 600;
  white-space: nowrap;
  text-overflow: ellipsis;
}

.task-detail-value-danger {
  color: #fca5a5;
}

.task-detail-muted-line {
  margin: 0 0 10px;
  color: #737373;
  font-size: 13px;
}

.task-assignee-chip-list {
  display: flex;
  flex-wrap: wrap;
  gap: 7px;
  margin-bottom: 10px;
}

.task-assignee-chip {
  min-width: 0;
  max-width: 100%;
  display: inline-flex;
  align-items: center;
  gap: 7px;
  padding: 4px 5px 4px 4px;
  border: 1px solid #2f2f2f;
  border-radius: 999px;
  color: #e8e8e8;
  background: #1d1d1d;
  font-size: 12.5px;
  font-weight: 650;
}

.task-assignee-chip img {
  width: 22px;
  height: 22px;
  border-radius: 999px;
  object-fit: cover;
  border: 1px solid #2f2f2f;
}

.task-assignee-chip span {
  min-width: 0;
  overflow: hidden;
  white-space: nowrap;
  text-overflow: ellipsis;
}

.task-assignee-chip button {
  width: 20px;
  height: 20px;
  border: 0;
  border-radius: 999px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: #858585;
  background: transparent;
}

.task-assignee-chip button:hover:not(:disabled) {
  color: #f1f1f1;
  background: #2a2a2a;
}

.task-assign-control {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto;
  gap: 8px;
}

.task-assign-control select,
.task-assign-control button {
  min-height: 32px;
  border: 1px solid #2f2f2f;
  border-radius: 8px;
  color: #d4d4d4;
  background: #111111;
  font-size: 12.5px;
}

.task-assign-control select {
  min-width: 0;
  padding: 0 8px;
  outline: 0;
}

.task-assign-control select option {
  color: #ededed;
  background: #191919;
}

.task-assign-control button {
  padding: 0 10px;
  font-weight: 700;
}

.task-assign-control button:hover:not(:disabled) {
  color: #f1f1f1;
  background: #202020;
}

.task-assign-control button:disabled,
.task-assign-control select:disabled {
  opacity: 0.48;
  cursor: not-allowed;
}

.task-detail-section {
  padding-top: 22px;
  margin-top: 22px;
  border-top: 1px solid #242424;
}

.task-detail-section-title {
  margin: 0 0 10px;
  color: #f1f1f1;
  font-size: 13px;
  font-weight: 800;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.task-detail-description {
  margin: 0;
  color: #a3a3a3;
  font-size: 14px;
  line-height: 1.65;
  white-space: pre-wrap;
}

.task-comment-heading {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}

.task-comment-count {
  min-width: 24px;
  height: 24px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border-radius: 999px;
  color: #d4d4d4;
  background: #1f1f1f;
  border: 1px solid #2f2f2f;
  font-size: 12px;
  font-weight: 700;
}

.task-comment-empty {
  padding: 14px;
  border: 1px dashed #2f2f2f;
  border-radius: 12px;
  color: #737373;
  background: #151515;
  font-size: 13px;
  line-height: 1.5;
}

.task-comment-loading {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 4px 0;
}

.task-comment-loading span {
  height: 48px;
  border-radius: 13px;
  background: linear-gradient(
    90deg,
    rgba(255, 255, 255, 0.035),
    rgba(255, 255, 255, 0.075),
    rgba(255, 255, 255, 0.035)
  );
  background-size: 220% 100%;
  animation: task-comment-skeleton 1.2s ease-in-out infinite;
}

.task-comment-list {
  display: flex;
  flex-direction: column;
  gap: 15px;
}

.task-comment-item {
  display: flex;
  align-items: flex-start;
  gap: 10px;
}

.task-comment-main {
  min-width: 0;
  flex: 1;
}

.task-comment-avatar {
  width: 26px;
  height: 26px;
  flex-shrink: 0;
  border-radius: 999px;
  object-fit: cover;
  border: 1px solid #2f2f2f;
  background: #242424;
}

.task-comment-bubble {
  padding: 10px 12px;
  border-radius: 14px;
  border-top-left-radius: 4px;
  background: #1a1a1a;
  border: 1px solid #242424;
}

.task-comment-bubble.reply {
  background: #151515;
}

.task-comment-author-row {
  display: flex;
  align-items: baseline;
  gap: 7px;
  margin-bottom: 4px;
}

.task-comment-author-row strong {
  color: #f1f1f1;
  font-size: 13px;
}

.task-comment-author-row span {
  color: #737373;
  font-size: 11px;
}

.task-comment-bubble p {
  margin: 0;
  color: #d4d4d4;
  font-size: 13px;
  line-height: 1.5;
  white-space: pre-wrap;
}

.task-comment-actions {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 9px;
  padding: 5px 2px 0;
}

.task-comment-actions span {
  color: #737373;
  font-size: 11px;
}

.task-comment-replies {
  margin-top: 10px;
  margin-left: 8px;
  padding-left: 12px;
  border-left: 1px solid #2f2f2f;
}

.task-comment-reply {
  display: flex;
  align-items: flex-start;
  gap: 8px;
}

.task-comment-avatar.small {
  width: 22px;
  height: 22px;
}

.task-detail-composer {
  display: flex;
  align-items: flex-end;
  gap: 10px;
  padding: 14px 16px 16px;
  border-top: 1px solid #242424;
  background: #101010;
}

.task-comment-input-wrap {
  min-width: 0;
  flex: 1;
  padding: 8px;
  border: 1px solid #2f2f2f;
  border-radius: 14px;
  background: #151515;
}

.task-comment-input {
  width: 100%;
  min-height: 34px;
  max-height: 120px;
  border: 0;
  outline: 0;
  resize: vertical;
  padding: 4px 4px 0;
  color: #f1f1f1;
  background: transparent;
  font-size: 13px;
  line-height: 1.45;
}

.task-comment-input::placeholder {
  color: #737373;
}

.task-comment-input:disabled {
  opacity: 0.58;
  cursor: not-allowed;
}

.task-comment-composer-actions {
  display: flex;
  align-items: center;
  gap: 6px;
  padding-top: 6px;
}

.task-comment-composer-actions button {
  height: 28px;
  border: 0;
  border-radius: 8px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: #737373;
  background: transparent;
}

.task-comment-composer-actions button:hover:not(:disabled) {
  color: #f1f1f1;
  background: #202020;
}

.task-comment-composer-actions .material-symbols-outlined {
  font-size: 16px;
}

.send-comment-btn {
  margin-left: auto;
  padding: 0 12px;
  color: #101010 !important;
  background: #f1f1f1 !important;
  font-size: 12px;
  font-weight: 800;
}

.send-comment-btn:disabled,
.task-comment-composer-actions button:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}

.status-pill,
.priority-pill {
  font-size: 12px;
  line-height: 1.2;
  border: 1px solid transparent;
  font-weight: 650;
}

.status-pill-dot,
.priority-pill-dot {
  width: 6px;
  height: 6px;
  border-radius: 999px;
}

.status-todo {
  color: #bbf7d0;
  background: rgba(34, 197, 94, 0.12);
  border-color: rgba(74, 222, 128, 0.24);
}

.status-todo .status-pill-dot {
  background: #4ade80;
}

.status-doing {
  color: #bfdbfe;
  background: rgba(59, 130, 246, 0.13);
  border-color: rgba(96, 165, 250, 0.26);
}

.status-doing .status-pill-dot {
  background: #60a5fa;
}

.status-done {
  color: #ddd6fe;
  background: rgba(139, 92, 246, 0.12);
  border-color: rgba(167, 139, 250, 0.24);
}

.status-done .status-pill-dot {
  background: #a78bfa;
}

.priority-high {
  color: #fecaca;
  background: rgba(239, 68, 68, 0.12);
  border-color: rgba(248, 113, 113, 0.28);
}

.priority-high .priority-pill-dot {
  background: #f87171;
}

.priority-medium {
  color: #fde68a;
  background: rgba(245, 158, 11, 0.12);
  border-color: rgba(251, 191, 36, 0.28);
}

.priority-medium .priority-pill-dot {
  background: #fbbf24;
}

.priority-low {
  color: #cbd5e1;
  background: rgba(148, 163, 184, 0.11);
  border-color: rgba(148, 163, 184, 0.22);
}

.priority-low .priority-pill-dot {
  background: #94a3b8;
}

.task-detail-backdrop-enter-active,
.task-detail-backdrop-leave-active {
  transition: opacity 0.24s ease;
}

.task-detail-backdrop-enter-from,
.task-detail-backdrop-leave-to {
  opacity: 0;
}

.task-detail-drawer-enter-active,
.task-detail-drawer-leave-active {
  transition:
    transform 0.32s cubic-bezier(0.16, 1, 0.3, 1),
    opacity 0.24s ease;
}

.task-detail-drawer-enter-from,
.task-detail-drawer-leave-to {
  opacity: 0;
  transform: translateX(100%);
}

.task-detail-drawer-enter-to,
.task-detail-drawer-leave-from {
  opacity: 1;
  transform: translateX(0);
}

@keyframes task-comment-skeleton {
  0% {
    background-position: 220% 0;
  }

  100% {
    background-position: -220% 0;
  }
}

@media (max-width: 575.98px) {
  .task-detail-drawer {
    width: 100vw;
  }

  .task-detail-grid {
    grid-template-columns: 1fr;
  }

  .task-assign-control {
    grid-template-columns: 1fr;
  }
}
</style>

<style>
.task-detail-lock-scroll {
  overflow: hidden;
}
</style>
