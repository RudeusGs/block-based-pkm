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
          v-if="isLoading"
          class="page-editor-loading"
        >
          <span></span>
          <span></span>
          <span></span>
        </div>

        <div
          v-show="!isLoading"
          :id="holderId"
          ref="holderRef"
          class="page-editor-holder"
          :class="{ 'page-editor-holder--readonly': !canEditDocument }"
          @mouseup="rememberTextSelection"
          @keyup="rememberTextSelection"
        ></div>
      </section>
    </template>
  </section>
</template>

<script setup lang="ts">
import { nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import EditorJS, { type OutputData } from '@editorjs/editorjs'
import Header from '@editorjs/header'
import List from '@editorjs/list'
import Checklist from '@editorjs/checklist'
import Quote from '@editorjs/quote'
import CodeTool from '@editorjs/code'
import Delimiter from '@editorjs/delimiter'
import Marker from '@editorjs/marker'
import InlineCode from '@editorjs/inline-code'
import Table from '@editorjs/table'
import Warning from '@editorjs/warning'
import { blockController } from '@/api/services/block.api'
import {
  getApiErrorMessage,
  getApiResultErrorMessage,
} from '@/api/utils/api-error.util'
import { realtimeClient } from '@/realtime/realtime.client'
import type { RealtimeEnvelope } from '@/realtime/realtime.types'
import type { Guid } from '@/api/models/common.model'
import type {
  BlockMutationResponse,
  BlockResponse,
  PageDocumentResponse,
} from '@/api/models/block.model'
import './css/PageEditor.css'

class InlineStylePersistTool {
  static get isInline() {
    return true
  }

  static get sanitize() {
    return {
      span: {
        class: true,
        style: true,
        'data-editor-inline-style': true,
      },
      b: true,
      strong: true,
      i: true,
      em: true,
      a: {
        href: true,
        target: true,
        rel: true,
      },
      mark: {
        class: true,
        style: true,
      },
      code: {
        class: true,
      },
    }
  }

  render() {
    const button = document.createElement('button')
    button.type = 'button'
    button.tabIndex = -1
    button.style.display = 'none'
    return button
  }

  surround() {
    // Giữ sanitize rule cho span style.
  }

  checkState() {
    return false
  }
}

const props = defineProps<{
  pageId: Guid | null
  pageTitle?: string | null
  pageIcon?: string | null
  workspaceName?: string | null
}>()

const holderRef = ref<HTMLElement | null>(null)
const holderId = `page-editor-${Math.random().toString(36).slice(2)}`

const editor = ref<EditorJS | null>(null)
const documentBlock = ref<BlockResponse | null>(null)
const currentRevision = ref(0)

const isLoading = ref(false)
const isSaving = ref(false)
const isApplyingRemote = ref(false)
const isReady = ref(false)
const error = ref<string | null>(null)

const canEditDocument = ref(true)

const selectedRange = ref<Range | null>(null)
const isTextToolbarVisible = ref(false)
const textToolbarStyle = ref<Record<string, string>>({
  top: '0px',
  left: '0px',
})

const selectedFontFamily = ref('')
const selectedFontSize = ref('')
const selectedTextColor = ref('#f1f1f1')
const selectedHighlightColor = ref('#5a4515')

const fontFamilies = [
  {
    label: 'Default',
    value:
      'Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif',
  },
  {
    label: 'Serif',
    value: 'Georgia, Cambria, "Times New Roman", Times, serif',
  },
  {
    label: 'Mono',
    value:
      '"JetBrains Mono", "SFMono-Regular", Consolas, "Liberation Mono", Menlo, Monaco, monospace',
  },
  {
    label: 'Arial',
    value: 'Arial, Helvetica, sans-serif',
  },
  {
    label: 'Times',
    value: '"Times New Roman", Times, serif',
  },
  {
    label: 'Courier',
    value: '"Courier New", Courier, monospace',
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

let saveTimer: number | null = null
let leaseRenewTimer: number | null = null
let toolbarHideTimer: number | null = null
let remoteApplyTimer: number | null = null
let pendingRemoteBlock: BlockResponse | null = null

let unsubscribeBlockCreated: (() => void) | null = null
let unsubscribeBlockUpdated: (() => void) | null = null
let unsubscribeBlockDeleted: (() => void) | null = null

function createBlockId() {
  return Math.random().toString(36).slice(2, 12)
}

function defaultEditorData(): OutputData {
  return {
    time: Date.now(),
    version: '2.31.0',
    blocks: [],
  }
}

function getRangeContainer(range: Range) {
  const container = range.commonAncestorContainer

  return container.nodeType === Node.ELEMENT_NODE
    ? (container as HTMLElement)
    : (container.parentElement ?? null)
}

function getCurrentEditorRange(): Range | null {
  if (!canEditDocument.value) return null

  const selection = window.getSelection()

  if (!selection || selection.rangeCount === 0 || selection.isCollapsed) {
    return null
  }

  const range = selection.getRangeAt(0)
  const container = getRangeContainer(range)

  if (!holderRef.value || !container || !holderRef.value.contains(container)) {
    return null
  }

  return range
}

function keepTextToolbarOpen() {
  if (toolbarHideTimer) {
    window.clearTimeout(toolbarHideTimer)
    toolbarHideTimer = null
  }
}

function updateTextToolbarPosition(range: Range) {
  if (!canEditDocument.value) return

  const rect = range.getBoundingClientRect()

  if (!rect || rect.width === 0 || rect.height === 0) return

  const toolbarWidth = Math.min(620, window.innerWidth - 24)

  const safeLeft = Math.min(
    Math.max(12, rect.left + rect.width / 2 - toolbarWidth / 2),
    window.innerWidth - toolbarWidth - 12
  )

  const topAbove = rect.top - 54
  const safeTop = topAbove > 12 ? topAbove : rect.bottom + 12

  textToolbarStyle.value = {
    top: `${safeTop}px`,
    left: `${safeLeft}px`,
    maxWidth: `${toolbarWidth}px`,
  }

  isTextToolbarVisible.value = true
}

function rememberTextSelection() {
  if (!canEditDocument.value) return

  keepTextToolbarOpen()

  const range = getCurrentEditorRange()

  if (range) {
    selectedRange.value = range.cloneRange()
    updateTextToolbarPosition(range)
  }
}

function hideTextToolbarSoon() {
  if (toolbarHideTimer) {
    window.clearTimeout(toolbarHideTimer)
  }

  toolbarHideTimer = window.setTimeout(() => {
    const range = getCurrentEditorRange()

    if (!range) {
      isTextToolbarVisible.value = false
    }
  }, 180)
}

function handleDocumentSelectionChange() {
  if (!canEditDocument.value) {
    isTextToolbarVisible.value = false
    selectedRange.value = null
    return
  }

  const range = getCurrentEditorRange()

  if (range) {
    selectedRange.value = range.cloneRange()
    updateTextToolbarPosition(range)
    return
  }

  hideTextToolbarSoon()
}

function handleFloatingToolbarReposition() {
  if (!selectedRange.value || !isTextToolbarVisible.value || !canEditDocument.value) {
    return
  }

  updateTextToolbarPosition(selectedRange.value)
}

function restoreTextSelection() {
  if (!selectedRange.value || !canEditDocument.value) return null

  const selection = window.getSelection()
  if (!selection) return null

  selection.removeAllRanges()
  selection.addRange(selectedRange.value)

  return selectedRange.value
}

function getClosestEditorBlock(node: Node) {
  const element =
    node.nodeType === Node.ELEMENT_NODE
      ? (node as HTMLElement)
      : node.parentElement

  return element?.closest('.ce-block') ?? null
}

function isSingleBlockRange(range: Range) {
  return (
    getClosestEditorBlock(range.startContainer) ===
    getClosestEditorBlock(range.endContainer)
  )
}

function selectNodeContents(node: Node) {
  const selection = window.getSelection()
  if (!selection) return

  const range = document.createRange()
  range.selectNodeContents(node)

  selection.removeAllRanges()
  selection.addRange(range)

  selectedRange.value = range.cloneRange()
  updateTextToolbarPosition(range)
}

function applyStyleToSelection(styles: Record<string, string>) {
  if (!canEditDocument.value) return

  const range = restoreTextSelection()
  if (!range || range.collapsed) return
  if (!isSingleBlockRange(range)) return

  const selectedText = range.toString()
  if (!selectedText.trim()) return

  const span = document.createElement('span')
  span.className = 'editor-inline-style'
  span.setAttribute('data-editor-inline-style', 'word-toolbar')

  Object.entries(styles).forEach(([key, value]) => {
    span.style.setProperty(key, value)
  })

  const content = range.extractContents()
  span.appendChild(content)
  range.insertNode(span)

  selectNodeContents(span)
  scheduleSave()
}

function execEditorCommand(command: 'bold' | 'italic' | 'removeFormat') {
  if (!canEditDocument.value) return

  const range = restoreTextSelection()
  if (!range) return

  document.execCommand(command, false)
  rememberTextSelection()
  scheduleSave()
}

function toggleBold() {
  execEditorCommand('bold')
}

function toggleItalic() {
  execEditorCommand('italic')
}

function clearInlineStyle() {
  execEditorCommand('removeFormat')
}

function applyFontFamily() {
  if (!selectedFontFamily.value) return

  applyStyleToSelection({
    'font-family': selectedFontFamily.value,
  })

  selectedFontFamily.value = ''
}

function applyFontSize() {
  if (!selectedFontSize.value) return

  applyStyleToSelection({
    'font-size': selectedFontSize.value,
    'line-height': '1.65',
  })

  selectedFontSize.value = ''
}

function applyTextColor() {
  applyStyleToSelection({
    color: selectedTextColor.value,
  })
}

function applyHighlightColor() {
  applyStyleToSelection({
    'background-color': selectedHighlightColor.value,
    'border-radius': '4px',
    padding: '0.02em 0.18em',
  })
}

function getObject(value: unknown): Record<string, unknown> | null {
  return value && typeof value === 'object' ? (value as Record<string, unknown>) : null
}

function normalizePayloadValue<T>(
  payload: Record<string, unknown>,
  camelKey: string,
  pascalKey: string
): T | null {
  return (payload[camelKey] ?? payload[pascalKey] ?? null) as T | null
}

function getEnvelopePageId(envelope: unknown): Guid | null {
  const raw = getObject(envelope)
  if (!raw) return null

  return (raw.pageId ?? raw.PageId ?? null) as Guid | null
}

function getEnvelopePayload(envelope: unknown) {
  const raw = getObject(envelope)
  if (!raw) return envelope

  return raw.payload ?? raw.Payload ?? envelope
}

function normalizeMutationPayload(payload: unknown): BlockMutationResponse | null {
  const raw = getObject(payload)
  if (!raw) return null

  const block = normalizePayloadValue<BlockResponse>(raw, 'block', 'Block')
  const rawRevision = raw.appliedRevision ?? raw.AppliedRevision
  const appliedRevision =
    typeof rawRevision === 'number' ? rawRevision : Number(rawRevision)

  const pageId =
    normalizePayloadValue<Guid>(raw, 'pageId', 'PageId') ??
    ((block as any)?.pageId as Guid | undefined) ??
    ((block as any)?.PageId as Guid | undefined) ??
    null

  const blockId =
    normalizePayloadValue<Guid>(raw, 'blockId', 'BlockId') ??
    ((block as any)?.id as Guid | undefined) ??
    ((block as any)?.Id as Guid | undefined) ??
    null

  if (!pageId || !Number.isFinite(appliedRevision)) return null

  return {
    pageId,
    blockId,
    appliedRevision,
    block,
  }
}

function normalizeDeletedBlockIds(payload: unknown): Guid[] {
  const raw = getObject(payload)
  if (!raw) return []

  const ids = raw.deletedBlockIds ?? raw.DeletedBlockIds

  return Array.isArray(ids) ? (ids as Guid[]) : []
}

function pickEditorJsBlock(document: PageDocumentResponse): BlockResponse | null {
  return (
    document.blocks.find((block) => block.type === 'editorjs') ??
    document.blocks.find((block) => block.parentBlockId === null) ??
    null
  )
}

function parseEditorData(block: BlockResponse | null): OutputData {
  if (!block?.propsJson) return defaultEditorData()

  try {
    const parsed = JSON.parse(block.propsJson)

    if (parsed && typeof parsed === 'object' && Array.isArray(parsed.blocks)) {
      return parsed as OutputData
    }
  } catch {
    // fallback below
  }

  return defaultEditorData()
}

function plainTextFromEditorData(data: OutputData) {
  return data.blocks
    .map((block) => {
      const raw = block.data as Record<string, unknown>

      if (typeof raw.text === 'string') {
        return raw.text.replace(/<[^>]+>/g, '').trim()
      }

      if (Array.isArray(raw.items)) {
        return raw.items
          .map((item) => {
            if (typeof item === 'string') return item

            if (item && typeof item === 'object' && 'text' in item) {
              return String((item as { text?: unknown }).text ?? '')
            }

            return ''
          })
          .join(' ')
          .trim()
      }

      return ''
    })
    .filter(Boolean)
    .join('\n')
    .slice(0, 2000)
}

function scheduleSave() {
  if (!canEditDocument.value) return
  if (!isReady.value || isApplyingRemote.value || isLoading.value) return

  if (saveTimer) {
    window.clearTimeout(saveTimer)
  }

  saveTimer = window.setTimeout(() => {
    void saveEditorData()
  }, 850)
}

async function ensureRealtimePageJoined(pageId: Guid) {
  await realtimeClient.start()
  await realtimeClient.joinPage(pageId)
}

function getEditorSessionId() {
  const connectionId = realtimeClient.state.connectionId

  if (!connectionId) {
    throw new Error('Chưa kết nối được phiên chỉnh sửa.')
  }

  return connectionId
}

async function setEditorReadOnly(readOnly: boolean) {
  const editorAny = editor.value as any

  try {
    if (editorAny?.readOnly?.toggle) {
      const isReadOnly = Boolean(editorAny.readOnly.isEnabled)

      if (isReadOnly !== readOnly) {
        await editorAny.readOnly.toggle(readOnly)
      }
    }
  } catch {
    // fallback bên dưới
  }

  holderRef.value
    ?.querySelectorAll<HTMLElement>('[contenteditable]')
    .forEach((element) => {
      element.setAttribute('contenteditable', readOnly ? 'false' : 'true')
    })
}

function getHttpStatus(errorLike: any) {
  return errorLike?.status ?? errorLike?.response?.status ?? errorLike?.data?.status ?? null
}

async function acquireLease(blockId: Guid) {
  await realtimeClient.start()

  const editorSessionId = getEditorSessionId()

  try {
    const result = await blockController.acquireLease(blockId, {
      editorSessionId,
      holderDisplayName: 'Editor',
    })

    if (!result.isSuccess || !result.data?.granted) {
      canEditDocument.value = false
      isTextToolbarVisible.value = false
      await setEditorReadOnly(true)
      return
    }

    canEditDocument.value = true
    await setEditorReadOnly(false)
    startLeaseRenewal(blockId)
  } catch (leaseError: any) {
    if (getHttpStatus(leaseError) === 409) {
      canEditDocument.value = false
      isTextToolbarVisible.value = false
      await setEditorReadOnly(true)
      return
    }

    throw leaseError
  }
}

function startLeaseRenewal(blockId: Guid) {
  stopLeaseRenewal()

  leaseRenewTimer = window.setInterval(() => {
    const editorSessionId = realtimeClient.state.connectionId

    if (!editorSessionId || !canEditDocument.value) return

    void blockController.renewLease(blockId, {
      editorSessionId,
    }).catch(() => {
      // save tiếp theo sẽ xử lý lỗi rõ hơn
    })
  }, 20_000)
}

function stopLeaseRenewal() {
  if (!leaseRenewTimer) return

  window.clearInterval(leaseRenewTimer)
  leaseRenewTimer = null
}

async function releaseCurrentLease() {
  const blockId = documentBlock.value?.id
  const editorSessionId = realtimeClient.state.connectionId

  if (!blockId || !editorSessionId || !canEditDocument.value) return

  try {
    await blockController.releaseLease(blockId, {
      editorSessionId,
    })
  } catch {
    // server vẫn tự cleanup khi disconnect hoặc lease hết hạn
  }
}

function getEditorDataSignature(data: OutputData) {
  return JSON.stringify(
    data.blocks.map((block) => ({
      id: block.id,
      type: block.type,
      data: block.data,
      tunes: block.tunes ?? null,
    }))
  )
}

async function patchRemoteBlocksIfPossible(nextData: OutputData) {
  if (!editor.value) return false

  const editorAny = editor.value as any
  const canUpdateBlock = typeof editorAny.blocks?.update === 'function'

  if (!canUpdateBlock) return false

  try {
    const currentData = await editor.value.save()
    const currentBlocks = currentData.blocks
    const nextBlocks = nextData.blocks

    const canPatchSafely =
      currentBlocks.length === nextBlocks.length &&
      currentBlocks.every((currentBlock, index) => {
        const nextBlock = nextBlocks[index]

        return (
          currentBlock.id &&
          nextBlock?.id &&
          currentBlock.id === nextBlock.id &&
          currentBlock.type === nextBlock.type
        )
      })

    if (!canPatchSafely) return false

    for (const nextBlock of nextBlocks) {
      const currentBlock = currentBlocks.find((item) => item.id === nextBlock.id)

      if (!currentBlock) continue

      const currentSignature = JSON.stringify({
        data: currentBlock.data,
        tunes: currentBlock.tunes ?? null,
      })

      const nextSignature = JSON.stringify({
        data: nextBlock.data,
        tunes: nextBlock.tunes ?? null,
      })

      if (currentSignature === nextSignature) continue

      await editorAny.blocks.update(nextBlock.id, nextBlock.data)
    }

    return true
  } catch {
    return false
  }
}

async function applyRemoteEditorData(block: BlockResponse) {
  if (!editor.value) return

  const nextData = parseEditorData(block)
  const currentData = await editor.value.save()

  if (getEditorDataSignature(currentData) === getEditorDataSignature(nextData)) {
    return
  }

  isApplyingRemote.value = true

  try {
    const patched = await patchRemoteBlocksIfPossible(nextData)

    if (!patched) {
      await editor.value.render(nextData)
    }

    if (!canEditDocument.value) {
      await setEditorReadOnly(true)
    }
  } finally {
    window.setTimeout(() => {
      isApplyingRemote.value = false
    }, 120)
  }
}

function scheduleRemoteEditorApply(block: BlockResponse) {
  pendingRemoteBlock = block

  if (remoteApplyTimer) {
    window.clearTimeout(remoteApplyTimer)
  }

  remoteApplyTimer = window.setTimeout(() => {
    const blockToApply = pendingRemoteBlock

    pendingRemoteBlock = null
    remoteApplyTimer = null

    if (!blockToApply) return

    void applyRemoteEditorData(blockToApply)
  }, 120)
}

async function initEditor(data: OutputData) {
  await destroyEditor()
  await nextTick()

  if (!holderRef.value) return

  const editorInlineToolbar = ['bold', 'italic', 'marker', 'inlineCode', 'link']

  editor.value = new EditorJS({
    holder: holderId,
    data,
    autofocus: false,
    placeholder: "Type '/' for commands",
    minHeight: 180,
    sanitizer: {
      span: {
        class: true,
        style: true,
        'data-editor-inline-style': true,
      },
      b: true,
      strong: true,
      i: true,
      em: true,
      a: {
        href: true,
        target: true,
        rel: true,
      },
      mark: {
        class: true,
        style: true,
      },
      code: {
        class: true,
      },
    },
    tools: {
      inlineStylePersist: {
        class: InlineStylePersistTool as any,
      },
      header: {
        class: Header,
        inlineToolbar: editorInlineToolbar,
        config: {
          placeholder: 'Heading',
          levels: [1, 2, 3],
          defaultLevel: 2,
        },
      },
      list: {
        class: List,
        inlineToolbar: editorInlineToolbar,
        config: {
          defaultStyle: 'unordered',
        },
      },
      checklist: {
        class: Checklist,
        inlineToolbar: editorInlineToolbar,
      },
      quote: {
        class: Quote,
        inlineToolbar: editorInlineToolbar,
        config: {
          quotePlaceholder: 'Quote',
          captionPlaceholder: 'Author',
        },
      },
      code: {
        class: CodeTool,
      },
      delimiter: Delimiter,
      marker: Marker,
      inlineCode: InlineCode,
      table: {
        class: Table,
        inlineToolbar: editorInlineToolbar,
        config: {
          rows: 2,
          cols: 3,
        },
      },
      warning: {
        class: Warning,
        inlineToolbar: editorInlineToolbar,
        config: {
          titlePlaceholder: 'Title',
          messagePlaceholder: 'Message',
        },
      },
    },
    onReady: () => {
      isReady.value = true

      if (!canEditDocument.value) {
        void setEditorReadOnly(true)
      }
    },
    onChange: () => {
      rememberTextSelection()
      scheduleSave()
    },
  } as any)

  await editor.value.isReady
}

async function destroyEditor() {
  isReady.value = false

  if (saveTimer) {
    window.clearTimeout(saveTimer)
    saveTimer = null
  }

  if (editor.value) {
    try {
      editor.value.destroy()
    } catch {
      // Editor.js đôi khi throw nếu destroy lúc init chưa xong
    }
  }

  editor.value = null
}

async function createInitialEditorBlock(pageId: Guid, revision: number) {
  const data = defaultEditorData()

  const result = await blockController.create(pageId, {
    expectedRevision: revision,
    type: 'editorjs',
    textContent: plainTextFromEditorData(data),
    propsJson: JSON.stringify(data),
    parentBlockId: null,
    previousBlockId: null,
    nextBlockId: null,
    schemaVersion: 1,
  })

  if (!result.isSuccess || !result.data?.block) {
    throw new Error(
      getApiResultErrorMessage(result, 'Không tạo được nội dung ban đầu.')
    )
  }

  currentRevision.value = result.data.appliedRevision
  documentBlock.value = result.data.block

  return data
}

async function fetchPageDocument(pageId: Guid) {
  isLoading.value = true
  error.value = null

  try {
    await ensureRealtimePageJoined(pageId)

    const result = await blockController.listByPage(pageId)

    if (!result.isSuccess || !result.data) {
      throw new Error(
        getApiResultErrorMessage(result, 'Không tải được nội dung page.')
      )
    }

    currentRevision.value = result.data.currentRevision

    const editorBlock = pickEditorJsBlock(result.data)
    documentBlock.value = editorBlock

    const data = editorBlock
      ? parseEditorData(editorBlock)
      : await createInitialEditorBlock(pageId, result.data.currentRevision)

    await initEditor(data)

    if (documentBlock.value) {
      await acquireLease(documentBlock.value.id)
    }
  } catch (loadError) {
    error.value = getApiErrorMessage(loadError, 'Không tải được page này.')
  } finally {
    isLoading.value = false
  }
}

async function saveEditorData() {
  if (!canEditDocument.value) return

  if (!props.pageId || !editor.value || !documentBlock.value || isSaving.value) {
    return
  }

  isSaving.value = true
  error.value = null

  try {
    await realtimeClient.start()

    const editorSessionId = getEditorSessionId()
    const data = await editor.value.save()

    const result = await blockController.update(documentBlock.value.id, {
      expectedRevision: currentRevision.value,
      editorSessionId,
      type: 'editorjs',
      textContent: plainTextFromEditorData(data),
      propsJson: JSON.stringify(data),
    })

    if (!result.isSuccess || !result.data?.block) {
      const message = getApiResultErrorMessage(result, 'Không lưu được nội dung.')

      if (message.toLowerCase().includes('chỉnh sửa')) {
        canEditDocument.value = false
        isTextToolbarVisible.value = false
        await setEditorReadOnly(true)
        return
      }

      throw new Error(message)
    }

    currentRevision.value = result.data.appliedRevision
    documentBlock.value = result.data.block
  } catch (saveError: any) {
    if (getHttpStatus(saveError) === 409) {
      canEditDocument.value = false
      isTextToolbarVisible.value = false
      await setEditorReadOnly(true)
      return
    }

    error.value = getApiErrorMessage(
      saveError,
      'Không lưu được nội dung. Đang tải lại page.'
    )

    if (props.pageId) {
      await fetchPageDocument(props.pageId)
    }
  } finally {
    isSaving.value = false
  }
}

async function handleBlockMutation(envelope: RealtimeEnvelope<unknown>) {
  if (!props.pageId) return

  const envelopePageId = getEnvelopePageId(envelope)
  const payload = getEnvelopePayload(envelope)
  const mutation = normalizeMutationPayload(payload)

  if (envelopePageId && envelopePageId !== props.pageId) return
  if (!mutation || mutation.pageId !== props.pageId) return
  if (mutation.appliedRevision <= currentRevision.value) return

  currentRevision.value = mutation.appliedRevision

  if (!mutation.block) return
  if (mutation.block.type !== 'editorjs') return

  documentBlock.value = mutation.block

  if (!isSaving.value) {
    scheduleRemoteEditorApply(mutation.block)
  }
}

async function handleBlockDeleted(envelope: RealtimeEnvelope<unknown>) {
  if (!props.pageId || !documentBlock.value) return

  const envelopePageId = getEnvelopePageId(envelope)
  const payload = getEnvelopePayload(envelope)

  if (envelopePageId && envelopePageId !== props.pageId) return

  const deletedIds = normalizeDeletedBlockIds(payload)

  if (!deletedIds.includes(documentBlock.value.id)) return

  documentBlock.value = null
  await initEditor(defaultEditorData())

  if (!canEditDocument.value) {
    await setEditorReadOnly(true)
  }
}

function bindRealtimeEvents() {
  if (!unsubscribeBlockCreated) {
    unsubscribeBlockCreated = realtimeClient.on('BlockCreated', handleBlockMutation)
  }

  if (!unsubscribeBlockUpdated) {
    unsubscribeBlockUpdated = realtimeClient.on('BlockUpdated', handleBlockMutation)
  }

  if (!unsubscribeBlockDeleted) {
    unsubscribeBlockDeleted = realtimeClient.on('BlockDeleted', handleBlockDeleted)
  }
}

function unbindRealtimeEvents() {
  unsubscribeBlockCreated?.()
  unsubscribeBlockUpdated?.()
  unsubscribeBlockDeleted?.()

  unsubscribeBlockCreated = null
  unsubscribeBlockUpdated = null
  unsubscribeBlockDeleted = null
}

onMounted(() => {
  document.addEventListener('selectionchange', handleDocumentSelectionChange)
  window.addEventListener('scroll', handleFloatingToolbarReposition, true)
  window.addEventListener('resize', handleFloatingToolbarReposition)
})

watch(
  () => props.pageId,
  async (nextPageId, previousPageId) => {
    await releaseCurrentLease()
    stopLeaseRenewal()

    error.value = null
    canEditDocument.value = true
    currentRevision.value = 0
    documentBlock.value = null
    selectedRange.value = null
    isTextToolbarVisible.value = false

    if (previousPageId) {
      try {
        await realtimeClient.leavePage(previousPageId)
      } catch {
        // bỏ qua khi chuyển page
      }
    }

    await destroyEditor()

    if (!nextPageId) return

    bindRealtimeEvents()
    await fetchPageDocument(nextPageId)
  },
  { immediate: true }
)

onBeforeUnmount(() => {
  document.removeEventListener('selectionchange', handleDocumentSelectionChange)
  window.removeEventListener('scroll', handleFloatingToolbarReposition, true)
  window.removeEventListener('resize', handleFloatingToolbarReposition)

  if (toolbarHideTimer) {
    window.clearTimeout(toolbarHideTimer)
    toolbarHideTimer = null
  }

  if (remoteApplyTimer) {
    window.clearTimeout(remoteApplyTimer)
    remoteApplyTimer = null
  }

  pendingRemoteBlock = null

  unbindRealtimeEvents()

  void releaseCurrentLease()
  stopLeaseRenewal()

  if (props.pageId) {
    void realtimeClient.leavePage(props.pageId).catch(() => {
      // bỏ qua khi unmount
    })
  }

  void destroyEditor()
})
</script>