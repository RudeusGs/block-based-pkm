<template>
  <Teleport to="body">
    <Transition name="page-drawer-scrim">
      <div
        v-if="modelValue"
        class="page-drawer-scrim"
        @click="handleOverlayClick"
      ></div>
    </Transition>

    <Transition name="page-drawer-slide">
      <aside
        v-if="modelValue"
        class="page-drawer-panel"
        role="dialog"
        aria-modal="true"
        aria-label="Create page"
        @click.stop
      >
        <div class="page-drawer-shell">
          <div class="page-drawer-topbar">
            <div class="page-drawer-crumb">
              <i class="bi bi-file-earmark-text"></i>
              <span>Page</span>
              <i class="bi bi-chevron-right page-crumb-separator"></i>
              <span>New</span>
            </div>

            <button
              class="page-drawer-close"
              type="button"
              title="Close"
              :disabled="isCreatingPage"
              @click="closeModal"
            >
              <i class="bi bi-x-lg"></i>
            </button>
          </div>

          <form class="page-drawer-body" @submit.prevent="handleSubmit">
            <section class="page-drawer-head">
              <button class="page-icon-tile" type="button">
                <span>{{ previewIcon }}</span>
              </button>

              <p class="page-eyebrow">
                New page in
                <span>{{ workspaceName || 'Workspace' }}</span>
              </p>

              <input
                ref="titleInputRef"
                v-model="form.title"
                class="page-title-input"
                type="text"
                placeholder="Untitled page"
                maxlength="80"
                :disabled="isCreatingPage"
              />

              <div class="page-title-meta">
                <span :class="{ danger: titleError }">
                  {{ titleError || 'Page này sẽ được tạo trong workspace đã chọn.' }}
                </span>

                <span :class="{ danger: form.title.length >= 80 }">
                  {{ form.title.length }}/80
                </span>
              </div>
            </section>

            <section class="page-properties">
              <div class="page-property-row">
                <label class="page-property-label">
                  <i class="bi bi-stars"></i>
                  <span>Icon</span>
                </label>

                <div class="page-property-value">
                  <input
                    v-model="form.icon"
                    class="page-property-input"
                    type="text"
                    maxlength="8"
                    placeholder="VD: 📄, 🚀, 🧠"
                    :disabled="isCreatingPage"
                  />

                  <p class="page-property-help">
                    Để trống sẽ dùng icon mặc định.
                  </p>
                </div>
              </div>

              <div class="page-property-row">
                <label class="page-property-label">
                  <i class="bi bi-image"></i>
                  <span>Cover image</span>
                </label>

                <div class="page-property-value">
                  <input
                    v-model="form.coverImage"
                    class="page-property-input"
                    type="text"
                    placeholder="https://..."
                    :disabled="isCreatingPage"
                  />

                  <p class="page-property-help">
                    Optional. Có thể thêm cover URL sau.
                  </p>
                </div>
              </div>
            </section>

            <section class="page-preview-section">
              <div class="page-preview-label">
                Sidebar preview
              </div>

              <div class="page-preview-card">
                <div class="page-preview-icon">
                  {{ previewIcon }}
                </div>

                <div class="page-preview-content">
                  <div class="page-preview-title">
                    {{ previewTitle }}
                  </div>

                  <div class="page-preview-subtitle">
                    Inside {{ workspaceName || 'selected workspace' }}
                  </div>
                </div>

                <div class="page-preview-badge">
                  Page
                </div>
              </div>
            </section>

            <div v-if="createPageError" class="page-drawer-error">
              <i class="bi bi-exclamation-triangle"></i>
              <span>{{ createPageError }}</span>
            </div>
          </form>

          <footer class="page-drawer-footer">
            <div class="page-keyboard-hint">
              <span>Ctrl + Enter to create</span>
              <span class="page-dot"></span>
              <span>Esc to close</span>
            </div>

            <div class="page-drawer-actions">
              <button
                class="page-btn page-btn-ghost"
                type="button"
                :disabled="isCreatingPage"
                @click="closeModal"
              >
                Cancel
              </button>

              <button
                class="page-btn page-btn-primary"
                type="button"
                :disabled="!isFormValid || isCreatingPage || !workspaceId"
                @click="handleSubmit"
              >
                <span v-if="isCreatingPage" class="page-btn-spinner"></span>
                <span>{{ isCreatingPage ? 'Creating...' : 'Create page' }}</span>
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
import { useCreatePage } from '@/modules/pages/composables/useCreatePage'
import type { Guid } from '@/api/models/common.model'
import type { PageResponse } from '@/api/models/page.model'

interface CreatePageForm {
  title: string
  icon: string
  coverImage: string
}

const props = defineProps<{
  modelValue: boolean
  workspaceId: Guid | null
  workspaceName?: string | null
  parentPageId?: Guid | null
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  created: [page: PageResponse]
}>()

const {
  isCreatingPage,
  createPageError,
  createPage,
  clearCreatePageError,
} = useCreatePage()

const titleInputRef = ref<HTMLInputElement | null>(null)

const form = reactive<CreatePageForm>({
  title: '',
  icon: '',
  coverImage: '',
})

const titleError = computed(() => {
  const value = form.title.trim()

  if (!value) return 'Tên page không được để trống.'
  if (value.length > 80) return 'Tên page không được quá 80 ký tự.'

  return ''
})

const isFormValid = computed(() => {
  return !titleError.value
})

const previewTitle = computed(() => {
  return form.title.trim() || 'Untitled page'
})

const previewIcon = computed(() => {
  return form.icon.trim() || '📄'
})

watch(
  () => props.modelValue,
  async (isOpen) => {
    document.body.classList.toggle('page-drawer-lock-scroll', isOpen)

    if (!isOpen) return

    clearCreatePageError()

    await nextTick()
    titleInputRef.value?.focus()
  }
)

function resetForm() {
  form.title = ''
  form.icon = ''
  form.coverImage = ''
}

function closeModal() {
  if (isCreatingPage.value) return

  emit('update:modelValue', false)
}

function handleOverlayClick() {
  closeModal()
}

async function handleSubmit() {
  if (!props.workspaceId || !isFormValid.value || isCreatingPage.value) return

  const page = await createPage(props.workspaceId, {
    title: form.title.trim(),
    parentPageId: props.parentPageId ?? null,
    icon: form.icon.trim() || null,
    coverImage: form.coverImage.trim() || null,
  })

  if (!page) return

  emit('created', page)
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
  document.body.classList.remove('page-drawer-lock-scroll')
})
</script>

<style scoped src="./css/CreatePageModal.css"></style>