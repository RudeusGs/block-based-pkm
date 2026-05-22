<template>
  <section class="page-editor-shell">
    <div
      v-if="!pageId"
      class="page-editor-empty"
    >
      <div class="page-editor-empty-icon">📄</div>
      <h2>Chọn một page để bắt đầu</h2>
      <p>Nội dung sẽ hiện ở đây sau khi bạn chọn page bên sidebar.</p>
    </div>

    <template v-else>
      <section
        v-if="normalizedCoverImage || canEditDocument"
        class="page-editor-cover"
        :class="{ 'page-editor-cover--empty': !normalizedCoverImage }"
      >
        <img
          v-if="normalizedCoverImage"
          :src="normalizedCoverImage"
          :alt="`${pageTitle || 'Page'} cover`"
          referrerpolicy="no-referrer"
          @error="coverImageFailed = true"
          @load="coverImageFailed = false"
        />

        <div
          v-else
          class="page-editor-cover-placeholder"
        >
          <i class="bi bi-image"></i>
          <span>Add a cover image</span>
        </div>

        <input
          ref="coverFileInputRef"
          class="page-editor-cover-input"
          type="file"
          accept="image/*"
          :disabled="!canEditDocument || isUploadingCoverImage"
          @change="handleCoverFileChange"
        />

        <button
          v-if="canEditDocument"
          class="page-editor-cover-action"
          type="button"
          :disabled="isUploadingCoverImage"
          @click="coverFileInputRef?.click()"
        >
          <span
            v-if="isUploadingCoverImage"
            class="page-editor-cover-spinner"
          ></span>

          <i
            v-else
            class="bi bi-cloud-arrow-up"
          ></i>

          <span>
            {{ normalizedCoverImage ? 'Change cover' : 'Upload cover' }}
          </span>
        </button>
      </section>

      <header class="page-editor-header">
        <button
          class="page-editor-icon"
          type="button"
          title="Page icon"
        >
          {{ pageIcon || '📄' }}
        </button>

        <div class="page-editor-title-wrap">
          <p>{{ workspaceName || 'Workspace' }}</p>
          <h1>{{ pageTitle || 'Untitled' }}</h1>
        </div>
      </header>

      <div
        v-show="isTextToolbarVisible && canEditDocument"
        class="page-editor-word-toolbar"
        :style="textToolbarStyle"
        @mousedown.stop="keepTextToolbarOpen"
      >
        <select
          v-model="selectedFontFamily"
          class="word-toolbar-select word-toolbar-font"
          title="Font"
          @mousedown="rememberTextSelection"
          @change="applyFontFamily"
        >
          <option value="">Font</option>
          <option
            v-for="font in fontFamilies"
            :key="font.value"
            :value="font.value"
          >
            {{ font.label }}
          </option>
        </select>

        <select
          v-model="selectedFontSize"
          class="word-toolbar-select word-toolbar-size"
          title="Size"
          @mousedown="rememberTextSelection"
          @change="applyFontSize"
        >
          <option value="">Size</option>
          <option
            v-for="size in fontSizes"
            :key="size.value"
            :value="size.value"
          >
            {{ size.label }}
          </option>
        </select>

        <span class="word-toolbar-divider"></span>

        <button
          class="word-toolbar-button word-toolbar-bold"
          type="button"
          title="Bold"
          @mousedown.prevent="toggleBold"
        >
          B
        </button>

        <button
          class="word-toolbar-button word-toolbar-italic"
          type="button"
          title="Italic"
          @mousedown.prevent="toggleItalic"
        >
          I
        </button>

        <span class="word-toolbar-divider"></span>

        <label
          class="word-toolbar-color"
          title="Text color"
          @mousedown="rememberTextSelection"
        >
          <span>A</span>
          <i :style="{ backgroundColor: selectedTextColor }"></i>
          <input
            v-model="selectedTextColor"
            type="color"
            @change="applyTextColor"
          />
        </label>

        <label
          class="word-toolbar-color"
          title="Highlight color"
          @mousedown="rememberTextSelection"
        >
          <span>H</span>
          <i :style="{ backgroundColor: selectedHighlightColor }"></i>
          <input
            v-model="selectedHighlightColor"
            type="color"
            @change="applyHighlightColor"
          />
        </label>

        <button
          class="word-toolbar-button word-toolbar-clear"
          type="button"
          title="Clear style"
          @mousedown.prevent="clearInlineStyle"
        >
          Clear
        </button>
      </div>

      <section class="page-editor-card">
        <div
          v-if="error"
          class="page-editor-error-banner"
        >
          <i class="bi bi-exclamation-triangle"></i>
          <span>{{ error }}</span>
        </div>

        <div
          v-if="coverUploadError"
          class="page-editor-error-banner"
        >
          <i class="bi bi-exclamation-triangle"></i>
          <span>{{ coverUploadError }}</span>
        </div>

        <div
          v-if="isLoading"
          class="page-editor-loading"
        >
          <span></span>
          <span></span>
          <span></span>
        </div>

        <div
          v-show="!isLoading"
          class="page-editor-live-layer"
        >
          <div
            :id="holderId"
            ref="holderRef"
            class="page-editor-holder"
            :class="{ 'page-editor-holder--readonly': !canEditDocument }"
            @focusin="handleEditorFocusIn"
            @beforeinput.capture="handleEditorBeforeInput"
            @keydown.capture="handleEditorKeydown"
            @input="handleEditorInput"
            @focusout="handleEditorFocusOut"
            @pointermove.passive="handleEditorPointerMove"
            @pointerleave.passive="handleEditorPointerLeave"
            @mouseup="rememberTextSelection"
            @keyup="rememberTextSelection"
          ></div>

          <div
            class="page-editor-remote-cursor-layer"
            aria-hidden="true"
          >
            <div
              v-for="pointer in remotePointers"
              :key="pointer.key"
              class="page-editor-remote-cursor"
              :style="{
                left: `${pointer.x}%`,
                top: `${pointer.y}%`,
                '--remote-cursor-color': pointer.color,
              }"
            >
              <svg
                viewBox="0 0 24 24"
                width="22"
                height="22"
              >
                <path
                  d="M4 2.8L20.6 14.2L13.1 15.1L9.6 21.7L4 2.8Z"
                  fill="currentColor"
                />
              </svg>

              <span>{{ pointer.userName }}</span>
            </div>
          </div>
        </div>
      </section>
    </template>
  </section>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import type { Guid } from '@/api/models/common.model'
