<template>
  <section class="page-editor-shell">
    <div
      v-if="!pageId"
      class="page-editor-empty"
    >
      <div class="page-editor-empty-icon">📄</div>
      <h2>Chọn một page để bắt đầu</h2>
      <p>Nội dung block editor sẽ hiện ở đây sau khi bạn chọn page bên sidebar.</p>
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

        <div class="page-editor-status">
          <span
            class="status-dot"
            :class="realtimeStatusClass"
          ></span>

          <span>{{ statusLabel }}</span>
        </div>
      </header>

      <section class="page-editor-card">
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
        ></div>
      </section>

      <footer class="page-editor-footer">
        <span
          v-if="error"
          class="page-editor-error"
        >
          {{ error }}
        </span>

        <span v-else>
          Revision {{ currentRevision || 0 }}
        </span>

        <span>{{ saveLabel }}</span>
      </footer>
    </template>
  </section>
</template>

<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, ref, watch } from 'vue'
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
const lastSavedAt = ref<Date | null>(null)

let saveTimer: number | null = null
let leaseRenewTimer: number | null = null
let unsubscribeBlockCreated: (() => void) | null = null
let unsubscribeBlockUpdated: (() => void) | null = null
let unsubscribeBlockDeleted: (() => void) | null = null

const realtimeStatusClass = computed(() => {
  if (realtimeClient.state.status === 'connected') return 'online'
  if (realtimeClient.state.status === 'reconnecting') return 'warning'

  return 'offline'
})

const statusLabel = computed(() => {
  if (isSaving.value) return 'Đang lưu'
  if (realtimeClient.state.status === 'connected') return 'Realtime on'
  if (realtimeClient.state.status === 'reconnecting') return 'Đang reconnect'

  return 'Offline'
})

const saveLabel = computed(() => {
  if (isSaving.value) return 'Saving...'
  if (!lastSavedAt.value) return 'Not saved yet'

  return `Saved ${lastSavedAt.value.toLocaleTimeString()}`
})

function createBlockId() {
  return Math.random().toString(36).slice(2, 12)
}

function defaultEditorData(): OutputData {
  return {
    time: Date.now(),
    version: '2.31.0',
    blocks: [
      {
        id: createBlockId(),
        type: 'header',
        data: {
          text: 'Untitled section',
          level: 2,
        },
      },
      {
        id: createBlockId(),
        type: 'paragraph',
        data: {
          text: 'Gõ nội dung ở đây. Dùng / để mở menu block.',
        },
      },
    ],
  }
}

function normalizePayloadValue<T>(
  payload: Record<string, unknown>,
  camelKey: string,
  pascalKey: string
): T | null {
  return (payload[camelKey] ?? payload[pascalKey] ?? null) as T | null
}

function normalizeMutationPayload(payload: unknown): BlockMutationResponse | null {
  if (!payload || typeof payload !== 'object') return null

  const raw = payload as Record<string, unknown>
  const pageId = normalizePayloadValue<Guid>(raw, 'pageId', 'PageId')
  const blockId = normalizePayloadValue<Guid>(raw, 'blockId', 'BlockId')
  const appliedRevision = normalizePayloadValue<number>(
    raw,
    'appliedRevision',
    'AppliedRevision'
  )
  const block = normalizePayloadValue<BlockResponse>(raw, 'block', 'Block')

  if (!pageId || !appliedRevision) return null

  return {
    pageId,
    blockId,
    appliedRevision,
    block,
  }
}

function normalizeDeletedBlockIds(payload: unknown): Guid[] {
  if (!payload || typeof payload !== 'object') return []

  const raw = payload as Record<string, unknown>
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
    throw new Error('Realtime connection chưa có connectionId.')
  }

  return connectionId
}

async function acquireLease(blockId: Guid) {
  await realtimeClient.start()

  const editorSessionId = getEditorSessionId()

  const result = await blockController.acquireLease(blockId, {
    editorSessionId,
    holderDisplayName: 'Editor',
  })

  if (!result.isSuccess || !result.data?.granted) {
    throw new Error(
      result.message ||
        result.data?.status ||
        'Không acquire được quyền sửa block.'
    )
  }

  startLeaseRenewal(blockId)
}

