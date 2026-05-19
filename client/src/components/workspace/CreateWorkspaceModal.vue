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
        aria-label="Create workspace"
        @click.stop
      >
        <div class="workspace-drawer-shell">
          <div class="workspace-drawer-topbar d-flex align-items-center justify-content-between">
            <div class="d-flex align-items-center gap-2 drawer-crumb">
              <i class="bi bi-layout-sidebar-inset"></i>
              <span>Workspace</span>
              <i class="bi bi-chevron-right drawer-crumb-separator"></i>
              <span>New</span>
            </div>

            <button
              class="ghost-icon-btn"
              type="button"
              title="Close"
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
                placeholder="Untitled workspace"
                maxlength="50"
                :disabled="isCreatingWorkspace"
              />

              <div class="title-meta d-flex align-items-center justify-content-between mt-2">
                <span class="inline-hint" :class="{ danger: nameError }">
                  {{ nameError || 'Tên workspace sẽ hiển thị ở sidebar và danh sách workspace.' }}
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
                  <i class="bi bi-text-paragraph"></i>
                  <span>Description</span>
                </div>

                <div class="property-value">
                  <textarea
                    v-model="form.description"
                    class="workspace-description-input"
                    rows="6"
                    maxlength="500"
                    placeholder="Write a short description about this workspace..."
                    :disabled="isCreatingWorkspace"
                  />

                  <div class="title-meta d-flex align-items-center justify-content-between mt-2">
                    <span
                      class="inline-hint"
                      :class="{ danger: descriptionError }"
                    >
                      {{ descriptionError || 'Mô tả giúp team hiểu workspace này dùng để làm gì.' }}
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
              <div class="preview-label">Preview in sidebar</div>

              <div class="preview-card d-flex align-items-center gap-3">
                <div class="preview-icon">
                  <i class="bi bi-grid-1x2-fill"></i>
                </div>

                <div class="min-w-0">
                  <div class="preview-title text-truncate">
                    {{ previewName }}
                  </div>

                  <div class="preview-description text-truncate">
                    {{ previewDescription }}
                  </div>
                </div>
              </div>
            </section>

            <div v-if="createWorkspaceError" class="workspace-api-error mt-4">
              <i class="bi bi-exclamation-triangle"></i>
              <span>{{ createWorkspaceError }}</span>
            </div>
          </form>

          <footer class="workspace-drawer-footer">
            <div class="keyboard-hint">
              <span>Ctrl + Enter to create</span>
              <span class="dot-separator"></span>
              <span>Esc to close</span>
            </div>

            <div class="drawer-actions d-flex align-items-center gap-2">
              <button
                class="btn btn-ghost-action"
                type="button"
                :disabled="isCreatingWorkspace"
                @click="closeModal"
              >
                Cancel
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
                  {{ isCreatingWorkspace ? 'Creating...' : 'Create workspace' }}
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
import type { WorkspaceResponse } from '@/api/models/workspace.model'

interface CreateWorkspaceForm {
  name: string
  description: string
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

const nameInputRef = ref<HTMLInputElement | null>(null)

const form = reactive<CreateWorkspaceForm>({
  name: '',
  description: '',
})

const nameError = computed(() => {
  const value = form.name.trim()

  if (!value) return 'Tên Workspace không được để trống.'
  if (value.length > 50) return 'Tên Workspace không được quá 50 ký tự.'

  return ''
})

const descriptionError = computed(() => {
  const value = form.description.trim()

  if (value.length > 500) {
    return 'Description không được quá 500 ký tự.'
  }

  return ''
})

const isFormValid = computed(() => {
  return !nameError.value && !descriptionError.value
})

const previewName = computed(() => {
  return form.name.trim() || 'Untitled workspace'
})

const previewDescription = computed(() => {
  return form.description.trim() || 'No description'
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