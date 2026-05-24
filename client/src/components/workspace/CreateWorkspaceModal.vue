<template>
  <Teleport to="body">
    <Transition name="workspace-drawer-scrim">
      <div
        v-if="modelValue"
        class="workspace-drawer-scrim"
        @click="handleOverlayClick"
      ></div>
    </Transition>

    <Transition name="workspace-drawer-slide">
      <aside
        v-if="modelValue"
        class="workspace-drawer-panel"
        role="dialog"
        aria-modal="true"
        aria-label="Tạo không gian làm việc"
        @click.stop
      >
        <div class="workspace-drawer-shell">
          <div class="workspace-drawer-topbar d-flex align-items-center justify-content-between">
            <div class="d-flex align-items-center gap-2 drawer-crumb">
              <i class="bi bi-layout-sidebar-inset"></i>
              <span>Không gian</span>
              <i class="bi bi-chevron-right drawer-crumb-separator"></i>
              <span>Tạo mới</span>
            </div>

            <button
              class="ghost-icon-btn"
              type="button"
              title="Đóng"
              :disabled="isCreatingWorkspace"
              @click="closeModal"
            >
              <i class="bi bi-x-lg"></i>
            </button>
          </div>

          <form class="workspace-drawer-body" @submit.prevent="handleSubmit">
            <section class="notion-page-head">
              <button class="workspace-icon-btn" type="button">
                <i class="bi bi-grid-1x2-fill"></i>
              </button>

              <input
                ref="nameInputRef"
                v-model="form.name"
                type="text"
                class="workspace-title-input"
                placeholder="Không gian chưa đặt tên"
                maxlength="50"
                :disabled="isCreatingWorkspace"
              />

              <div class="title-meta d-flex align-items-center justify-content-between mt-2">
                <span class="inline-hint" :class="{ danger: nameError }">
                  {{ nameError || 'Tên không gian sẽ hiển thị ở thanh bên và danh sách không gian.' }}
                </span>

                <span
                  class="char-counter"
                  :class="{ danger: form.name.length >= 50 }"
                >
                  {{ form.name.length }}/50
                </span>
              </div>
            </section>

            <section class="workspace-properties mt-4">
              <div class="property-row">
                <div class="property-label">
                  <i class="bi bi-shield-lock"></i>
                  <span>Hiển thị</span>
                </div>

                <div class="property-value">
                  <div class="visibility-card-list">
                    <button
                      v-for="option in visibilityOptions"
                      :key="option.value"
                      type="button"
                      class="visibility-card"
                      :class="{ active: form.visibility === option.value }"
                      :disabled="isCreatingWorkspace"
                      @click="form.visibility = option.value"
                    >
                      <span class="visibility-icon">
                        <i :class="option.icon"></i>
                      </span>

                      <span class="visibility-copy">
                        <strong>{{ option.label }}</strong>
                        <small>{{ option.description }}</small>
                      </span>

                      <i
                        class="bi visibility-check"
                        :class="
                          form.visibility === option.value
                            ? 'bi-check-circle-fill'
                            : 'bi-circle'
                        "
                      ></i>
                    </button>
                  </div>

                  <p class="inline-hint mt-2 mb-0">
                    Công khai chỉ mở quyền đọc. Quyền tạo/sửa/xóa vẫn theo vai trò trong không gian.
                  </p>
                </div>
              </div>

              <div class="property-row">
                <div class="property-label">
                  <i class="bi bi-text-paragraph"></i>
                  <span>Mô tả</span>
                </div>

                <div class="property-value">
                  <textarea
                    v-model="form.description"
                    class="workspace-description-input"
                    rows="6"
                    maxlength="500"
                    placeholder="Viết mô tả ngắn về không gian này..."
                    :disabled="isCreatingWorkspace"
                  />

                  <div class="title-meta d-flex align-items-center justify-content-between mt-2">
                    <span
                      class="inline-hint"
                      :class="{ danger: descriptionError }"
                    >
                      {{ descriptionError || 'Mô tả giúp nhóm hiểu không gian này dùng để làm gì.' }}
                    </span>

                    <span
                      class="char-counter"
                      :class="{ danger: form.description.length >= 500 }"
                    >
                      {{ form.description.length }}/500
                    </span>
                  </div>
                </div>
              </div>
            </section>

            <section class="workspace-preview mt-4">
              <div class="preview-label">Xem trước trong thanh bên</div>

              <div class="preview-card d-flex align-items-center gap-3">
                <div class="preview-icon">
                  <i class="bi bi-grid-1x2-fill"></i>
                </div>

                <div class="min-w-0 flex-grow-1">
                  <div class="preview-title text-truncate">
                    {{ previewName }}
                  </div>

                  <div class="preview-description text-truncate">
                    {{ previewDescription }}
                  </div>
                </div>

                <span class="preview-visibility">
                  {{ form.visibility === 'public' ? 'Công khai' : 'Riêng tư' }}
                </span>
              </div>
            </section>

            <div v-if="createWorkspaceError" class="workspace-api-error mt-4">
              <i class="bi bi-exclamation-triangle"></i>
              <span>{{ createWorkspaceError }}</span>
            </div>
          </form>

          <footer class="workspace-drawer-footer">
            <div class="keyboard-hint">
              <span>Ctrl + Enter để tạo</span>
              <span class="dot-separator"></span>
              <span>Esc để đóng</span>
            </div>

            <div class="drawer-actions d-flex align-items-center gap-2">
              <button
                class="btn btn-ghost-action"
                type="button"
                :disabled="isCreatingWorkspace"
                @click="closeModal"
              >
                Hủy
              </button>

              <button
                class="btn btn-create-workspace"
                type="button"
                :disabled="!isFormValid || isCreatingWorkspace"
                @click="handleSubmit"
              >
                <span
                  v-if="isCreatingWorkspace"
                  class="create-spinner"
                ></span>

                <span>
                  {{ isCreatingWorkspace ? 'Đang tạo...' : 'Tạo không gian' }}
                </span>
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
import { useCreateWorkspace } from '@/modules/workspaces/composables/useCreateWorkspace'
import type {
  WorkspaceResponse,
  WorkspaceVisibilityValue,
} from '@/api/models/workspace.model'

