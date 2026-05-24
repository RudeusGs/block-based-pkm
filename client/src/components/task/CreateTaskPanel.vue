<template>
  <Teleport to="body">
    <Transition name="task-drawer-scrim">
      <div
        v-if="modelValue"
        class="task-drawer-scrim"
        @click="handleOverlayClick"
      ></div>
    </Transition>

    <Transition name="task-drawer-slide">
      <aside
        v-if="modelValue"
        class="task-drawer-panel"
        role="dialog"
        aria-modal="true"
        aria-label="Tạo công việc"
        @click.stop
      >
        <div class="task-drawer-shell">
          <div class="task-drawer-topbar">
            <div class="task-drawer-crumb">
              <i class="bi bi-check2-square"></i>
              <span>Công việc</span>
              <i class="bi bi-chevron-right task-crumb-separator"></i>
              <span>Tạo mới</span>
            </div>

            <button
              class="task-drawer-close"
              type="button"
              title="Đóng"
              :disabled="isCreatingTask"
              @click="closePanel"
            >
              <i class="bi bi-x-lg"></i>
            </button>
          </div>

          <form
            class="task-drawer-body"
            @submit.prevent="handleSubmit"
          >
            <section class="task-drawer-head">
              <div class="task-icon-tile">
                <i class="bi bi-check2-square"></i>
              </div>

              <p class="task-eyebrow">
                Tạo công việc trong
                <span>{{ pageTitle || 'trang đang chọn' }}</span>
              </p>

              <input
                ref="titleInputRef"
                v-model="form.title"
                class="task-title-input"
                type="text"
                placeholder="Công việc chưa đặt tên"
                maxlength="100"
                :disabled="isCreatingTask"
              />

              <div class="task-title-meta">
                <span :class="{ danger: titleError }">
                  {{ titleError || 'Công việc sẽ được gắn với trang hiện tại.' }}
                </span>

                <span :class="{ danger: form.title.length >= 100 }">
                  {{ form.title.length }}/100
                </span>
              </div>
            </section>

            <section class="task-properties">
              <div class="task-property-row">
                <label
                  class="task-property-label"
                  for="new-task-priority"
                >
                  <i class="bi bi-flag"></i>
                  <span>Ưu tiên</span>
                </label>

                <div class="task-property-value">
                  <select
                    id="new-task-priority"
                    v-model="form.priority"
                    class="task-property-input"
                    :disabled="isCreatingTask"
                  >
                    <option value="low">Thấp</option>
                    <option value="medium">Vừa</option>
                    <option value="high">Cao</option>
                  </select>

                  <p class="task-property-help">
                    Mặc định là vừa, chỉnh cao/thấp tùy độ gấp.
                  </p>
                </div>
              </div>

              <div class="task-property-row">
                <label
                  class="task-property-label"
                  for="new-task-due-date"
                >
                  <i class="bi bi-calendar-event"></i>
                  <span>Hạn</span>
                </label>

                <div class="task-property-value">
                  <input
                    id="new-task-due-date"
                    v-model="form.dueDate"
                    class="task-property-input"
                    type="date"
                    :disabled="isCreatingTask"
                  />

                  <p class="task-property-help">
                    Không bắt buộc. Để trống nếu công việc chưa có hạn.
                  </p>
                </div>
              </div>

              <div
                v-if="canManageAssignees"
                class="task-property-row task-property-row--assignees"
              >
                <label class="task-property-label">
                  <i class="bi bi-people"></i>
                  <span>Người phụ trách</span>
                </label>

                <div class="task-property-value">
                  <div
                    v-if="isLoadingMembers"
                    class="task-assignee-loading"
                  >
                    Đang tải thành viên trong không gian...
                  </div>

                  <div
                    v-else-if="!members.length"
                    class="task-assignee-empty"
                  >
                    Không có thành viên khả dụng để giao việc. Bạn vẫn tạo công việc trống được.
                  </div>

                  <div
                    v-else
                    class="task-assignee-picker"
                  >
                    <button
                      v-for="member in members"
                      :key="member.userId"
                      class="task-assignee-option"
                      type="button"
                      :class="{ selected: isAssigneeSelected(member.userId) }"
                      :disabled="isCreatingTask"
                      @click="toggleAssignee(member.userId)"
                    >
                      <img
                        :src="member.avatarUrl"
                        :alt="member.displayName"
                      />

                      <span class="task-assignee-option-main">
                        <strong>{{ member.displayName }}</strong>
                        <small>{{ roleLabel(member.role) }}</small>
                      </span>

                      <i
                        v-if="isAssigneeSelected(member.userId)"
                        class="bi bi-check2"
                      ></i>
                    </button>
                  </div>

                  <p class="task-property-help">
                    {{ memberHelpText }}
                  </p>
                </div>
              </div>

              <div class="task-property-row task-property-row--textarea">
                <label
                  class="task-property-label"
                  for="new-task-description"
                >
                  <i class="bi bi-card-text"></i>
                  <span>Mô tả</span>
                </label>

                <div class="task-property-value">
                  <textarea
                    id="new-task-description"
                    v-model="form.description"
                    class="task-property-input task-property-textarea"
                    rows="5"
                    maxlength="400"
                    placeholder="Ghi nhanh bối cảnh, yêu cầu, danh sách việc cần làm..."
                    :disabled="isCreatingTask"
                  ></textarea>

                  <p class="task-property-help task-property-help-inline">
                    <span>Không bắt buộc.</span>
                    <span>{{ form.description.length }}/400</span>
                  </p>
                </div>
              </div>
            </section>

            <section class="task-preview-section">
              <div class="task-preview-label">Xem trước</div>

              <div class="task-preview-card">
                <div class="task-preview-icon">
                  <i class="bi bi-check2"></i>
                </div>

                <div class="task-preview-content">
                  <div class="task-preview-title">
                    {{ previewTitle }}
                  </div>

                  <div class="task-preview-subtitle">
                    <span>{{ priorityLabel }}</span>
                    <span class="task-preview-dot"></span>
                    <span>{{ assigneeLabel }}</span>
                  </div>
                </div>

                <div class="task-preview-badge">
                  Cần làm
                </div>
              </div>
            </section>

            <div
              v-if="createTaskError || membersError"
              class="task-drawer-error"
            >
              <i class="bi bi-exclamation-triangle"></i>
              <span>{{ createTaskError || membersError }}</span>
            </div>
          </form>

          <footer class="task-drawer-footer">
            <div class="task-keyboard-hint">
              <span>Ctrl + Enter để tạo</span>
              <span class="task-dot"></span>
              <span>Esc để đóng</span>
            </div>

            <div class="task-drawer-actions">
              <button
                class="task-btn task-btn-ghost"
                type="button"
                :disabled="isCreatingTask"
                @click="closePanel"
              >
                Hủy
              </button>

              <button
                class="task-btn task-btn-primary"
                type="button"
                :disabled="!isFormValid || isCreatingTask || !pageId"
                @click="handleSubmit"
              >
                <span
                  v-if="isCreatingTask"
                  class="task-btn-spinner"
                ></span>
                <span>{{ isCreatingTask ? 'Đang tạo...' : 'Tạo công việc' }}</span>
              </button>
            </div>
          </footer>
        </div>
      </aside>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import {
  computed,
  nextTick,
  onBeforeUnmount,
  onMounted,
  reactive,
  ref,
  watch,
} from 'vue'
import { useCreateTask } from '@/modules/task/composables/useCreateTask'
import type { Guid } from '@/api/models/common.model'
import type { WorkTaskPriorityRequest } from '@/api/models/task.model'
import type {
  CreateTaskCreatedPayload,
  TaskMemberOption,
} from './task.types'

