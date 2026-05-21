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
import type {
  BlockDraftPayload,
  BlockEditingStatePayload,
  PageMousePointerPayload,
  RealtimeEnvelope,
} from '@/realtime/realtime.types'
import type { Guid } from '@/api/models/common.model'
import type {
  BlockLeaseResponse,
  BlockMutationResponse,
  BlockResponse,
  PageDocumentResponse,
} from '@/api/models/block.model'

type EditorBlockData = OutputData['blocks'][number]

interface PageEditorProps {
  pageId: Guid | null
  pageTitle?: string | null
  pageIcon?: string | null
  workspaceName?: string | null
}

interface StoredEditorJsBlockProps {
  editorjs?: {
    type: string
    data: Record<string, unknown>
    tunes?: Record<string, unknown> | null
  }
}

interface RemotePointerView {
  key: string
  userName: string
  color: string
  x: number
  y: number
}

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
      mark: {
        class: true,
        style: true,
      },
      code: {
        class: true,
      },
      a: {
        href: true,
        target: true,
        rel: true,
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

  surround() {}

  checkState() {
    return false
  }
}

const SAVE_DEBOUNCE_MS = 1200
const SOFT_SYNC_DELAY_MS = 650
const POINTER_THROTTLE_MS = 70
const REMOTE_TYPING_TTL_MS = 2200
const REMOTE_POINTER_TTL_MS = 2400
const RELEASE_LEASE_DELAY_MS = 900

function defaultEditorData(): OutputData {
  return {
    time: Date.now(),
    version: '2.31.0',
    blocks: [],
  }
}

function getObject(value: unknown): Record<string, unknown> | null {
  return value && typeof value === 'object'
    ? (value as Record<string, unknown>)
    : null
}

function normalizePayloadValue<T>(
  payload: Record<string, unknown>,
  camelKey: string,
  pascalKey: string
) {
  return (payload[camelKey] ?? payload[pascalKey] ?? null) as T | null
}

function plainTextFromHtml(value: string) {
  const element = document.createElement('div')
  element.innerHTML = value
  return element.textContent ?? element.innerText ?? ''
}

function safeJsonParse<T>(value: string | null | undefined): T | null {
  if (!value) return null

  try {
    return JSON.parse(value) as T
  } catch {
    return null
  }
}

function cssEscape(value: string) {
  const css = window.CSS as
    | (typeof CSS & { escape?: (value: string) => string })
    | undefined

  return css?.escape
    ? css.escape(value)
    : value.replace(/[^a-zA-Z0-9_-]/g, '\\$&')
}

function fallbackPointerColor(seed: string) {
  const colors = [
    '#2f80ed',
    '#eb5757',
    '#27ae60',
    '#f2994a',
    '#9b51e0',
    '#00bcd4',
    '#ff6b81',
    '#4d96ff',
  ]

  let hash = 0

  for (const char of seed) {
    hash = (hash * 31 + char.charCodeAt(0)) >>> 0
  }

  return colors[hash % colors.length] ?? '#2f80ed'
}

function sortBackendBlocks(blocks: BlockResponse[]) {
  return [...blocks].sort((left, right) => {
    const parentSort = String(left.parentBlockId ?? '').localeCompare(
      String(right.parentBlockId ?? '')
    )

    if (parentSort !== 0) return parentSort

    const orderSort = String(left.orderKey ?? '').localeCompare(
      String(right.orderKey ?? '')
    )

    if (orderSort !== 0) return orderSort

    return String(left.createdDate ?? '').localeCompare(
      String(right.createdDate ?? '')
    )
  })
}

function plainTextFromEditorBlock(block: EditorBlockData) {
  const data = (block.data ?? {}) as Record<string, any>

  if (typeof data.text === 'string') {
    return plainTextFromHtml(data.text).slice(0, 2000)
  }

  if (Array.isArray(data.items)) {
    return data.items
      .map((item) => {
        if (typeof item === 'string') return plainTextFromHtml(item)

        if (item && typeof item === 'object') {
          return plainTextFromHtml(String(item.text ?? item.content ?? ''))
        }

        return ''
      })
      .filter(Boolean)
      .join(' ')
      .slice(0, 2000)
  }

  if (typeof data.code === 'string') return data.code.slice(0, 2000)

  if (typeof data.message === 'string') {
    return plainTextFromHtml(data.message).slice(0, 2000)
  }

  if (typeof data.title === 'string') {
    return plainTextFromHtml(data.title).slice(0, 2000)
  }

  return ''
}

function blockToPropsJson(block: EditorBlockData) {
  const payload: StoredEditorJsBlockProps = {
    editorjs: {
      type: block.type,
      data: (block.data ?? {}) as Record<string, unknown>,
      tunes: (block.tunes ?? null) as Record<string, unknown> | null,
    },
  }

  return JSON.stringify(payload)
}

function serverBlockSignature(block: BlockResponse) {
  return JSON.stringify({
    type: block.type,
    textContent: block.textContent ?? '',
    propsJson: block.propsJson ?? '',
  })
}

function editorBlockSignature(block: EditorBlockData) {
  return JSON.stringify({
    type: block.type,
    textContent: plainTextFromEditorBlock(block),
    propsJson: blockToPropsJson(block),
  })
}

function parsePropsJson(propsJson: string | null) {
  return safeJsonParse<StoredEditorJsBlockProps>(propsJson)
}

function backendBlockToEditorBlock(
  block: BlockResponse,
  editorBlockId: string = block.id
): EditorBlockData {
  const propsJson = parsePropsJson(block.propsJson)
  const stored = propsJson?.editorjs

  if (stored?.type) {
    return {
      id: editorBlockId,
      type: stored.type,
      data: stored.data ?? {},
      tunes: stored.tunes ?? undefined,
    } as EditorBlockData
  }

  if (block.type === 'editorjs' && block.propsJson) {
    const legacy = safeJsonParse<OutputData>(block.propsJson)

    if (legacy?.blocks?.[0]) {
      return {
        ...legacy.blocks[0],
        id: editorBlockId,
      } as EditorBlockData
    }
  }

  return {
    id: editorBlockId,
    type: block.type === 'paragraph' ? 'paragraph' : block.type,
    data: {
      text: block.textContent ?? '',
    },
  } as EditorBlockData
}

