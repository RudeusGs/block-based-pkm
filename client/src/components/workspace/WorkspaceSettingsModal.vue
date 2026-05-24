<template>
  <Teleport to="body">
    <Transition name="workspace-settings-fade">
      <div
        v-if="open"
        class="workspace-settings-layer"
      >
        <button
          class="workspace-settings-scrim"
          type="button"
          aria-label="Đóng cài đặt không gian"
          @click="close"
        ></button>

        <aside
          class="workspace-settings-panel"
          role="dialog"
          aria-modal="true"
          aria-label="Cài đặt không gian"
          @click.stop
          @keydown.esc="close"
        >
          <header class="workspace-settings-header">
            <div>
              <p>Không gian</p>
              <h2>Cài đặt</h2>
            </div>

            <button
              type="button"
              title="Đóng"
              :disabled="isUpdatingWorkspace"
              @click="close"
            >
              <span class="material-symbols-outlined">close</span>
            </button>
          </header>

          <main class="workspace-settings-body">
            <section
              v-if="!workspaceId"
              class="workspace-settings-state"
            >
              <span class="material-symbols-outlined">workspaces</span>
              <strong>Chưa chọn workspace</strong>
              <p>Chọn không gian trước rồi quay lại phần cài đặt nha.</p>
            </section>

            <section
              v-else-if="isLoadingWorkspace"
              class="workspace-settings-skeleton"
            >
              <span></span>
              <span></span>
              <span></span>
            </section>

            <form
              v-else
              class="workspace-settings-form"
              @submit.prevent="save"
            >
              <label class="workspace-settings-field">
                <span>Tên</span>
                <input
                  ref="nameInputRef"
                  v-model="form.name"
                  type="text"
                  maxlength="50"
                  placeholder="Tên không gian"
                  :disabled="isUpdatingWorkspace || !canEdit"
                />
                <small :class="{ danger: nameError }">
                  {{ nameError || 'Tên hiển thị ở thanh bên, đường dẫn và hồ sơ.' }}
                </small>
              </label>

              <label class="workspace-settings-field">
                <span>Mô tả</span>
                <textarea
                  v-model="form.description"
                  rows="5"
                  maxlength="500"
                  placeholder="Mô tả không gian"
                  :disabled="isUpdatingWorkspace || !canEdit"
                ></textarea>
                <small :class="{ danger: descriptionError }">
                  {{ descriptionError || `${form.description.length}/500 ký tự` }}
                </small>
              </label>

              <section class="workspace-settings-field">
                <span>Hiển thị</span>

                <div class="workspace-settings-visibility">
                  <button
                    v-for="option in visibilityOptions"
                    :key="option.value"
                    type="button"
                    :class="{ active: form.visibility === option.value }"
                    :disabled="isUpdatingWorkspace || !canEdit"
                    @click="form.visibility = option.value"
                  >
                    <span class="material-symbols-outlined">
                      {{ option.icon }}
                    </span>

                    <span>
                      <strong>{{ option.label }}</strong>
                      <small>{{ option.description }}</small>
                    </span>
                  </button>
                </div>

                <small>
                  Riêng tư chỉ thành viên xem được. Công khai cho người dùng đăng nhập xem ở chế độ chỉ đọc.
                </small>
              </section>

              <section class="workspace-settings-note">
                <span class="material-symbols-outlined">
                  {{ canEdit ? 'verified_user' : 'lock' }}
                </span>

                <p>
                  <strong>{{ permissionTitle }}</strong>
                  <small>{{ permissionDescription }}</small>
                </p>
              </section>

              <div
                v-if="workspaceSettingsError"
                class="workspace-settings-error"
              >
                <span class="material-symbols-outlined">error</span>
                <span>{{ workspaceSettingsError }}</span>
              </div>
            </form>
          </main>

          <footer class="workspace-settings-footer">
            <button
              type="button"
              class="settings-ghost-btn"
              :disabled="isUpdatingWorkspace"
              @click="close"
            >
              Hủy
            </button>

            <button
              type="button"
              class="settings-save-btn"
              :disabled="!canEdit || !isFormValid || isUpdatingWorkspace"
              @click="save"
            >
              <span
                v-if="isUpdatingWorkspace"
                class="settings-spinner"
              ></span>
              <span>{{ isUpdatingWorkspace ? 'Đang lưu...' : 'Lưu thay đổi' }}</span>
            </button>
          </footer>
        </aside>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import {
  computed,
  nextTick,
  reactive,
  ref,
  watch,
} from 'vue'
import { useUpdateWorkspace } from '@/modules/workspaces/composables/useUpdateWorkspace'
import type { Guid } from '@/api/models/common.model'
import type {
  WorkspaceResponse,
  WorkspaceVisibilityValue,
} from '@/api/models/workspace.model'