function startLeaseRenewal(blockId: Guid) {
  stopLeaseRenewal()

  leaseRenewTimer = window.setInterval(() => {
    const editorSessionId = realtimeClient.state.connectionId

    if (!editorSessionId) return

    void blockController.renewLease(blockId, {
      editorSessionId,
    }).catch(() => {
      // save tiếp theo sẽ báo lỗi rõ hơn nếu lease hỏng
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

  if (!blockId || !editorSessionId) return

  try {
    await blockController.releaseLease(blockId, {
      editorSessionId,
    })
  } catch {
    // server vẫn tự cleanup khi disconnect hoặc lease hết hạn
  }
}

async function initEditor(data: OutputData) {
  await destroyEditor()
  await nextTick()

  if (!holderRef.value) return

  editor.value = new EditorJS({
    holder: holderId,
    data,
    autofocus: false,
    placeholder: "Type '/' for commands",
    minHeight: 180,
    tools: {
      header: {
        class: Header,
        inlineToolbar: true,
        config: {
          placeholder: 'Heading',
          levels: [1, 2, 3],
          defaultLevel: 2,
        },
      },
      list: {
        class: List,
        inlineToolbar: true,
        config: {
          defaultStyle: 'unordered',
        },
      },
      checklist: {
        class: Checklist,
        inlineToolbar: true,
      },
      quote: {
        class: Quote,
        inlineToolbar: true,
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
        inlineToolbar: true,
        config: {
          rows: 2,
          cols: 3,
        },
      },
      warning: {
        class: Warning,
        inlineToolbar: true,
        config: {
          titlePlaceholder: 'Title',
          messagePlaceholder: 'Message',
        },
      },
    },
    onReady: () => {
      isReady.value = true
    },
    onChange: () => {
      scheduleSave()
    },
  })

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
      getApiResultErrorMessage(result, 'Không tạo được block editor đầu tiên.')
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
        getApiResultErrorMessage(result, 'Không tải được block của page.')
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
    error.value = getApiErrorMessage(loadError, 'Không tải được editor.')
  } finally {
    isLoading.value = false
  }
}

async function saveEditorData() {
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
      throw new Error(getApiResultErrorMessage(result, 'Không lưu được block.'))
    }

    currentRevision.value = result.data.appliedRevision
    documentBlock.value = result.data.block
    lastSavedAt.value = new Date()
  } catch (saveError) {
    error.value = getApiErrorMessage(
      saveError,
      'Không lưu được nội dung. Có thể revision đã thay đổi, đang tải lại page.'
    )

    if (props.pageId) {
      await fetchPageDocument(props.pageId)
    }
  } finally {
    isSaving.value = false
  }
}

async function applyRemoteEditorData(block: BlockResponse) {
  if (!editor.value) return

  const data = parseEditorData(block)

  isApplyingRemote.value = true

  try {
    await editor.value.render(data)
  } finally {
    window.setTimeout(() => {
      isApplyingRemote.value = false
    }, 120)
  }
}

async function handleBlockMutation(envelope: RealtimeEnvelope<unknown>) {
  if (!props.pageId || envelope.pageId !== props.pageId) return

  const mutation = normalizeMutationPayload(envelope.payload)
  if (!mutation || mutation.pageId !== props.pageId) return

  if (mutation.appliedRevision <= currentRevision.value) return

  currentRevision.value = mutation.appliedRevision

  if (!mutation.block) return
  if (mutation.block.type !== 'editorjs') return

  documentBlock.value = mutation.block

  if (!isSaving.value) {
    await applyRemoteEditorData(mutation.block)
  }
}

async function handleBlockDeleted(envelope: RealtimeEnvelope<unknown>) {
  if (!props.pageId || envelope.pageId !== props.pageId) return
  if (!documentBlock.value) return

  const deletedIds = normalizeDeletedBlockIds(envelope.payload)

  if (!deletedIds.includes(documentBlock.value.id)) return

  documentBlock.value = null
  await initEditor(defaultEditorData())
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

watch(
  () => props.pageId,
  async (nextPageId, previousPageId) => {
    error.value = null
    currentRevision.value = 0
    documentBlock.value = null
    lastSavedAt.value = null

    await releaseCurrentLease()
    stopLeaseRenewal()

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