function documentToEditorData(document: PageDocumentResponse): OutputData {
  return {
    time: Date.now(),
    version: '2.31.0',
    blocks: sortBackendBlocks(document.blocks).map((block) =>
      backendBlockToEditorBlock(block)
    ),
  }
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

function getRevisionFromEnvelope(
  envelope: RealtimeEnvelope<unknown>,
  fallbackRevision?: number | null
) {
  if (typeof envelope.revision === 'number') return envelope.revision
  if (typeof fallbackRevision === 'number') return fallbackRevision
  return null
}

export function usePageEditor(props: PageEditorProps) {
  const holderRef = ref<HTMLElement | null>(null)
  const holderId = `page-editor-${Math.random().toString(36).slice(2)}`

  const editor = ref<EditorJS | null>(null)
  const currentRevision = ref(0)

  const isLoading = ref(false)
  const isSaving = ref(false)
  const isApplyingRemote = ref(false)
  const isReady = ref(false)
  const error = ref<string | null>(null)

  const canEditDocument = ref(true)
  const remotePointers = ref<RemotePointerView[]>([])

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
        '"JetBrains Mono", "SFMono-Regular", Consolas, "Liberation Mono", monospace',
    },
  ]

  const fontSizes = [
    { label: '12', value: '12px' },
    { label: '14', value: '14px' },
    { label: '16', value: '16px' },
    { label: '18', value: '18px' },
    { label: '24', value: '24px' },
    { label: '32', value: '32px' },
  ]

  const serverBlocksById = ref<Record<Guid, BlockResponse>>({})
  const serverBlockSignatures = ref<Record<Guid, string>>({})

  const heldLeaseBlockIds = ref<Set<Guid>>(new Set())
  const heldLeaseSessionIds = new Map<Guid, string>()

  const localToServerBlockIds = new Map<string, Guid>()
  const serverToLocalBlockIds = new Map<Guid, string>()
  const remoteBlocksPendingUi = new Set<Guid>()

  const remotePointerMap = new Map<string, RemotePointerView>()
  const remotePointerTimers = new Map<string, number>()
  const remoteTypingTimers = new Map<Guid, number>()

  const activeEditorBlockId = ref<string | null>(null)

  let saveTimer: number | null = null
  let softSyncTimer: number | null = null
  let releaseBlockTimer: number | null = null
  let pointerSendTimer: number | null = null
  let heartbeatTimer: number | null = null
  let pendingRemoteRefresh = false
  let isDestroyed = false

  let lastPointerPayload:
    | {
        pageId: Guid
        blockId: Guid | null
        x: number
        y: number
      }
    | null = null

  let unsubscribeBlockCreated: (() => void) | null = null
  let unsubscribeBlockUpdated: (() => void) | null = null
  let unsubscribeBlockDeleted: (() => void) | null = null
  let unsubscribeBlockDraftChanged: (() => void) | null = null
  let unsubscribeBlockEditingStateChanged: (() => void) | null = null
  let unsubscribeBlockLeaseChanged: (() => void) | null = null
  let unsubscribePageMousePointerChanged: (() => void) | null = null

  function getEditorSessionId() {
    return realtimeClient.state.connectionId ?? `local-${holderId}`
  }

  function getCurrentUserDisplayName() {
    return 'Bạn'
  }

  function resolveServerBlockId(editorBlockId: string | undefined | null) {
    if (!editorBlockId) return null
    if (serverBlocksById.value[editorBlockId]) return editorBlockId as Guid
    return localToServerBlockIds.get(editorBlockId) ?? null
  }

  function rememberBlockAlias(localBlockId: string | undefined, serverBlockId: Guid) {
    if (!localBlockId || localBlockId === serverBlockId) return

    localToServerBlockIds.set(localBlockId, serverBlockId)
    serverToLocalBlockIds.set(serverBlockId, localBlockId)
  }

  function forgetServerBlockAlias(serverBlockId: Guid) {
    const localBlockId = serverToLocalBlockIds.get(serverBlockId)

    if (localBlockId) {
      localToServerBlockIds.delete(localBlockId)
    }

    serverToLocalBlockIds.delete(serverBlockId)
  }

  function getEditorBlockIdForServerBlock(serverBlockId: Guid) {
    return serverToLocalBlockIds.get(serverBlockId) ?? serverBlockId
  }

  function backendBlockToVisibleEditorBlock(block: BlockResponse) {
    return backendBlockToEditorBlock(block, getEditorBlockIdForServerBlock(block.id))
  }

  function hydrateServerSnapshot(document: PageDocumentResponse) {
    const nextBlocksById: Record<Guid, BlockResponse> = {}
    const nextSignatures: Record<Guid, string> = {}

    for (const block of document.blocks) {
      nextBlocksById[block.id] = block
      nextSignatures[block.id] = serverBlockSignature(block)
    }

    serverBlocksById.value = nextBlocksById
    serverBlockSignatures.value = nextSignatures
    currentRevision.value = document.currentRevision
  }

  function markLeaseHeld(blockId: Guid, editorSessionId: string) {
    const next = new Set(heldLeaseBlockIds.value)
    next.add(blockId)
    heldLeaseBlockIds.value = next
    heldLeaseSessionIds.set(blockId, editorSessionId)
  }

  function clearLease(blockId: Guid) {
    const next = new Set(heldLeaseBlockIds.value)
    next.delete(blockId)
    heldLeaseBlockIds.value = next
    heldLeaseSessionIds.delete(blockId)
  }

  function hasCurrentLease(blockId: Guid) {
    const editorSessionId = getEditorSessionId()
    const heldSessionId = heldLeaseSessionIds.get(blockId)

    return (
      heldLeaseBlockIds.value.has(blockId) &&
      (!heldSessionId || heldSessionId === editorSessionId)
    )
  }

  async function ensureBlockLease(blockId: Guid) {
    if (hasCurrentLease(blockId)) return

    const editorSessionId = getEditorSessionId()

    const result = await blockController.acquireLease(blockId, {
      editorSessionId,
      holderDisplayName: getCurrentUserDisplayName(),
    })

    if (!result.isSuccess || !result.data?.granted) {
      throw new Error(
        result.data?.holderDisplayName
          ? `${result.data.holderDisplayName} đang sửa block này.`
          : getApiResultErrorMessage(result, 'Block này đang được người khác sửa.')
      )
    }

    markLeaseHeld(blockId, editorSessionId)

    await realtimeClient
      .sendBlockEditingState({
        pageId: props.pageId!,
        blockId,
        editorSessionId,
        isEditing: true,
      })
      .catch(() => {})
  }

  async function releaseBlockLease(blockId: Guid) {
    if (!heldLeaseBlockIds.value.has(blockId)) return

    const editorSessionId = heldLeaseSessionIds.get(blockId) ?? getEditorSessionId()

    try {
      await blockController.releaseLease(blockId, {
        editorSessionId,
      })

      if (props.pageId) {
        await realtimeClient
          .sendBlockEditingState({
            pageId: props.pageId,
            blockId,
            editorSessionId,
            isEditing: false,
          })
          .catch(() => {})
      }
    } finally {
      clearLease(blockId)
    }
  }

  async function releaseAllLeases() {
    const blockIds = [...heldLeaseBlockIds.value]

    await Promise.allSettled(blockIds.map((blockId) => releaseBlockLease(blockId)))
  }

  function getClosestEditorBlock(node: EventTarget | Node | null) {
    if (!(node instanceof Node)) return null

    const element =
      node instanceof HTMLElement ? node : node.parentElement

    return element?.closest?.('.ce-block') ?? null
  }

  function getEditorBlockIdFromEvent(event: Event) {
    const blockElement = getClosestEditorBlock(event.target)

    if (!(blockElement instanceof HTMLElement)) return null

    return blockElement.dataset.id ?? null
  }

  function getEditorBlockIdFromNode(node: Node | null) {
    const blockElement = getClosestEditorBlock(node)

    if (!(blockElement instanceof HTMLElement)) return null

    return blockElement.dataset.id ?? null
  }

  function getEditorBlockElementByEditorId(editorBlockId: string | null | undefined) {
    if (!holderRef.value || !editorBlockId) return null

    return holderRef.value.querySelector<HTMLElement>(
      `.ce-block[data-id="${cssEscape(editorBlockId)}"]`
    )
  }

  function clearRemoteTyping(blockId: Guid) {
    const timer = remoteTypingTimers.get(blockId)

    if (timer) {
      window.clearTimeout(timer)
      remoteTypingTimers.delete(blockId)
    }

    const element = getEditorBlockElementByEditorId(
      getEditorBlockIdForServerBlock(blockId)
    )

    if (!element) return

    element.classList.remove('ce-block--remote-typing')
    element.removeAttribute('data-remote-user')
  }

  function markRemoteTyping(blockId: Guid, userName?: string | null) {
    const element = getEditorBlockElementByEditorId(
      getEditorBlockIdForServerBlock(blockId)
    )

    if (!element) return

    element.classList.add('ce-block--remote-typing')
    element.dataset.remoteUser = userName?.trim() || 'Đang sửa'

    const oldTimer = remoteTypingTimers.get(blockId)

    if (oldTimer) {
      window.clearTimeout(oldTimer)
    }

    const timer = window.setTimeout(() => {
      clearRemoteTyping(blockId)
    }, REMOTE_TYPING_TTL_MS)

    remoteTypingTimers.set(blockId, timer)
  }

  function refreshRemotePointerList() {
    remotePointers.value = [...remotePointerMap.values()]
  }

  function clearRemotePointer(connectionId: string) {
    const timer = remotePointerTimers.get(connectionId)

    if (timer) {
      window.clearTimeout(timer)
      remotePointerTimers.delete(connectionId)
    }

    remotePointerMap.delete(connectionId)
    refreshRemotePointerList()
  }

  function handlePageMousePointerChanged(
    envelope: RealtimeEnvelope<PageMousePointerPayload>
  ) {
    const payload = envelope.payload

    if (!payload || !props.pageId) return
    if (payload.pageId !== props.pageId) return
    if (payload.connectionId === realtimeClient.state.connectionId) return

    if (payload.isLeaving) {
      clearRemotePointer(payload.connectionId)
      return
    }

    const color =
      payload.color?.trim() ||
      fallbackPointerColor(payload.connectionId || payload.userId)

    remotePointerMap.set(payload.connectionId, {
      key: payload.connectionId,
      userName: payload.userName?.trim() || 'Người dùng',
      color,
      x: Math.max(0, Math.min(100, Number(payload.x) || 0)),
      y: Math.max(0, Math.min(100, Number(payload.y) || 0)),
    })

    const oldTimer = remotePointerTimers.get(payload.connectionId)

    if (oldTimer) {
      window.clearTimeout(oldTimer)
    }

    const timer = window.setTimeout(() => {
      clearRemotePointer(payload.connectionId)
    }, REMOTE_POINTER_TTL_MS)

    remotePointerTimers.set(payload.connectionId, timer)
    refreshRemotePointerList()
  }

  async function saveEditorSnapshot() {
    if (!editor.value || !isReady.value) return defaultEditorData()

    try {
      return await editor.value.save()
    } catch {
      return defaultEditorData()
    }
  }

  async function findEditorBlockIndexByServerId(serverBlockId: Guid) {
    const data = await saveEditorSnapshot()

    return data.blocks.findIndex((block) => {
      return resolveServerBlockId(block.id) === serverBlockId
    })
  }

  async function patchEditorBlock(block: EditorBlockData) {
    if (!editor.value || !isReady.value || !block.id) return false

    const editorAny = editor.value as any

    if (typeof editorAny.blocks?.update !== 'function') return false

    try {
      await editorAny.blocks.update(block.id, block.data, block.tunes)
      return true
    } catch {
      try {
        await editorAny.blocks.update(block.id, block.data)
        return true
      } catch {
        return false
      }
    }
  }

  async function insertEditorBlock(
    block: EditorBlockData,
    serverBlockId: Guid,
    index: number
  ) {
    if (!editor.value || !isReady.value) return false

    const editorAny = editor.value as any

    if (typeof editorAny.blocks?.insert !== 'function') return false

    const safeIndex = Math.max(0, index)

    try {
      await editorAny.blocks.insert(
        block.type,
        block.data ?? {},
        block.tunes ?? {},
        safeIndex,
        false,
        false,
        block.id
      )
    } catch {
      try {
        await editorAny.blocks.insert(
          block.type,
          block.data ?? {},
          {},
          safeIndex,
          false
        )
      } catch {
        return false
      }
    }

    const data = await saveEditorSnapshot()
    const insertedBlock = data.blocks[safeIndex]

    if (insertedBlock?.id) {
      rememberBlockAlias(insertedBlock.id, serverBlockId)
    }

    return true
  }

  async function deleteEditorBlock(serverBlockId: Guid) {
    if (!editor.value || !isReady.value) return false

    const editorAny = editor.value as any

    if (typeof editorAny.blocks?.delete !== 'function') return false

    const index = await findEditorBlockIndexByServerId(serverBlockId)

    if (index < 0) return false

    try {
      await editorAny.blocks.delete(index)
      forgetServerBlockAlias(serverBlockId)
      return true
    } catch {
      return false
    }
  }

  async function applyRemoteBlockToEditor(block: BlockResponse) {
    if (!editor.value || !isReady.value) return false

    const serverBlockId = block.id
    const existingIndex = await findEditorBlockIndexByServerId(serverBlockId)
    const editorBlock = backendBlockToVisibleEditorBlock(block)

    if (existingIndex >= 0) {
      return patchEditorBlock(editorBlock)
    }

    const sortedBlocks = sortBackendBlocks(Object.values(serverBlocksById.value))
    const targetIndex = sortedBlocks.findIndex((item) => item.id === serverBlockId)

    return insertEditorBlock(
      editorBlock,
      serverBlockId,
      targetIndex >= 0 ? targetIndex : sortedBlocks.length
    )
  }

  async function reloadFromServer(render = false) {
    if (!props.pageId) return null

    const result = await blockController.listByPage(props.pageId)

    if (!result.isSuccess || !result.data) {
      throw new Error(getApiResultErrorMessage(result, 'Không tải được page.'))
    }

    hydrateServerSnapshot(result.data)

    if (render) {
      await renderEditorData(documentToEditorData(result.data))
    }

    return result.data
  }

  async function softSyncFromServer() {
    const document = await reloadFromServer(false)

    if (!document) return null

    for (const block of sortBackendBlocks(document.blocks)) {
      const activeUiBlockId = getEditorBlockIdForServerBlock(block.id)

      if (activeUiBlockId === activeEditorBlockId.value || isSaving.value) {
        remoteBlocksPendingUi.add(block.id)
        continue
      }

      const applied = await applyRemoteBlockToEditor(block)

      if (applied) {
        remoteBlocksPendingUi.delete(block.id)
      } else {
        remoteBlocksPendingUi.add(block.id)
      }
    }

    const serverIds = new Set(document.blocks.map((block) => block.id))
    const currentData = await saveEditorSnapshot()

    for (const editorBlock of currentData.blocks) {
      const serverBlockId = resolveServerBlockId(editorBlock.id)

      if (!serverBlockId || serverIds.has(serverBlockId)) continue
      if (editorBlock.id === activeEditorBlockId.value || isSaving.value) continue

      await deleteEditorBlock(serverBlockId)
    }

    return document
  }

  function scheduleSoftSync(delay = SOFT_SYNC_DELAY_MS) {
    if (softSyncTimer) {
      window.clearTimeout(softSyncTimer)
    }

    softSyncTimer = window.setTimeout(() => {
      softSyncTimer = null

      if (isSaving.value) {
        scheduleSoftSync(SOFT_SYNC_DELAY_MS)
        return
      }

      void softSyncFromServer()
        .then(() => {
          pendingRemoteRefresh = false
        })
        .catch(() => {
          pendingRemoteRefresh = true
        })
    }, delay)
  }

  async function renderEditorData(data: OutputData) {
    if (!editor.value || !isReady.value) return

    isApplyingRemote.value = true

    try {
      await editor.value.render(data)
    } finally {
      window.setTimeout(() => {
        isApplyingRemote.value = false
      }, 80)
    }
  }

  function getEditorTools() {
    return {
      header: {
        class: Header as any,
        inlineToolbar: true,
        config: {
          levels: [1, 2, 3],
          defaultLevel: 2,
        },
      },
      list: {
        class: List as any,
        inlineToolbar: true,
      },
      checklist: {
        class: Checklist as any,
        inlineToolbar: true,
      },
      quote: {
        class: Quote as any,
        inlineToolbar: true,
      },
      code: {
        class: CodeTool as any,
      },
      delimiter: {
        class: Delimiter as any,
      },
      marker: {
        class: Marker as any,
      },
      inlineCode: {
        class: InlineCode as any,
      },
      table: {
        class: Table as any,
        inlineToolbar: true,
      },
      warning: {
        class: Warning as any,
        inlineToolbar: true,
      },
      inlineStylePersist: {
        class: InlineStylePersistTool as any,
      },
    }
  }

  async function destroyEditor() {
    isReady.value = false

    if (!editor.value) return

    try {
      await editor.value.isReady
      editor.value.destroy()
    } catch {
      // ignore
    } finally {
      editor.value = null
    }
  }

  async function createEditor(data: OutputData) {
    await destroyEditor()
    await nextTick()

    if (!holderRef.value || !props.pageId || isDestroyed) return

    editor.value = new EditorJS({
      holder: holderId,
      data,
      tools: getEditorTools(),
      autofocus: false,
      placeholder: canEditDocument.value
        ? 'Viết gì đó đi bro...'
        : 'Bạn chỉ có quyền xem page này.',
      readOnly: !canEditDocument.value,
      minHeight: 220,
      onReady: () => {
        isReady.value = true
      },
    })

    await editor.value.isReady
    isReady.value = true
  }

  function getChangedExistingBlocks(data: OutputData) {
    return data.blocks.filter((block) => {
      const serverBlockId = resolveServerBlockId(block.id)

      if (!serverBlockId) return false

      const serverBlock = serverBlocksById.value[serverBlockId]
      if (!serverBlock) return false

      return editorBlockSignature(block) !== serverBlockSignatures.value[serverBlockId]
    })
  }

  function getCreatedBlocks(data: OutputData) {
    return data.blocks.filter((block) => !resolveServerBlockId(block.id))
  }

  function getDeletedBlockIds(data: OutputData) {
    const currentServerIds = new Set(
      data.blocks
        .map((block) => resolveServerBlockId(block.id))
        .filter((id): id is Guid => Boolean(id))
    )

    return Object.keys(serverBlocksById.value).filter((serverId) => {
      if (remoteBlocksPendingUi.has(serverId)) return false
      return !currentServerIds.has(serverId)
    })
  }

  async function syncEditorDataToServer(data: OutputData) {
    if (!props.pageId) return

    const createdBlocks = getCreatedBlocks(data)
    const changedExistingBlocks = getChangedExistingBlocks(data)
    const deletedBlockIds = getDeletedBlockIds(data)

    if (
      createdBlocks.length === 0 &&
      changedExistingBlocks.length === 0 &&
      deletedBlockIds.length === 0
    ) {
      return
    }

    isSaving.value = true

    try {
      let previousBlockId: Guid | null = null

      for (const editorBlock of data.blocks) {
        const existingServerId = resolveServerBlockId(editorBlock.id)

        if (existingServerId) {
          previousBlockId = existingServerId
          continue
        }

        const result = await blockController.create(props.pageId, {
          expectedRevision: currentRevision.value,
          type: editorBlock.type,
          textContent: plainTextFromEditorBlock(editorBlock),
          propsJson: blockToPropsJson(editorBlock),
          previousBlockId,
          nextBlockId: null,
          parentBlockId: null,
          schemaVersion: 1,
        })

        if (!result.isSuccess || !result.data?.block) {
          throw new Error(getApiResultErrorMessage(result, 'Không tạo được block.'))
        }

        currentRevision.value = result.data.appliedRevision
        previousBlockId = result.data.block.id

        rememberBlockAlias(editorBlock.id, result.data.block.id)
        serverBlocksById.value[result.data.block.id] = result.data.block
        serverBlockSignatures.value[result.data.block.id] =
          serverBlockSignature(result.data.block)
      }

      for (const editorBlock of changedExistingBlocks) {
        const serverBlockId = resolveServerBlockId(editorBlock.id)

        if (!serverBlockId) continue

        await ensureBlockLease(serverBlockId)

        const activeSessionId =
          heldLeaseSessionIds.get(serverBlockId) ?? getEditorSessionId()

        const result = await blockController.update(serverBlockId, {
          expectedRevision: currentRevision.value,
          editorSessionId: activeSessionId,
          type: editorBlock.type,
          textContent: plainTextFromEditorBlock(editorBlock),
          propsJson: blockToPropsJson(editorBlock),
        })

        if (!result.isSuccess || !result.data?.block) {
          throw new Error(getApiResultErrorMessage(result, 'Không lưu được block.'))
        }

        markLeaseHeld(result.data.block.id, activeSessionId)
        rememberBlockAlias(editorBlock.id, result.data.block.id)

        currentRevision.value = result.data.appliedRevision
        serverBlocksById.value[result.data.block.id] = result.data.block
        serverBlockSignatures.value[result.data.block.id] =
          serverBlockSignature(result.data.block)
      }

      for (const blockId of deletedBlockIds) {
        await ensureBlockLease(blockId)

        const activeSessionId = heldLeaseSessionIds.get(blockId) ?? getEditorSessionId()

        const result = await blockController.delete(blockId, {
          expectedRevision: currentRevision.value,
          editorSessionId: activeSessionId,
          note: null,
        })

        if (!result.isSuccess || !result.data) {
          throw new Error(getApiResultErrorMessage(result, 'Không xóa được block.'))
        }

        currentRevision.value = result.data.appliedRevision
        delete serverBlocksById.value[blockId]
        delete serverBlockSignatures.value[blockId]
        clearLease(blockId)
        forgetServerBlockAlias(blockId)
      }

      error.value = null

      if (pendingRemoteRefresh) {
        scheduleSoftSync(220)
      }
    } finally {
      isSaving.value = false
    }
  }

  async function saveEditorData() {
    if (
      !props.pageId ||
      !editor.value ||
      !isReady.value ||
      isApplyingRemote.value ||
      isLoading.value
    ) {
      return
    }

    try {
      const data = await editor.value.save()
      await syncEditorDataToServer(data)
    } catch (saveError: any) {
      if (saveError?.status === 409 || saveError?.data?.statusCode === 409) {
        pendingRemoteRefresh = true
        scheduleSoftSync(220)
        error.value = 'Revision lệch. Mình đang đồng bộ mềm để tránh ghi đè.'
        return
      }

      error.value = getApiErrorMessage(saveError, 'Không lưu được nội dung page.')
    }
  }

  function scheduleSave(delay = SAVE_DEBOUNCE_MS) {
    if (saveTimer) {
      window.clearTimeout(saveTimer)
    }

    saveTimer = window.setTimeout(() => {
      saveTimer = null
      void saveEditorData()
    }, delay)
  }

  function prepareBlockForEdit(editorBlockId: string) {
    activeEditorBlockId.value = editorBlockId

    const serverBlockId = resolveServerBlockId(editorBlockId)

    if (!serverBlockId) {
      scheduleSave(SAVE_DEBOUNCE_MS)
      return
    }

    if (hasCurrentLease(serverBlockId)) {
      return
    }

    void ensureBlockLease(serverBlockId).catch((leaseError) => {
      error.value = getApiErrorMessage(
        leaseError,
        'Block này đang được người khác chỉnh sửa.'
      )
    })
  }

  function scheduleReleaseBlockLease(editorBlockId: string) {
    const serverBlockId = resolveServerBlockId(editorBlockId)

    if (!serverBlockId) return

    if (releaseBlockTimer) {
      window.clearTimeout(releaseBlockTimer)
      releaseBlockTimer = null
    }

    releaseBlockTimer = window.setTimeout(() => {
      releaseBlockTimer = null

      if (activeEditorBlockId.value === editorBlockId) return
      if (!heldLeaseBlockIds.value.has(serverBlockId)) return

      void saveEditorData()
        .catch(() => {})
        .finally(() => {
          void releaseBlockLease(serverBlockId)
        })
    }, RELEASE_LEASE_DELAY_MS)
  }

  function isEditingKeyboardIntent(event: KeyboardEvent) {
    if (event.defaultPrevented) return false

    if (event.ctrlKey || event.metaKey) {
      return ['b', 'i', 'u', 'x', 'v', 'z', 'y'].includes(event.key.toLowerCase())
    }

    if (event.altKey) return false

    return (
      event.key.length === 1 ||
      event.key === 'Backspace' ||
      event.key === 'Delete' ||
      event.key === 'Enter' ||
      event.key === 'Tab'
    )
  }

  function isEditingBeforeInputIntent(event: InputEvent) {
    const inputType = event.inputType ?? ''

    if (!inputType) return true

    return (
      inputType.startsWith('insert') ||
      inputType.startsWith('delete') ||
      inputType.startsWith('format') ||
      inputType === 'historyUndo' ||
      inputType === 'historyRedo'
    )
  }

  function handleEditorFocusIn(event: FocusEvent) {
    const blockId = getEditorBlockIdFromEvent(event)

    if (!blockId) return

    const previousBlockId = activeEditorBlockId.value

    activeEditorBlockId.value = blockId

    if (previousBlockId && previousBlockId !== blockId) {
      scheduleReleaseBlockLease(previousBlockId)
    }

    // Focus chỉ là xem/đặt caret. Không lock ở đây.
  }

  function handleEditorBeforeInput(event: InputEvent) {
    if (!canEditDocument.value || isApplyingRemote.value || isLoading.value) return
    if (!isEditingBeforeInputIntent(event)) return

    const blockId = getEditorBlockIdFromEvent(event)

    if (!blockId) return

    prepareBlockForEdit(blockId)
  }

  function handleEditorKeydown(event: KeyboardEvent) {
    if (!canEditDocument.value || isApplyingRemote.value || isLoading.value) return
    if (!isEditingKeyboardIntent(event)) return

    const blockId = getEditorBlockIdFromEvent(event)

    if (!blockId) return

    prepareBlockForEdit(blockId)
  }

  function handleEditorInput() {
    if (!canEditDocument.value || isApplyingRemote.value || isLoading.value) return

    scheduleSave(SAVE_DEBOUNCE_MS)

    const serverBlockId = resolveServerBlockId(activeEditorBlockId.value)

    if (!props.pageId || !serverBlockId) return

    void realtimeClient
      .sendBlockEditingState({
        pageId: props.pageId,
        blockId: serverBlockId,
        editorSessionId: getEditorSessionId(),
        isEditing: true,
      })
      .catch(() => {})
  }

  function handleEditorFocusOut(event: FocusEvent) {
    const blockId = getEditorBlockIdFromEvent(event)

    if (!blockId) return

    const nextTarget =
      event.relatedTarget instanceof Node ? event.relatedTarget : null
    const nextBlockId = getEditorBlockIdFromNode(nextTarget)

    if (nextBlockId === blockId) return

    activeEditorBlockId.value = nextBlockId
    scheduleReleaseBlockLease(blockId)
    scheduleSave(250)
  }

  function getOwnPointerColor() {
    const connectionId = realtimeClient.state.connectionId ?? 'local'
    return fallbackPointerColor(connectionId)
  }

  function handleEditorPointerMove(event: PointerEvent) {
    if (!props.pageId || !holderRef.value) return
    if (realtimeClient.state.status !== 'connected') return

    const rect = holderRef.value.getBoundingClientRect()

    if (rect.width <= 0 || rect.height <= 0) return

    const x = ((event.clientX - rect.left) / rect.width) * 100
    const y = ((event.clientY - rect.top) / rect.height) * 100

    if (x < 0 || x > 100 || y < 0 || y > 100) return

    const editorBlockId = getEditorBlockIdFromEvent(event)
    const serverBlockId = resolveServerBlockId(editorBlockId)

    lastPointerPayload = {
      pageId: props.pageId,
      blockId: serverBlockId,
      x: Math.round(x * 10) / 10,
      y: Math.round(y * 10) / 10,
    }

    if (pointerSendTimer) return

    pointerSendTimer = window.setTimeout(() => {
      pointerSendTimer = null

      if (!lastPointerPayload) return

      void realtimeClient
        .sendMousePointer({
          ...lastPointerPayload,
          color: getOwnPointerColor(),
          isLeaving: false,
        })
        .catch(() => {})
    }, POINTER_THROTTLE_MS)
  }

  function handleEditorPointerLeave() {
    if (!props.pageId) return
    if (realtimeClient.state.status !== 'connected') return

    if (pointerSendTimer) {
      window.clearTimeout(pointerSendTimer)
      pointerSendTimer = null
    }

    lastPointerPayload = null

    void realtimeClient
      .sendMousePointer({
        pageId: props.pageId,
        blockId: null,
        x: 0,
        y: 0,
        color: getOwnPointerColor(),
        isLeaving: true,
      })
      .catch(() => {})
  }

  function handleBlockDraftChanged(
    envelope: RealtimeEnvelope<BlockDraftPayload>
  ) {
    const payload = envelope.payload

    if (!payload || !props.pageId) return
    if (payload.pageId !== props.pageId) return
    if (payload.connectionId === realtimeClient.state.connectionId) return

    // Không patch nội dung draft vào DOM. Chỉ hiện typing indicator.
    markRemoteTyping(payload.blockId, payload.userName)
  }

  function handleBlockEditingStateChanged(
    envelope: RealtimeEnvelope<BlockEditingStatePayload>
  ) {
    const payload = envelope.payload

    if (!payload || !props.pageId) return
    if (payload.pageId !== props.pageId) return
    if (payload.connectionId === realtimeClient.state.connectionId) return

    if (!payload.isEditing) {
      clearRemoteTyping(payload.blockId)
      return
    }

    markRemoteTyping(payload.blockId, payload.userName)
  }

  function handleBlockLeaseChanged(envelope: RealtimeEnvelope<BlockLeaseResponse>) {
    const lease = envelope.payload

    if (!lease || !props.pageId) return
    if (lease.pageId !== props.pageId) return

    if (!lease.granted || !lease.isHeldByCurrentUser) {
      if (lease.blockId) {
        clearLease(lease.blockId)
      }
    }
  }

  async function handleBlockMutation(envelope: RealtimeEnvelope<unknown>) {
    if (!props.pageId) return

    const envelopePageId = getEnvelopePageId(envelope)
    const payload = getEnvelopePayload(envelope)
    const mutation = normalizeMutationPayload(payload)

    if (envelopePageId && envelopePageId !== props.pageId) return
    if (!mutation || mutation.pageId !== props.pageId) return

    const revision = getRevisionFromEnvelope(envelope, mutation.appliedRevision)

    if (revision !== null) {
      if (revision <= currentRevision.value) return

      if (revision !== currentRevision.value + 1) {
        currentRevision.value = revision
        pendingRemoteRefresh = true
        scheduleSoftSync(activeEditorBlockId.value ? 700 : 260)
        return
      }

      currentRevision.value = revision
    }

    if (!mutation.block) return

    serverBlocksById.value[mutation.block.id] = mutation.block
    serverBlockSignatures.value[mutation.block.id] =
      serverBlockSignature(mutation.block)

    const uiBlockId = getEditorBlockIdForServerBlock(mutation.block.id)

    if (uiBlockId === activeEditorBlockId.value || isSaving.value) {
      remoteBlocksPendingUi.add(mutation.block.id)
      pendingRemoteRefresh = true
      return
    }

    const applied = await applyRemoteBlockToEditor(mutation.block)

    if (applied) {
      remoteBlocksPendingUi.delete(mutation.block.id)
      return
    }

    remoteBlocksPendingUi.add(mutation.block.id)
    pendingRemoteRefresh = true
    scheduleSoftSync(SOFT_SYNC_DELAY_MS)
  }

  async function handleBlockDeleted(envelope: RealtimeEnvelope<unknown>) {
    if (!props.pageId) return

    const envelopePageId = getEnvelopePageId(envelope)
    const payload = getEnvelopePayload(envelope)

    if (envelopePageId && envelopePageId !== props.pageId) return

    const deletedIds = normalizeDeletedBlockIds(payload)

    if (deletedIds.length === 0) {
      pendingRemoteRefresh = true
      scheduleSoftSync(SOFT_SYNC_DELAY_MS)
      return
    }

    const activeDeleted = activeEditorBlockId.value
      ? deletedIds.some(
          (id) => getEditorBlockIdForServerBlock(id) === activeEditorBlockId.value
        )
      : false

    for (const id of deletedIds) {
      delete serverBlocksById.value[id]
      delete serverBlockSignatures.value[id]
      clearLease(id)
      clearRemoteTyping(id)
      forgetServerBlockAlias(id)
      remoteBlocksPendingUi.delete(id)
    }

    if (activeDeleted || isSaving.value) {
      pendingRemoteRefresh = true
      return
    }

    for (const id of deletedIds) {
      await deleteEditorBlock(id)
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

    if (!unsubscribeBlockDraftChanged) {
      unsubscribeBlockDraftChanged = realtimeClient.on(
        'BlockDraftChanged',
        handleBlockDraftChanged
      )
    }

    if (!unsubscribeBlockEditingStateChanged) {
      unsubscribeBlockEditingStateChanged = realtimeClient.on(
        'BlockEditingStateChanged',
        handleBlockEditingStateChanged
      )
    }

    if (!unsubscribeBlockLeaseChanged) {
      unsubscribeBlockLeaseChanged = realtimeClient.on(
        'BlockLeaseChanged',
        handleBlockLeaseChanged
      )
    }

    if (!unsubscribePageMousePointerChanged) {
      unsubscribePageMousePointerChanged = realtimeClient.on(
        'PageMousePointerChanged',
        handlePageMousePointerChanged
      )
    }
  }

  function unbindRealtimeEvents() {
    unsubscribeBlockCreated?.()
    unsubscribeBlockUpdated?.()
    unsubscribeBlockDeleted?.()
    unsubscribeBlockDraftChanged?.()
    unsubscribeBlockEditingStateChanged?.()
    unsubscribeBlockLeaseChanged?.()
    unsubscribePageMousePointerChanged?.()

    unsubscribeBlockCreated = null
    unsubscribeBlockUpdated = null
    unsubscribeBlockDeleted = null
    unsubscribeBlockDraftChanged = null
    unsubscribeBlockEditingStateChanged = null
    unsubscribeBlockLeaseChanged = null
    unsubscribePageMousePointerChanged = null
  }

  async function joinRealtimePage() {
    if (!props.pageId) return

    try {
      await realtimeClient.start()
      await realtimeClient.joinPage(props.pageId)
      bindRealtimeEvents()

      if (heartbeatTimer) {
        window.clearInterval(heartbeatTimer)
      }

      heartbeatTimer = window.setInterval(() => {
        if (!props.pageId) return

        void realtimeClient.heartbeatPage(props.pageId).catch(() => {})
      }, 15000)
    } catch {
      // realtime lỗi thì editor vẫn phải dùng được
    }
  }

  async function leaveRealtimePage(pageId: Guid | null) {
    if (!pageId) return

    if (heartbeatTimer) {
      window.clearInterval(heartbeatTimer)
      heartbeatTimer = null
    }

    handleEditorPointerLeave()

    try {
      await realtimeClient.leavePage(pageId)
    } catch {
      // ignore
    }
  }

  async function loadPage() {
    if (!props.pageId) return

    isLoading.value = true
    error.value = null

    try {
      const result = await blockController.listByPage(props.pageId)

      if (!result.isSuccess || !result.data) {
        throw new Error(getApiResultErrorMessage(result, 'Không tải được page.'))
      }

      hydrateServerSnapshot(result.data)
      await createEditor(documentToEditorData(result.data))
      await joinRealtimePage()
    } catch (loadError) {
      error.value = getApiErrorMessage(loadError, 'Không tải được editor.')
    } finally {
      isLoading.value = false
    }
  }

  function rememberTextSelection() {
    window.setTimeout(() => {
      const selection = window.getSelection()

      if (!selection || selection.rangeCount === 0 || selection.isCollapsed) {
        isTextToolbarVisible.value = false
        return
      }

      const range = selection.getRangeAt(0)
      const commonAncestor = range.commonAncestorContainer

      if (!holderRef.value?.contains(commonAncestor)) {
        isTextToolbarVisible.value = false
        return
      }

      selectedRange.value = range.cloneRange()

      const rect = range.getBoundingClientRect()

      textToolbarStyle.value = {
        top: `${Math.max(12, rect.top - 52 + window.scrollY)}px`,
        left: `${Math.max(12, rect.left + rect.width / 2 - 180 + window.scrollX)}px`,
      }

      isTextToolbarVisible.value = true
    }, 0)
  }

  function keepTextToolbarOpen() {
    if (selectedRange.value) {
      isTextToolbarVisible.value = true
    }
  }

  function restoreSelection() {
    if (!selectedRange.value) return false

    const selection = window.getSelection()
    if (!selection) return false

    selection.removeAllRanges()
    selection.addRange(selectedRange.value)
    return true
  }

  async function applyStyleToSelection(styles: Record<string, string>) {
    if (!restoreSelection()) return

    const selection = window.getSelection()
    if (!selection || selection.rangeCount === 0) return

    const range = selection.getRangeAt(0)
    if (range.collapsed) return

    const span = document.createElement('span')
    span.dataset.editorInlineStyle = 'true'

    for (const [key, value] of Object.entries(styles)) {
      span.style.setProperty(key, value)
    }

    try {
      range.surroundContents(span)
    } catch {
      const fragment = range.extractContents()
      span.appendChild(fragment)
      range.insertNode(span)
    }

    selection.removeAllRanges()
    const nextRange = document.createRange()
    nextRange.selectNodeContents(span)
    selection.addRange(nextRange)
    selectedRange.value = nextRange.cloneRange()

    scheduleSave(300)
  }

  function applyFontFamily() {
    if (!selectedFontFamily.value) return

    void applyStyleToSelection({
      'font-family': selectedFontFamily.value,
    })

    selectedFontFamily.value = ''
  }

  function applyFontSize() {
    if (!selectedFontSize.value) return

    void applyStyleToSelection({
      'font-size': selectedFontSize.value,
      'line-height': '1.65',
    })

    selectedFontSize.value = ''
  }

  function applyTextColor() {
    void applyStyleToSelection({
      color: selectedTextColor.value,
    })
  }

  function applyHighlightColor() {
    void applyStyleToSelection({
      'background-color': selectedHighlightColor.value,
      'border-radius': '4px',
      padding: '0.02em 0.18em',
    })
  }

  function toggleBold() {
    if (!restoreSelection()) return
    document.execCommand('bold')
    rememberTextSelection()
    scheduleSave(300)
  }

  function toggleItalic() {
    if (!restoreSelection()) return
    document.execCommand('italic')
    rememberTextSelection()
    scheduleSave(300)
  }

  function clearInlineStyle() {
    if (!restoreSelection()) return
    document.execCommand('removeFormat')
    rememberTextSelection()
    scheduleSave(300)
  }

  async function resetForPageChange(oldPageId: Guid | null) {
    if (saveTimer) {
      window.clearTimeout(saveTimer)
      saveTimer = null
    }

    if (softSyncTimer) {
      window.clearTimeout(softSyncTimer)
      softSyncTimer = null
    }

    if (releaseBlockTimer) {
      window.clearTimeout(releaseBlockTimer)
      releaseBlockTimer = null
    }

    if (pointerSendTimer) {
      window.clearTimeout(pointerSendTimer)
      pointerSendTimer = null
    }

    await releaseAllLeases()
    await leaveRealtimePage(oldPageId)
    unbindRealtimeEvents()

    localToServerBlockIds.clear()
    serverToLocalBlockIds.clear()
    remoteBlocksPendingUi.clear()
    remotePointerMap.clear()
    remotePointerTimers.clear()
    remoteTypingTimers.clear()
    remotePointers.value = []

    activeEditorBlockId.value = null
    pendingRemoteRefresh = false
    error.value = null
  }

  watch(
    () => props.pageId,
    async (nextPageId, oldPageId) => {
      await resetForPageChange(oldPageId ?? null)

      if (!nextPageId) {
        await destroyEditor()
        return
      }

      await loadPage()
    }
  )

  onMounted(() => {
    void loadPage()
  })

  onBeforeUnmount(() => {
    isDestroyed = true

    if (saveTimer) {
      window.clearTimeout(saveTimer)
      saveTimer = null
    }

    if (softSyncTimer) {
      window.clearTimeout(softSyncTimer)
      softSyncTimer = null
    }

    if (releaseBlockTimer) {
      window.clearTimeout(releaseBlockTimer)
      releaseBlockTimer = null
    }

    if (pointerSendTimer) {
      window.clearTimeout(pointerSendTimer)
      pointerSendTimer = null
    }

    if (heartbeatTimer) {
      window.clearInterval(heartbeatTimer)
      heartbeatTimer = null
    }

    for (const timer of remotePointerTimers.values()) {
      window.clearTimeout(timer)
    }

    for (const timer of remoteTypingTimers.values()) {
      window.clearTimeout(timer)
    }

    handleEditorPointerLeave()
    unbindRealtimeEvents()

    void releaseAllLeases()
    void leaveRealtimePage(props.pageId)
    void destroyEditor()
  })

  return {
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
  }
}