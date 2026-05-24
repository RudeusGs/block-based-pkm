<template>
  <section class="page-editor-shell">
    <div
      v-if="!pageId"
      class="page-editor-empty"
    >
      <div class="page-editor-empty-icon">📄</div>
      <h2>Chọn một trang để bắt đầu</h2>
      <p>Nội dung sẽ hiện ở đây sau khi bạn chọn trang bên sidebar.</p>
    </div>

    <template v-else>
      <section
        v-if="normalizedCoverImage || canEditPage"
        class="page-editor-cover"
        :class="{ 'page-editor-cover--empty': !normalizedCoverImage }"
      >
        <img
          v-if="normalizedCoverImage"
          :src="normalizedCoverImage"
          :alt="`Ảnh bìa ${pageTitle || 'trang'}`"
          referrerpolicy="no-referrer"
          @error="coverImageFailed = true"
          @load="coverImageFailed = false"
        />

        <div
          v-else
          class="page-editor-cover-placeholder"
        >
          <i class="bi bi-image"></i>
          <span>Thêm ảnh bìa</span>
        </div>

        <input
          ref="coverFileInputRef"
          class="page-editor-cover-input"
          type="file"
          accept="image/*"
          :disabled="!canEditPage || isUploadingCoverImage"
          @change="handleCoverFileChange"
        />

        <button
          v-if="canEditPage"
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
            {{ normalizedCoverImage ? 'Đổi ảnh bìa' : 'Tải ảnh bìa' }}
          </span>
        </button>
      </section>

      <header class="page-editor-header">
        <button
          class="page-editor-icon"
          type="button"
          title="Biểu tượng trang"
        >
          {{ pageIcon || '📄' }}
        </button>

        <div class="page-editor-title-wrap">
          <p>{{ workspaceName || 'Không gian' }}</p>
          <h1>{{ pageTitle || 'Chưa đặt tên' }}</h1>
        </div>
      </header>

      <div
        v-show="isTextToolbarVisible && canEditPage"
        class="page-editor-word-toolbar"
        :style="textToolbarStyle"
        @mousedown.stop="keepTextToolbarOpen"
      >
        <div
          class="word-toolbar-dropdown word-toolbar-font"
          :class="{ open: activeTextDropdown === 'font' }"
        >
          <button
            type="button"
            class="word-toolbar-dropdown-trigger"
            title="Phông chữ"
            @mousedown.prevent.stop="toggleTextDropdown('font')"
          >
            <span>{{ selectedFontFamilyLabel }}</span>
            <i class="bi bi-chevron-down"></i>
          </button>

          <div
            v-if="activeTextDropdown === 'font'"
            class="word-toolbar-dropdown-menu font-menu"
            @mousedown.prevent.stop
          >
            <button
              v-for="font in fontFamilies"
              :key="font.value"
              type="button"
              class="word-toolbar-dropdown-item"
              :class="{ active: selectedFontFamily === font.value }"
              :style="{ fontFamily: font.value }"
              @mousedown.prevent.stop="chooseFontFamily(font.value)"
            >
              {{ font.label }}
            </button>
          </div>
        </div>

        <div
          class="word-toolbar-dropdown word-toolbar-size"
          :class="{ open: activeTextDropdown === 'size' }"
        >
          <button
            type="button"
            class="word-toolbar-dropdown-trigger"
            title="Cỡ chữ"
            @mousedown.prevent.stop="toggleTextDropdown('size')"
          >
            <span>{{ selectedFontSizeLabel }}</span>
            <i class="bi bi-chevron-down"></i>
          </button>

          <div
            v-if="activeTextDropdown === 'size'"
            class="word-toolbar-dropdown-menu size-menu"
            @mousedown.prevent.stop
          >
            <button
              v-for="size in fontSizes"
              :key="size.value"
              type="button"
              class="word-toolbar-dropdown-item"
              :class="{ active: selectedFontSize === size.value }"
              :style="{ fontSize: size.value }"
              @mousedown.prevent.stop="chooseFontSize(size.value)"
            >
              {{ size.label }}
            </button>
          </div>
        </div>

        <span class="word-toolbar-divider"></span>

        <button
          class="word-toolbar-button word-toolbar-bold"
          type="button"
          title="Đậm"
          @mousedown.prevent="toggleBold"
        >
          B
        </button>

        <button
          class="word-toolbar-button word-toolbar-italic"
          type="button"
          title="Nghiêng"
          @mousedown.prevent="toggleItalic"
        >
          I
        </button>

        <span class="word-toolbar-divider"></span>

        <label
          class="word-toolbar-color"
          title="Màu chữ"
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
          title="Màu đánh dấu"
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
          title="Xóa định dạng"
          @mousedown.prevent="clearInlineStyle"
        >
          Xóa
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
          v-if="!canEditPage && !isLoading"
          class="page-editor-readonly-banner"
        >
          <i class="bi bi-eye"></i>
          <span>Chế độ chỉ xem · Bạn chỉ có quyền xem trang này.</span>
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
            :class="{ 'page-editor-holder--readonly': !canEditPage }"
            @focusin="handleEditorFocusInGuarded"
            @beforeinput.capture="handleEditorBeforeInputGuarded"
            @paste.capture="handleEditorPasteGuarded"
            @drop.capture="handleEditorDropGuarded"
            @keydown.capture="handleEditorKeydownGuarded"
            @input="handleEditorInputGuarded"
            @focusout="handleEditorFocusOutGuarded"
            @pointermove.passive="handleEditorPointerMove"
            @pointerleave.passive="handleEditorPointerLeave"
            @mouseup="rememberTextSelectionGuarded"
            @keyup="rememberTextSelectionGuarded"
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
  canEdit?: boolean
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

  if (!file || !props.pageId || !canEditPage.value || isUploadingCoverImage.value) {
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
        'Không thể tải ảnh bìa lên.'
      )
      return
    }

    const coverImage = normalizeImageUrl(result.data.coverImage) ?? result.data.coverImage

    if (!coverImage) {
      coverUploadError.value = 'Hệ thống đã xử lý nhưng chưa trả về ảnh bìa.'
      return
    }

    uploadedCoverImage.value = coverImage
    emit('coverUploaded', {
      pageId: props.pageId,
      coverImage,
      currentRevision: result.data.currentRevision,
    })
    toast.success('Đã cập nhật ảnh bìa', 'Ảnh bìa của trang đã được tải lên.')
  } catch (error) {
    coverUploadError.value = getApiErrorMessage(
      error,
      'Không thể tải ảnh bìa lên.'
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

const canEditPage = computed(() => props.canEdit !== false && canEditDocument.value)

const fontFamilies = [
  {
    label: 'Mặc định',
    value:
      'Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif',
  },
  {
    label: 'Arial',
    value: 'Arial, Helvetica, sans-serif',
  },
  {
    label: 'Times New Roman',
    value: '"Times New Roman", Times, serif',
  },
  {
    label: 'Georgia',
    value: 'Georgia, Cambria, "Times New Roman", Times, serif',
  },
  {
    label: 'Verdana',
    value: 'Verdana, Geneva, sans-serif',
  },
  {
    label: 'Tahoma',
    value: 'Tahoma, Geneva, sans-serif',
  },
  {
    label: 'Trebuchet MS',
    value: '"Trebuchet MS", Helvetica, sans-serif',
  },
  {
    label: 'Courier New',
    value: '"Courier New", Courier, monospace',
  },
  {
    label: 'Đơn cách',
    value:
      '"JetBrains Mono", "SFMono-Regular", Consolas, "Liberation Mono", Menlo, Monaco, monospace',
  },
  {
    label: 'Comic Sans',
    value: '"Comic Sans MS", "Comic Sans", cursive',
  },
]

const fontSizes = [
  { label: '12', value: '12px' },
  { label: '14', value: '14px' },
  { label: '16', value: '16px' },
  { label: '18', value: '18px' },
  { label: '20', value: '20px' },
  { label: '24', value: '24px' },
  { label: '28', value: '28px' },
  { label: '32', value: '32px' },
  { label: '40', value: '40px' },
  { label: '48', value: '48px' },
]

type TextToolbarDropdownType = 'font' | 'size'
type TextToolbarOption = {
  label: string
  value: string
}

const activeTextDropdown = ref<TextToolbarDropdownType | null>(null)

function getOptionArray(options: unknown): TextToolbarOption[] {
  if (Array.isArray(options)) {
    return options as TextToolbarOption[]
  }

  const maybeRef = options as { value?: TextToolbarOption[] }

  if (Array.isArray(maybeRef?.value)) {
    return maybeRef.value
  }

  return []
}

const selectedFontFamilyLabel = computed(() => {
  const option = getOptionArray(fontFamilies).find(
    (item) => item.value === selectedFontFamily.value
  )

  return option?.label ?? 'Phông chữ'
})

const selectedFontSizeLabel = computed(() => {
  const option = getOptionArray(fontSizes).find(
    (item) => item.value === selectedFontSize.value
  )

  return option?.label ?? 'Cỡ chữ'
})

function toggleTextDropdown(type: TextToolbarDropdownType) {
  rememberTextSelection()

  activeTextDropdown.value = activeTextDropdown.value === type ? null : type
}

function chooseFontFamily(value: string) {
  rememberTextSelection()
  selectedFontFamily.value = value
  applyFontFamily()
  activeTextDropdown.value = null
}

function chooseFontSize(value: string) {
  rememberTextSelection()
  selectedFontSize.value = value
  applyFontSize()
  activeTextDropdown.value = null
}

watch(isTextToolbarVisible, (visible) => {
  if (!visible) {
    activeTextDropdown.value = null
  }
})

function preventReadonlyMutation(event: Event) {
  if (canEditPage.value) return false

  event.preventDefault()
  event.stopPropagation()

  return true
}

function handleEditorFocusInGuarded(event: FocusEvent) {
  if (!canEditPage.value) return

  handleEditorFocusIn(event)
}

function handleEditorBeforeInputGuarded(event: InputEvent) {
  if (preventReadonlyMutation(event)) return

  handleEditorBeforeInput(event)
}

function handleEditorPasteGuarded(event: ClipboardEvent) {
  preventReadonlyMutation(event)
}

function handleEditorDropGuarded(event: DragEvent) {
  preventReadonlyMutation(event)
}

function isReadonlyEditKey(event: KeyboardEvent) {
  if (event.ctrlKey || event.metaKey) {
    return ['x', 'v', 'b', 'i', 'u', 'z', 'y'].includes(event.key.toLowerCase())
  }

  return (
    event.key.length === 1 ||
    event.key === 'Enter' ||
    event.key === 'Backspace' ||
    event.key === 'Delete' ||
    event.key === 'Tab'
  )
}

function handleEditorKeydownGuarded(event: KeyboardEvent) {
  if (!canEditPage.value && isReadonlyEditKey(event)) {
    preventReadonlyMutation(event)
    return
  }

  if (!canEditPage.value) return

  handleEditorKeydown(event)
}

function handleEditorInputGuarded(event?: Event) {
  if (!canEditPage.value) {
    event?.preventDefault()
    event?.stopPropagation()
    return
  }

  handleEditorInput()
}

function handleEditorFocusOutGuarded(event: FocusEvent) {
  if (!canEditPage.value) return

  handleEditorFocusOut(event)
}

function rememberTextSelectionGuarded() {
  if (!canEditPage.value) return

  rememberTextSelection()
}
</script>