interface CreateTaskForm {
  title: string
  description: string
  priority: WorkTaskPriorityRequest
  dueDate: string
  assigneeUserIds: Guid[]
}

const props = defineProps<{
  modelValue: boolean
  workspaceId: Guid | null
  pageId: Guid | null
  workspaceName?: string | null
  pageTitle?: string | null
  members: TaskMemberOption[]
  canManageAssignees?: boolean
  isLoadingMembers?: boolean
  membersError?: string | null
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  created: [payload: CreateTaskCreatedPayload]
  refreshMembers: []
}>()

const {
  isCreatingTask,
  createTaskError,
  createTask,
  clearCreateTaskError,
} = useCreateTask()

const titleInputRef = ref<HTMLInputElement | null>(null)

const form = reactive<CreateTaskForm>({
  title: '',
  description: '',
  priority: 'medium',
  dueDate: '',
  assigneeUserIds: [],
})

const selectedAssignees = computed(() => {
  if (!props.canManageAssignees) return []

  const selectedUserIds = new Set(form.assigneeUserIds)

  return props.members.filter((member) => selectedUserIds.has(member.userId))
})

const titleError = computed(() => {
  const value = form.title.trim()

  if (!value) return 'Tên công việc không được để trống.'
  if (value.length > 100) return 'Tên công việc không được quá 100 ký tự.'

  return ''
})