import { fileController } from '@/api/services/file.api'
import {
  getApiErrorMessage,
  getApiResultErrorMessage,
} from '@/api/utils/api-error.util'
import { useToast } from '@/components/composables/useToast'
import { normalizeImageUrl } from '@/utils/image-url.util'
import { usePageEditor } from './composables/usePageEditor'
import './css/PageEditor.css'

const props = defineProps<{
  pageId: Guid | null
  pageTitle?: string | null
  pageIcon?: string | null
  pageCoverImage?: string | null
  pageRevision?: number | null
  workspaceName?: string | null
}>()

const emit = defineEmits<{
  coverUploaded: [payload: {
    pageId: Guid
    coverImage: string
    currentRevision: number
  }]
}>()

const toast = useToast()
const coverFileInputRef = ref<HTMLInputElement | null>(null)
const isUploadingCoverImage = ref(false)
const coverUploadError = ref<string | null>(null)
const coverImageFailed = ref(false)
const uploadedCoverImage = ref<string | null>(null)

const normalizedCoverImage = computed(() => {
  if (coverImageFailed.value) return null

  return (
    normalizeImageUrl(uploadedCoverImage.value) ||
    normalizeImageUrl(props.pageCoverImage)
  )
})

watch(
  () => [props.pageId, props.pageCoverImage],
  () => {
    uploadedCoverImage.value = null
    coverUploadError.value = null
    coverImageFailed.value = false
  }
)

async function handleCoverFileChange(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]

  if (!file || !props.pageId || isUploadingCoverImage.value) {
    if (input) input.value = ''
    return
  }

  isUploadingCoverImage.value = true
  coverUploadError.value = null
  coverImageFailed.value = false

  try {
    const result = await fileController.uploadPageCoverImage(
      props.pageId,
      file,
      props.pageRevision
    )

    if (!result.isSuccess || !result.data) {
      coverUploadError.value = getApiResultErrorMessage(
        result,
        'Không thể upload cover image.'
      )
      return
    }

    const coverImage = normalizeImageUrl(result.data.coverImage) ?? result.data.coverImage

    if (!coverImage) {
      coverUploadError.value = 'Backend đã xử lý nhưng chưa trả về cover image.'
      return
    }

    uploadedCoverImage.value = coverImage
    emit('coverUploaded', {
      pageId: props.pageId,
      coverImage,
      currentRevision: result.data.currentRevision,
    })
    toast.success('Đã cập nhật cover', 'Cover image của page đã được upload.')
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

const {
  holderRef,
  holderId,

  isLoading,
  error,
  canEditDocument,

  remotePointers,

  isTextToolbarVisible,
  textToolbarStyle,

  selectedFontFamily,
  selectedFontSize,
  selectedTextColor,
  selectedHighlightColor,
  fontFamilies,
  fontSizes,

  keepTextToolbarOpen,
  rememberTextSelection,
  applyFontFamily,
  applyFontSize,
  applyTextColor,
  applyHighlightColor,
  toggleBold,
  toggleItalic,
  clearInlineStyle,

  handleEditorFocusIn,
  handleEditorBeforeInput,
  handleEditorKeydown,
  handleEditorInput,
  handleEditorFocusOut,
  handleEditorPointerMove,
  handleEditorPointerLeave,
} = usePageEditor(props)
</script>


