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
              :disabled="isCreatingPage || isUploadingCoverImage"
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
                :disabled="isCreatingPage || isUploadingCoverImage"
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
                    :disabled="isCreatingPage || isUploadingCoverImage"
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
                  <div
                    v-if="coverImagePreviewSrc"
                    class="page-cover-preview"
                  >
                    <img
                      :src="coverImagePreviewSrc"
                      alt="Page cover preview"
                      referrerpolicy="no-referrer"
                      @error="coverPreviewFailed = true"
                      @load="coverPreviewFailed = false"
                    />

                    <button
                      type="button"
                      title="Remove cover"
                      :disabled="isCreatingPage || isUploadingCoverImage"
                      @click="clearCoverImage"
                    >
                      <i class="bi bi-x-lg"></i>
                    </button>
                  </div>

                  <div class="page-cover-upload-row">
                    <input
                      ref="coverImageInputRef"
                      class="page-file-input"
                      type="file"
                      accept="image/*"
                      :disabled="isCreatingPage || isUploadingCoverImage"
                      @change="handleCoverImageChange"
                    />

                    <button
                      class="page-cover-upload-btn"
                      type="button"
                      :disabled="isCreatingPage || isUploadingCoverImage"
                      @click="coverImageInputRef?.click()"
                    >
                      <span
                        v-if="isUploadingCoverImage"
                        class="page-btn-spinner"
                      ></span>

                      <i
                        v-else
                        class="bi bi-cloud-arrow-up"
                      ></i>

                      <span>
                        {{ isUploadingCoverImage ? 'Uploading...' : 'Upload cover' }}
                      </span>
                    </button>

                    <input
                      v-model="form.coverImage"
                      class="page-property-input"
                      type="text"
                      placeholder="https://..."
                      :disabled="isCreatingPage || isUploadingCoverImage"
                    />
                  </div>

                  <p class="page-property-help">
                    Upload file ảnh hoặc dán URL. Link upload sẽ được lưu vào coverImage.
                  </p>

                  <p
                    v-if="coverUploadError"
                    class="page-property-error"
                  >
                    {{ coverUploadError }}
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
                :disabled="isCreatingPage || isUploadingCoverImage"
                @click="closeModal"
              >
                Cancel
              </button>

              <button
                class="page-btn page-btn-primary"
                type="button"
                :disabled="!isFormValid || isCreatingPage || isUploadingCoverImage || !workspaceId"
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
import { fileController } from '@/api/services/file.api'
import {
  getApiErrorMessage,
  getApiResultErrorMessage,
} from '@/api/utils/api-error.util'
import { normalizeImageUrl } from '@/utils/image-url.util'
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
const coverImageInputRef = ref<HTMLInputElement | null>(null)
const isUploadingCoverImage = ref(false)
const coverUploadError = ref<string | null>(null)
const coverPreviewFailed = ref(false)

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

const coverImagePreviewSrc = computed(() => {
  if (coverPreviewFailed.value) return null

  return normalizeImageUrl(form.coverImage)
})

watch(
  () => props.modelValue,
  async (isOpen) => {
    document.body.classList.toggle('page-drawer-lock-scroll', isOpen)

    if (!isOpen) return

    clearCreatePageError()
    coverUploadError.value = null
    coverPreviewFailed.value = false

    await nextTick()
    titleInputRef.value?.focus()
  }
)

function resetForm() {
  form.title = ''
  form.icon = ''
  form.coverImage = ''
  coverUploadError.value = null
  coverPreviewFailed.value = false
}

function closeModal() {
  if (isCreatingPage.value || isUploadingCoverImage.value) return

  emit('update:modelValue', false)
}

function handleOverlayClick() {
  closeModal()
}

function clearCoverImage() {
  form.coverImage = ''
  coverUploadError.value = null
  coverPreviewFailed.value = false
}

async function handleCoverImageChange(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]

  if (!file) return

  isUploadingCoverImage.value = true
  coverUploadError.value = null
  coverPreviewFailed.value = false

  try {
    const result = await fileController.uploadImage(file, 'page-cover')

    if (!result.isSuccess || !result.data) {
      coverUploadError.value = getApiResultErrorMessage(
        result,
        'Không thể upload cover image.'
      )
      return
    }

    form.coverImage = normalizeImageUrl(result.data.publicUrl) ?? result.data.publicUrl
  } catch (error) {
    coverUploadError.value = getApiErrorMessage(
      error,
      'Không thể upload cover image.'
    )
  } finally {
    isUploadingCoverImage.value = false
    input.value = ''
  }
}

async function handleSubmit() {
  if (
    !props.workspaceId ||
    !isFormValid.value ||
    isCreatingPage.value ||
    isUploadingCoverImage.value
  ) {
    return
  }

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