const isFormValid = computed(() => {
  return !titleError.value
})

const previewTitle = computed(() => {
  return form.title.trim() || 'Công việc chưa đặt tên'
})

const priorityLabel = computed(() => {
  return {
    low: 'Ưu tiên thấp',
    medium: 'Ưu tiên vừa',
    high: 'Ưu tiên cao',
  }[form.priority]
})

const assigneeLabel = computed(() => {
  if (!selectedAssignees.value.length) return 'Chưa giao'

  const firstAssignee = selectedAssignees.value[0]
  if (!firstAssignee) return 'Chưa giao'

  if (selectedAssignees.value.length === 1) return firstAssignee.displayName

  return `${firstAssignee.displayName} +${selectedAssignees.value.length - 1}`
})

const memberHelpText = computed(() => {
  if (!props.canManageAssignees) return 'Chỉ chủ sở hữu hoặc quản lý mới được giao công việc.'
  if (!props.workspaceId) return 'Chọn không gian làm việc trước đã.'
  if (props.isLoadingMembers) return 'Đang tải thành viên trong không gian...'
  if (props.membersError) return 'Không tải được thành viên, vẫn có thể tạo công việc không gắn người.'
  if (!props.members.length) return 'Hệ thống không cho tự giao cho chính mình, nên chỉ hiện thành viên khác bạn.'

  return 'Có thể chọn nhiều người hoặc để trống và giao sau.'
})

watch(
  () => props.modelValue,
  async (isOpen) => {
    document.body.classList.toggle('task-drawer-lock-scroll', isOpen)

    if (!isOpen) return

    clearCreateTaskError()
    emit('refreshMembers')

    await nextTick()
    titleInputRef.value?.focus()
  }
)


watch(
  () => props.canManageAssignees,
  (canManageAssignees) => {
    if (!canManageAssignees) {
      form.assigneeUserIds = []
    }
  }
)

function resetForm() {
  form.title = ''
  form.description = ''
  form.priority = 'medium'
  form.dueDate = ''
  form.assigneeUserIds = []
}

function closePanel() {
  if (isCreatingTask.value) return

  emit('update:modelValue', false)
}

function handleOverlayClick() {
  closePanel()
}

function isAssigneeSelected(userId: Guid) {
  return form.assigneeUserIds.includes(userId)
}

function toggleAssignee(userId: Guid) {
  if (isCreatingTask.value || !props.canManageAssignees) return

  if (isAssigneeSelected(userId)) {
    form.assigneeUserIds = form.assigneeUserIds.filter((id) => id !== userId)
    return
  }

  form.assigneeUserIds = [...form.assigneeUserIds, userId]
}

function roleLabel(role: string | null | undefined) {
  const normalized = role?.trim().toLowerCase()

  if (normalized === 'owner') return 'Chủ sở hữu'
  if (normalized === 'manager') return 'Quản lý'
  if (normalized === 'viewer') return 'Người xem'

  return 'Thành viên'
}

function toDueDatePayload(value: string) {
  if (!value) return null

  return new Date(`${value}T23:59:59`).toISOString()
}

async function handleSubmit() {
  if (
    !props.pageId ||
    !props.canManageAssignees ||
    !isFormValid.value ||
    isCreatingTask.value
  ) {
    return
  }

  const assigneeUserIds =
    props.canManageAssignees && form.assigneeUserIds.length
      ? [...new Set(form.assigneeUserIds)]
      : null

  const task = await createTask(props.pageId, {
    title: form.title.trim(),
    description: form.description.trim() || null,
    priority: form.priority,
    dueDate: toDueDatePayload(form.dueDate),
    assigneeUserIds,
  })

  if (!task) return

  emit('created', {
    task,
    assignees: selectedAssignees.value,
  })

  resetForm()
  emit('update:modelValue', false)
}

function handleKeydown(event: KeyboardEvent) {
  if (!props.modelValue) return

  if (event.key === 'Escape') {
    closePanel()
    return
  }

  if (event.key === 'Enter' && event.ctrlKey) {
    void handleSubmit()
  }
}

onMounted(() => {
  window.addEventListener('keydown', handleKeydown)
})

onBeforeUnmount(() => {
  window.removeEventListener('keydown', handleKeydown)
  document.body.classList.remove('task-drawer-lock-scroll')
})
</script>

<style scoped src="./css/CreateTaskPanel.css"></style>