const props = defineProps<{
  open: boolean
  workspaceId: Guid | null
}>()

const emit = defineEmits<{
  close: []
  updated: [workspace: WorkspaceResponse]
}>()

const {
  isLoadingWorkspace,
  isUpdatingWorkspace,
  workspaceSettingsError,
  getWorkspace,
  updateWorkspace,
  clearWorkspaceSettingsError,
} = useUpdateWorkspace()

const nameInputRef = ref<HTMLInputElement | null>(null)
const loadedWorkspace = ref<WorkspaceResponse | null>(null)

const form = reactive({
  name: '',
  description: '',
  visibility: 'private' as WorkspaceVisibilityValue,
})

const visibilityOptions: Array<{
  value: WorkspaceVisibilityValue
  label: string
  description: string
  icon: string
}> = [
  {
    value: 'private',
    label: 'Riêng tư',
    description: 'Chỉ thành viên trong không gian có quyền xem.',
    icon: 'lock',
  },
  {
    value: 'public',
    label: 'Công khai',
    description: 'Ai đăng nhập cũng xem được, nhưng không tự sửa.',
    icon: 'public',
  },
]

const canEdit = computed(() => {
  const workspace = loadedWorkspace.value
  if (!workspace) return false

  const role = workspace.currentUserRole?.trim().toLowerCase() ?? ''

  return (
    workspace.canManageMembers ||
    workspace.canWrite ||
    role === 'owner' ||
    role === 'manager'
  )
})

const nameError = computed(() => {
  const value = form.name.trim()

  if (!value) return 'Tên không gian không được để trống.'
  if (value.length > 50) return 'Tên không gian không được quá 50 ký tự.'

  return ''
})

const descriptionError = computed(() => {
  if (form.description.trim().length > 500) {
    return 'Mô tả không được quá 500 ký tự.'
  }

  return ''
})

const isFormValid = computed(() => !nameError.value && !descriptionError.value)

const permissionTitle = computed(() => {
  if (!loadedWorkspace.value) return 'Không gian chưa được tải'
  return canEdit.value ? 'Bạn có quyền chỉnh không gian này' : 'Không gian chỉ đọc'
})

const permissionDescription = computed(() => {
  if (!loadedWorkspace.value) return 'Đang chờ dữ liệu từ hệ thống.'
  if (canEdit.value) return 'Bạn có thể đổi tên, mô tả và chế độ công khai/riêng tư.'

  return 'Bạn vẫn có thể xem nội dung nếu được cấp quyền, nhưng không thể chỉnh cài đặt.'
})

watch(
  () => props.open,
  async (open) => {
    document.body.classList.toggle('workspace-settings-lock', open)

    if (!open) return

    clearWorkspaceSettingsError()
    await loadWorkspace()

    await nextTick()
    nameInputRef.value?.focus()
  }
)

watch(
  () => props.workspaceId,
  () => {
    if (props.open) {
      void loadWorkspace()
    }
  }
)

function normalizeVisibility(value: string | null | undefined): WorkspaceVisibilityValue {
  return value?.trim().toLowerCase() === 'public' ? 'public' : 'private'
}

function applyWorkspace(workspace: WorkspaceResponse | null) {
  loadedWorkspace.value = workspace

  form.name = workspace?.name ?? ''
  form.description = workspace?.description ?? ''
  form.visibility = normalizeVisibility(workspace?.visibility)
}

async function loadWorkspace() {
  if (!props.workspaceId) {
    applyWorkspace(null)
    return
  }

  const workspace = await getWorkspace(props.workspaceId)
  applyWorkspace(workspace)
}

function close() {
  if (isUpdatingWorkspace.value) return

  document.body.classList.remove('workspace-settings-lock')
  emit('close')
}

async function save() {
  if (!props.workspaceId || !isFormValid.value || !canEdit.value) return

  const workspace = await updateWorkspace(props.workspaceId, {
    name: form.name.trim(),
    description: form.description.trim() || null,
    visibility: form.visibility,
  })

  if (!workspace) return

  applyWorkspace(workspace)
  emit('updated', workspace)
}
</script>

<style scoped src="./css/WorkspaceSettingsModal.css"></style>