interface CreateWorkspaceForm {
  name: string
  description: string
  visibility: WorkspaceVisibilityValue
}

const props = defineProps<{
  modelValue: boolean
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  created: [workspace: WorkspaceResponse]
}>()

const {
  isCreatingWorkspace,
  createWorkspaceError,
  createWorkspace,
  clearCreateWorkspaceError,
} = useCreateWorkspace()

const visibilityOptions: Array<{
  value: WorkspaceVisibilityValue
  label: string
  description: string
  icon: string
}> = [
  {
    value: 'private',
    label: 'Riêng tư',
    description: 'Chỉ thành viên được xem nội dung không gian.',
    icon: 'bi bi-lock-fill',
  },
  {
    value: 'public',
    label: 'Công khai',
    description: 'Người dùng đăng nhập có thể xem, không tự có quyền sửa.',
    icon: 'bi bi-globe2',
  },
]

const nameInputRef = ref<HTMLInputElement | null>(null)

const form = reactive<CreateWorkspaceForm>({
  name: '',
  description: '',
  visibility: 'private',
})

const nameError = computed(() => {
  const value = form.name.trim()

  if (!value) return 'Tên không gian không được để trống.'
  if (value.length > 50) return 'Tên không gian không được quá 50 ký tự.'

  return ''
})

const descriptionError = computed(() => {
  const value = form.description.trim()

  if (value.length > 500) {
    return 'Mô tả không được quá 500 ký tự.'
  }

  return ''
})

const isFormValid = computed(() => {
  return !nameError.value && !descriptionError.value
})

const previewName = computed(() => {
  return form.name.trim() || 'Không gian chưa đặt tên'
})

const previewDescription = computed(() => {
  return form.description.trim() || 'Chưa có mô tả'
})

watch(
  () => props.modelValue,
  async (isOpen) => {
    document.body.classList.toggle('workspace-drawer-lock-scroll', isOpen)

    if (!isOpen) return

    clearCreateWorkspaceError()

    await nextTick()
    nameInputRef.value?.focus()
  }
)

function resetForm() {
  form.name = ''
  form.description = ''
  form.visibility = 'private'
}

function closeModal() {
  if (isCreatingWorkspace.value) return

  emit('update:modelValue', false)
}

function handleOverlayClick() {
  closeModal()
}

async function handleSubmit() {
  if (!isFormValid.value || isCreatingWorkspace.value) return

  const workspace = await createWorkspace({
    name: form.name.trim(),
    description: form.description.trim() || null,
    visibility: form.visibility,
  })

  if (!workspace) return

  emit('created', workspace)
  resetForm()
  emit('update:modelValue', false)
}

function handleKeydown(event: KeyboardEvent) {
  if (!props.modelValue) return

  if (event.key === 'Escape') {
    closeModal()
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
  document.body.classList.remove('workspace-drawer-lock-scroll')
})
</script>

<style scoped src="./css/CreateWorkspaceModal.css"></style>
