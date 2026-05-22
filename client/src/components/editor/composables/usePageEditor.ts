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

interface LeaseState {
  blockId: Guid
  editorSessionId: string
  expiresAtUtc?: string | null
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
const SAVE_DEBOUNCE_MS = 850
const REMOTE_DRAFT_THROTTLE_MS = 120
const RELEASE_LEASE_DELAY_MS = 4500
const LEASE_RENEW_INTERVAL_MS = 7000
const HEARTBEAT_INTERVAL_MS = 15000
const POINTER_THROTTLE_MS = 70
const REMOTE_POINTER_TTL_MS = 300
const REMOTE_TYPING_TTL_MS = 650
const MUTED_AFTER_REMOTE_MS = 900
const REMOTE_QUEUE_IDLE_MS = 850

const TEXT_BLOCK_HTML_SELECTORS: Record<string, string[]> = {
  paragraph: ['.ce-paragraph'],
  header: ['.ce-header'],
  quote: ['.cdx-quote__text'],
  warning: ['.cdx-warning__message'],
}

const CODE_BLOCK_SELECTORS = ['.ce-code__textarea', 'textarea', 'pre', 'code']

const INLINE_STYLE_ALLOWED_PROPERTIES = new Set([
  'font-family',
  'font-size',
  'line-height',
  'color',
  'background-color',
  'border-radius',
  'padding',
  'font-weight',
  'font-style',
  'text-decoration',
])

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

function asString(value: unknown, fallback = '') {
  return typeof value === 'string' ? value : fallback
}

function asNullableString(value: unknown) {
  if (typeof value !== 'string') return null
  const trimmed = value.trim()
  return trimmed ? trimmed : null
}

function asGuid(value: unknown) {
  return asNullableString(value) as Guid | null
}

function asNumber(value: unknown, fallback = 0) {
  if (typeof value === 'number' && Number.isFinite(value)) return value

  if (typeof value === 'string' && value.trim()) {
    const parsed = Number(value)
    if (Number.isFinite(parsed)) return parsed
  }

  return fallback
}

function readCase(raw: Record<string, unknown>, camelKey: string, pascalKey: string) {
  return raw[camelKey] ?? raw[pascalKey]
}

function normalizeBlockResponse(value: unknown): BlockResponse | null {
  const raw = getObject(value)
  if (!raw) return null

  const id = asGuid(readCase(raw, 'id', 'Id'))
  const pageId = asGuid(readCase(raw, 'pageId', 'PageId'))
  const type = asString(readCase(raw, 'type', 'Type'), 'paragraph')
  const orderKey = asString(readCase(raw, 'orderKey', 'OrderKey'), 'U')
  const createdBy = asGuid(readCase(raw, 'createdBy', 'CreatedBy')) ?? ''

  if (!id || !pageId) return null

  return {
    id,
    pageId,
    parentBlockId: asGuid(readCase(raw, 'parentBlockId', 'ParentBlockId')),
    type,
    textContent:
      asNullableString(readCase(raw, 'textContent', 'TextContent')) ??
      (readCase(raw, 'textContent', 'TextContent') === '' ? '' : null),
    propsJson:
      asNullableString(readCase(raw, 'propsJson', 'PropsJson')) ??
      (readCase(raw, 'propsJson', 'PropsJson') === '' ? '' : null),
    schemaVersion: asNumber(readCase(raw, 'schemaVersion', 'SchemaVersion'), 1),
    orderKey,
    createdBy,
    lastModifiedBy: asGuid(readCase(raw, 'lastModifiedBy', 'LastModifiedBy')),
    createdDate: asString(readCase(raw, 'createdDate', 'CreatedDate'), ''),
    updatedDate: asNullableString(readCase(raw, 'updatedDate', 'UpdatedDate')),
  }
}

function normalizeMutationPayload(payload: unknown): BlockMutationResponse | null {
  const raw = getObject(payload)
  if (!raw) return null

  const block = normalizeBlockResponse(readCase(raw, 'block', 'Block'))
  const rawRevision = readCase(raw, 'appliedRevision', 'AppliedRevision')
  const appliedRevision = asNumber(rawRevision, Number.NaN)

  const pageId =
    asGuid(readCase(raw, 'pageId', 'PageId')) ??
    block?.pageId ??
    null

  const blockId =
    asGuid(readCase(raw, 'blockId', 'BlockId')) ??
    block?.id ??
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

  const ids = readCase(raw, 'deletedBlockIds', 'DeletedBlockIds')
  if (!Array.isArray(ids)) return []

  return ids.filter(
    (id): id is Guid => typeof id === 'string' && id.trim().length > 0
  )
}

function getEnvelopePageId(envelope: unknown): Guid | null {
  const raw = getObject(envelope)
  if (!raw) return null
  return asGuid(readCase(raw, 'pageId', 'PageId'))
}

function getEnvelopePayload(envelope: unknown) {
  const raw = getObject(envelope)
  if (!raw) return envelope
  return raw.payload ?? raw.Payload ?? envelope
}

function getEnvelopeRevision(
  envelope: RealtimeEnvelope<unknown>,
  fallback?: number | null
) {
  if (typeof envelope.revision === 'number') return envelope.revision
  if (typeof fallback === 'number') return fallback
  return null
}

function plainTextFromHtml(value: string) {
  const element = document.createElement('div')
  element.innerHTML = value
  return element.textContent ?? element.innerText ?? ''
}

function escapeHtml(value: string) {
  const element = document.createElement('div')
  element.textContent = value
  return element.innerHTML
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

function getDataObject(block: EditorBlockData) {
  return getObject(block.data) ?? {}
}

function normalizeChecklistItems(items: unknown) {
  if (!Array.isArray(items)) return []

  return items.map((item) => {
    const raw = getObject(item)

    if (!raw) {
      return {
        text: typeof item === 'string' ? item : '',
        checked: false,
      }
    }

    return {
      text: asString(raw.text ?? raw.content, ''),
      checked: raw.checked === true,
    }
  })
}

function normalizeListItems(items: unknown) {
  if (!Array.isArray(items)) return []

  return items.map((item) => {
    if (typeof item === 'string') return item

    const raw = getObject(item)
    if (!raw) return ''

    return {
      ...raw,
      content: asString(raw.content ?? raw.text, ''),
      items: Array.isArray(raw.items) ? raw.items : [],
    }
  })
}

function normalizeTableContent(content: unknown) {
  if (!Array.isArray(content)) return []

  return content.map((row) => {
    if (!Array.isArray(row)) return []
    return row.map((cell) => String(cell ?? ''))
  })
}

function normalizeEditorBlock(block: EditorBlockData): EditorBlockData {
  const type = block.type || 'paragraph'
  const data = getDataObject(block)

  if (type === 'paragraph') {
    return {
      ...block,
      type,
      data: {
        text: asString(data.text, ''),
      },
    } as EditorBlockData
  }

  if (type === 'header') {
    const level = asNumber(data.level, 2)

    return {
      ...block,
      type,
      data: {
        text: asString(data.text, ''),
        level: Math.max(1, Math.min(6, level)),
      },
    } as EditorBlockData
  }

  if (type === 'quote') {
    return {
      ...block,
      type,
      data: {
        text: asString(data.text, ''),
        caption: asString(data.caption, ''),
        alignment: asString(data.alignment, 'left'),
      },
    } as EditorBlockData
  }

  if (type === 'warning') {
    return {
      ...block,
      type,
      data: {
        title: asString(data.title, ''),
        message: asString(data.message, ''),
      },
    } as EditorBlockData
  }

  if (type === 'code') {
    return {
      ...block,
      type,
      data: {
        code: asString(data.code, ''),
      },
    } as EditorBlockData
  }

  if (type === 'checklist') {
    return {
      ...block,
      type,
      data: {
        items: normalizeChecklistItems(data.items),
      },
    } as EditorBlockData
  }

  if (type === 'list') {
    return {
      ...block,
      type,
      data: {
        style: asString(data.style, 'unordered'),
        items: normalizeListItems(data.items),
      },
    } as EditorBlockData
  }

  if (type === 'table') {
    return {
      ...block,
      type,
      data: {
        withHeadings: data.withHeadings === true,
        content: normalizeTableContent(data.content),
      },
    } as EditorBlockData
  }

  if (type === 'delimiter') {
    return {
      ...block,
      type,
      data: {},
    } as EditorBlockData
  }

  return {
    ...block,
    type,
    data,
  } as EditorBlockData
}

function normalizeEditorData(data: OutputData): OutputData {
  return {
    time: data.time ?? Date.now(),
    version: data.version ?? '2.31.0',
    blocks: (data.blocks ?? []).map((block) => normalizeEditorBlock(block)),
  }
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
    return normalizeEditorBlock({
      id: editorBlockId,
      type: stored.type,
      data: stored.data ?? {},
      tunes: stored.tunes ?? undefined,
    } as EditorBlockData)
  }

  if (block.type === 'editorjs' && block.propsJson) {
    const legacy = safeJsonParse<OutputData>(block.propsJson)

    if (legacy?.blocks?.[0]) {
      return normalizeEditorBlock({
        ...legacy.blocks[0],
        id: editorBlockId,
      } as EditorBlockData)
    }
  }

  return normalizeEditorBlock({
    id: editorBlockId,
    type: block.type === 'paragraph' ? 'paragraph' : block.type,
    data: {
      text: block.textContent ?? '',
    },
  } as EditorBlockData)
}

function documentToEditorData(document: PageDocumentResponse): OutputData {
  return normalizeEditorData({
    time: Date.now(),
    version: '2.31.0',
    blocks: sortBackendBlocks(document.blocks).map((block) =>
      backendBlockToEditorBlock(block)
    ),
  })
}

function plainTextFromEditorBlock(block: EditorBlockData) {
  const data = getDataObject(block)

  if (typeof data.text === 'string') {
    return plainTextFromHtml(data.text).slice(0, 2000)
  }

  if (Array.isArray(data.items)) {
    return data.items
      .map((item) => {
        if (typeof item === 'string') return plainTextFromHtml(item)

        const raw = getObject(item)
        if (!raw) return ''

        return plainTextFromHtml(String(raw.text ?? raw.content ?? ''))
      })
      .filter(Boolean)
      .join(' ')
      .slice(0, 2000)
  }

  if (typeof data.code === 'string') return data.code.slice(0, 2000)
  if (typeof data.message === 'string') return plainTextFromHtml(data.message).slice(0, 2000)
  if (typeof data.title === 'string') return plainTextFromHtml(data.title).slice(0, 2000)

  return ''
}

function blockToPropsJson(block: EditorBlockData) {
  const safeBlock = normalizeEditorBlock(block)

  const payload: StoredEditorJsBlockProps = {
    editorjs: {
      type: safeBlock.type,
      data: (safeBlock.data ?? {}) as Record<string, unknown>,
      tunes: (safeBlock.tunes ?? null) as Record<string, unknown> | null,
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
  const safeBlock = normalizeEditorBlock(block)

  return JSON.stringify({
    type: safeBlock.type,
    textContent: plainTextFromEditorBlock(safeBlock),
    propsJson: blockToPropsJson(safeBlock),
  })
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

  const leasesByBlock = new Map<Guid, LeaseState>()
  const localToServerBlockIds = new Map<string, Guid>()
  const serverToLocalBlockIds = new Map<Guid, string>()
  const remoteEditorsByBlock = new Map<Guid, string>()
  const pendingRemoteBlocks = new Map<Guid, BlockResponse>()
  const pendingRemoteDeletedIds = new Set<Guid>()

  const remotePointerMap = new Map<string, RemotePointerView>()
  const remotePointerTimers = new Map<string, number>()
  const remoteTypingTimers = new Map<Guid, number>()

  const activeEditorBlockId = ref<string | null>(null)

  let saveTimer: number | null = null
  let draftTimer: number | null = null
  let releaseTimer: number | null = null
  let remoteQueueTimer: number | null = null
  let pointerSendTimer: number | null = null
  let heartbeatTimer: number | null = null
  let leaseRenewTimer: number | null = null
  let localSaveMutedUntil = 0
  let isDestroyed = false
  let lastLoadedPageId: Guid | null = null

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
  let unsubscribePagePresenceChanged: (() => void) | null = null

  function getEditorSessionId() {
    return realtimeClient.state.connectionId ?? `local-${holderId}`
  }

  function getCurrentUserDisplayName() {
    return 'Bạn'
  }

  function muteLocalSaves(durationMs = MUTED_AFTER_REMOTE_MS) {
    localSaveMutedUntil = Math.max(localSaveMutedUntil, Date.now() + durationMs)

    if (saveTimer) {
      window.clearTimeout(saveTimer)
      saveTimer = null
    }
  }

  function isLocalSaveMuted() {
    return isApplyingRemote.value || Date.now() < localSaveMutedUntil
  }

  async function runAsRemoteMutation<T>(callback: () => Promise<T> | T) {
    isApplyingRemote.value = true
    muteLocalSaves(MUTED_AFTER_REMOTE_MS)

    try {
      return await callback()
    } finally {
      window.setTimeout(() => {
        isApplyingRemote.value = false
        muteLocalSaves(350)
      }, 80)
    }
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

  function getActiveServerBlockId() {
    return resolveServerBlockId(activeEditorBlockId.value)
  }

  function isActiveServerBlock(blockId: Guid) {
    return getActiveServerBlockId() === blockId
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

  function upsertServerBlock(block: BlockResponse, revision?: number | null) {
    serverBlocksById.value = {
      ...serverBlocksById.value,
      [block.id]: block,
    }

    serverBlockSignatures.value = {
      ...serverBlockSignatures.value,
      [block.id]: serverBlockSignature(block),
    }

    if (typeof revision === 'number' && revision > currentRevision.value) {
      currentRevision.value = revision
    }
  }

  function removeServerBlock(blockId: Guid, revision?: number | null) {
    const nextBlocks = { ...serverBlocksById.value }
    const nextSignatures = { ...serverBlockSignatures.value }

    delete nextBlocks[blockId]
    delete nextSignatures[blockId]

    serverBlocksById.value = nextBlocks
    serverBlockSignatures.value = nextSignatures

    if (typeof revision === 'number' && revision > currentRevision.value) {
      currentRevision.value = revision
    }

    forgetServerBlockAlias(blockId)
    leasesByBlock.delete(blockId)
    remoteEditorsByBlock.delete(blockId)
    clearRemoteTyping(blockId)
  }

  function markLeaseHeld(blockId: Guid, editorSessionId: string, expiresAtUtc?: string | null) {
    // Backend chỉ cho 1 connection giữ 1 block. Khi acquire block mới, local phải quên block cũ.
    for (const [heldBlockId, lease] of leasesByBlock.entries()) {
      if (heldBlockId !== blockId && lease.editorSessionId === editorSessionId) {
        leasesByBlock.delete(heldBlockId)
      }
    }

    leasesByBlock.set(blockId, {
      blockId,
      editorSessionId,
      expiresAtUtc,
    })
  }

  function clearLease(blockId: Guid) {
    leasesByBlock.delete(blockId)
  }

  function hasCurrentLease(blockId: Guid) {
    const lease = leasesByBlock.get(blockId)
    if (!lease) return false

    return lease.editorSessionId === getEditorSessionId()
  }

  async function renewLease(blockId: Guid) {
    const lease = leasesByBlock.get(blockId)
    const editorSessionId = lease?.editorSessionId ?? getEditorSessionId()

    try {
      const result = await blockController.renewLease(blockId, { editorSessionId })

      if (result.isSuccess && result.data?.granted) {
        markLeaseHeld(blockId, editorSessionId, result.data.expiresAtUtc)
        return true
      }
    } catch {
      // Lease hết hạn/server restart: clear local để lần save sau acquire lại.
    }

    clearLease(blockId)
    return false
  }

  async function renewHeldLeases() {
    for (const blockId of [...leasesByBlock.keys()]) {
      await renewLease(blockId)
    }
  }

  function startLeaseRenewal() {
    if (leaseRenewTimer) {
      window.clearInterval(leaseRenewTimer)
    }

    leaseRenewTimer = window.setInterval(() => {
      void renewHeldLeases().catch(() => {})
    }, LEASE_RENEW_INTERVAL_MS)
  }

  function stopLeaseRenewal() {
    if (!leaseRenewTimer) return
    window.clearInterval(leaseRenewTimer)
    leaseRenewTimer = null
  }

  async function ensureBlockLease(blockId: Guid) {
    if (hasCurrentLease(blockId)) {
      const renewed = await renewLease(blockId)
      if (renewed) return
    }

    if (remoteEditorsByBlock.has(blockId)) {
      throw new Error(`${remoteEditorsByBlock.get(blockId)} đang sửa block này.`)
    }

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

    markLeaseHeld(blockId, editorSessionId, result.data.expiresAtUtc)

    if (props.pageId) {
      await realtimeClient
        .sendBlockEditingState({
          pageId: props.pageId,
          blockId,
          editorSessionId,
          isEditing: true,
        })
        .catch(() => {})
    }
  }

  async function releaseBlockLease(blockId: Guid) {
    const lease = leasesByBlock.get(blockId)
    if (!lease) return

    try {
      await blockController.releaseLease(blockId, {
        editorSessionId: lease.editorSessionId,
      })

      if (props.pageId) {
        await realtimeClient
          .sendBlockEditingState({
            pageId: props.pageId,
            blockId,
            editorSessionId: lease.editorSessionId,
            isEditing: false,
          })
          .catch(() => {})
      }
    } finally {
      clearLease(blockId)
    }
  }

  async function releaseAllLeases() {
    await Promise.allSettled([...leasesByBlock.keys()].map((blockId) => releaseBlockLease(blockId)))
  }

  function getClosestEditorBlock(node: EventTarget | Node | null) {
    if (!(node instanceof Node)) return null

    const element = node instanceof HTMLElement ? node : node.parentElement
    return element?.closest?.('.ce-block') ?? null
  }

  function getEditorBlockIdFromEvent(event: Event) {
    const blockElement = getClosestEditorBlock(event.target)
    return blockElement instanceof HTMLElement ? blockElement.dataset.id ?? null : null
  }

  function getEditorBlockIdFromNode(node: Node | null) {
    const blockElement = getClosestEditorBlock(node)
    return blockElement instanceof HTMLElement ? blockElement.dataset.id ?? null : null
  }

  function getEditorBlockElementByEditorId(editorBlockId: string | null | undefined) {
    if (!holderRef.value || !editorBlockId) return null

    return holderRef.value.querySelector<HTMLElement>(
      `.ce-block[data-id="${cssEscape(editorBlockId)}"]`
    )
  }

  function getBlockElementByServerId(serverBlockId: Guid) {
    return getEditorBlockElementByEditorId(getEditorBlockIdForServerBlock(serverBlockId))
  }

  function isHolderFocused() {
    const active = document.activeElement
    return !!active && !!holderRef.value?.contains(active)
  }

  function shouldQueueStructuralRemote(blockId?: Guid | null) {
    if (isSaving.value) return true
    if (!isHolderFocused()) return false

    if (!blockId) return true
    if (isActiveServerBlock(blockId)) return true
    if (hasCurrentLease(blockId)) return true

    return false
  }

  function sanitizeInlineStyleAttribute(styleValue: string | null) {
    if (!styleValue) return ''

    const probe = document.createElement('span')
    probe.setAttribute('style', styleValue)

    const kept: string[] = []

    for (const property of INLINE_STYLE_ALLOWED_PROPERTIES) {
      const value = probe.style.getPropertyValue(property)

      if (value) {
        kept.push(`${property}: ${value}`)
      }
    }

    return kept.join('; ')
  }

  function normalizeInlineHtml(html: string) {
    const template = document.createElement('template')
    template.innerHTML = html

    template.content.querySelectorAll<HTMLElement>('[style]').forEach((element) => {
      const style = sanitizeInlineStyleAttribute(element.getAttribute('style'))

      if (style) {
        element.setAttribute('style', style)
      } else {
        element.removeAttribute('style')
      }
    })

    template.content.querySelectorAll<HTMLElement>('span[data-editor-inline-style]').forEach(
      (element) => {
        element.classList.add('editor-inline-style')
      }
    )

    return template.innerHTML
  }

  function getBlockInnerHtml(editorBlockId: string | undefined, selectors: string[]) {
    const blockElement = getEditorBlockElementByEditorId(editorBlockId)
    if (!blockElement) return null

    for (const selector of selectors) {
      const element = blockElement.querySelector<HTMLElement>(selector)

      if (element) {
        return normalizeInlineHtml(element.innerHTML)
      }
    }

    return null
  }

  function hydrateInlineHtmlFromDom(data: OutputData): OutputData {
    return normalizeEditorData({
      ...data,
      blocks: data.blocks.map((block) => {
        const selectors = TEXT_BLOCK_HTML_SELECTORS[block.type]
        if (!selectors) return block

        const html = getBlockInnerHtml(block.id, selectors)
        if (html === null) return block

        return {
          ...block,
          data: {
            ...(block.data ?? {}),
            text: html,
          },
        } as EditorBlockData
      }),
    })
  }

  async function saveEditorSnapshot() {
    if (!editor.value || !isReady.value) return defaultEditorData()

    const data = await editor.value.save()
    return hydrateInlineHtmlFromDom(data)
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
      data: normalizeEditorData(data),
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
      onChange: () => {
        if (
          !canEditDocument.value ||
          isLocalSaveMuted() ||
          isLoading.value ||
          !isReady.value
        ) {
          return
        }

        scheduleSave(SAVE_DEBOUNCE_MS)
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

      // Chỉ save block mà user đang active hoặc mình đang giữ lease.
      // Remote DOM patch của block khác không được save ngược.
      if (!hasCurrentLease(serverBlockId) && block.id !== activeEditorBlockId.value) {
        return false
      }

      return editorBlockSignature(block) !== serverBlockSignatures.value[serverBlockId]
    })
  }

  function reconcileEditorBlockAliases(data: OutputData) {
    const serverBlocks = sortBackendBlocks(Object.values(serverBlocksById.value))
    const usedServerIds = new Set<Guid>()

    for (const block of data.blocks) {
      const serverBlockId = resolveServerBlockId(block.id)

      if (serverBlockId) {
        usedServerIds.add(serverBlockId)
      }
    }

    data.blocks.forEach((editorBlock, index) => {
      if (!editorBlock.id || resolveServerBlockId(editorBlock.id)) return

      const editorSignature = editorBlockSignature(editorBlock)
      const sameIndexServerBlock = serverBlocks[index]

      if (
        sameIndexServerBlock &&
        !usedServerIds.has(sameIndexServerBlock.id) &&
        serverBlockSignature(sameIndexServerBlock) === editorSignature
      ) {
        rememberBlockAlias(editorBlock.id, sameIndexServerBlock.id)
        usedServerIds.add(sameIndexServerBlock.id)
        return
      }

      const candidates = serverBlocks.filter((serverBlock) => {
        if (usedServerIds.has(serverBlock.id)) return false
        return serverBlockSignature(serverBlock) === editorSignature
      })

      if (candidates.length !== 1) return

      const candidate = candidates[0]
      if (!candidate) return

      rememberBlockAlias(editorBlock.id, candidate.id)
      usedServerIds.add(candidate.id)
    })
  }

  function getCreatedBlocks(data: OutputData) {
    reconcileEditorBlockAliases(data)

    return data.blocks.filter((block) => !resolveServerBlockId(block.id))
  }

  function getDeletedBlockIds(data: OutputData) {
    const currentServerIds = new Set(
      data.blocks
        .map((block) => resolveServerBlockId(block.id))
        .filter((id): id is Guid => typeof id === 'string' && id.trim().length > 0)
    )

    return Object.keys(serverBlocksById.value).filter((serverId) => {
      if (pendingRemoteDeletedIds.has(serverId)) return false
      if (isActiveServerBlock(serverId)) return false
      return !currentServerIds.has(serverId)
    })
  }

  async function syncEditorDataToServer(data: OutputData) {
    if (!props.pageId) return

    const safeData = normalizeEditorData(data)

    reconcileEditorBlockAliases(safeData)

    const createdBlocks = getCreatedBlocks(safeData)
    const changedExistingBlocks = getChangedExistingBlocks(safeData)
    const deletedBlockIds = getDeletedBlockIds(safeData)

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

      for (const editorBlock of safeData.blocks) {
        const existingServerId = resolveServerBlockId(editorBlock.id)

        if (existingServerId) {
          previousBlockId = existingServerId
          continue
        }

        const safeBlock = normalizeEditorBlock(editorBlock)

        const result = await blockController.create(props.pageId, {
          expectedRevision: currentRevision.value,
          type: safeBlock.type,
          textContent: plainTextFromEditorBlock(safeBlock),
          propsJson: blockToPropsJson(safeBlock),
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
        upsertServerBlock(result.data.block, result.data.appliedRevision)
      }

      for (const editorBlock of changedExistingBlocks) {
        const serverBlockId = resolveServerBlockId(editorBlock.id)
        if (!serverBlockId) continue

        const safeBlock = normalizeEditorBlock(editorBlock)

        await ensureBlockLease(serverBlockId)

        const activeSessionId =
          leasesByBlock.get(serverBlockId)?.editorSessionId ?? getEditorSessionId()

        const result = await blockController.update(serverBlockId, {
          expectedRevision: currentRevision.value,
          editorSessionId: activeSessionId,
          type: safeBlock.type,
          textContent: plainTextFromEditorBlock(safeBlock),
          propsJson: blockToPropsJson(safeBlock),
        })

        if (!result.isSuccess || !result.data?.block) {
          throw new Error(getApiResultErrorMessage(result, 'Không lưu được block.'))
        }

        markLeaseHeld(result.data.block.id, activeSessionId)
        rememberBlockAlias(editorBlock.id, result.data.block.id)
        upsertServerBlock(result.data.block, result.data.appliedRevision)
      }

      for (const blockId of deletedBlockIds) {
        await ensureBlockLease(blockId)

        const activeSessionId = leasesByBlock.get(blockId)?.editorSessionId ?? getEditorSessionId()

        const result = await blockController.delete(blockId, {
          expectedRevision: currentRevision.value,
          editorSessionId: activeSessionId,
          note: null,
        })

        if (!result.isSuccess) {
          throw new Error(getApiResultErrorMessage(result, 'Không xóa được block.'))
        }

        removeServerBlock(
          blockId,
          typeof result.data?.appliedRevision === 'number'
            ? result.data.appliedRevision
            : currentRevision.value + 1
        )
      }

      error.value = null
    } finally {
      isSaving.value = false
    }
  }

  async function saveEditorData() {
    if (
      !props.pageId ||
      !editor.value ||
      !isReady.value ||
      isLocalSaveMuted() ||
      isLoading.value
    ) {
      return
    }

    try {
      const data = await saveEditorSnapshot()
      await syncEditorDataToServer(data)
    } catch (saveError: any) {
      if (saveError?.status === 409 || saveError?.data?.statusCode === 409) {
        error.value = getApiErrorMessage(
          saveError,
          'Block đang bị khóa hoặc lease đã hết hạn.'
        )

        const activeServerBlockId = getActiveServerBlockId()
        if (activeServerBlockId) {
          clearLease(activeServerBlockId)
        }

        // Chỉ sync metadata + patch DOM mềm, không render lại editor.
        scheduleSoftSync(600)
        return
      }

      error.value = getApiErrorMessage(saveError, 'Không lưu được nội dung page.')
    }
  }

  function scheduleSave(delay = SAVE_DEBOUNCE_MS) {
    if (isLocalSaveMuted() || isDestroyed) return

    if (saveTimer) {
      window.clearTimeout(saveTimer)
    }

    saveTimer = window.setTimeout(() => {
      saveTimer = null
      void saveEditorData()
    }, delay)
  }

  function isRemoteEditing(blockId: Guid) {
    return remoteEditorsByBlock.has(blockId) && !hasCurrentLease(blockId)
  }

  function prepareBlockForEdit(editorBlockId: string, silent = false) {
    activeEditorBlockId.value = editorBlockId

    const serverBlockId = resolveServerBlockId(editorBlockId)

    if (!serverBlockId) {
      scheduleSave(SAVE_DEBOUNCE_MS)
      return
    }

    if (hasCurrentLease(serverBlockId)) return

    if (isRemoteEditing(serverBlockId)) {
      if (!silent) {
        error.value = `${remoteEditorsByBlock.get(serverBlockId)} đang sửa block này.`
      }

      return
    }

    void ensureBlockLease(serverBlockId).catch((leaseError) => {
      if (!silent) {
        error.value = getApiErrorMessage(
          leaseError,
          'Block này đang được người khác chỉnh sửa.'
        )
      }
    })
  }

  function scheduleReleaseBlockLease(editorBlockId: string) {
    const serverBlockId = resolveServerBlockId(editorBlockId)
    if (!serverBlockId) return

    if (releaseTimer) {
      window.clearTimeout(releaseTimer)
      releaseTimer = null
    }

    releaseTimer = window.setTimeout(() => {
      releaseTimer = null

      if (activeEditorBlockId.value === editorBlockId) return
      if (!hasCurrentLease(serverBlockId)) return

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

  function blockInputShouldBeBlocked(editorBlockId: string | null) {
    const serverBlockId = resolveServerBlockId(editorBlockId)
    if (!serverBlockId) return false
    return isRemoteEditing(serverBlockId)
  }

  function handleEditorFocusIn(event: FocusEvent) {
    const blockId = getEditorBlockIdFromEvent(event)
    if (!blockId) return

    const previousBlockId = activeEditorBlockId.value
    activeEditorBlockId.value = blockId

    if (previousBlockId && previousBlockId !== blockId) {
      scheduleReleaseBlockLease(previousBlockId)
    }

    // Acquire nhẹ khi focus để lần gõ đầu không dính 409, nhưng không spam banner nếu fail.
    prepareBlockForEdit(blockId, true)
  }

  function handleEditorBeforeInput(event: InputEvent) {
    if (!canEditDocument.value || isApplyingRemote.value || isLoading.value) return
    if (!isEditingBeforeInputIntent(event)) return

    const blockId = getEditorBlockIdFromEvent(event)
    if (!blockId) return

    if (blockInputShouldBeBlocked(blockId)) {
      event.preventDefault()
      const serverBlockId = resolveServerBlockId(blockId)
      error.value = serverBlockId
        ? `${remoteEditorsByBlock.get(serverBlockId)} đang sửa block này.`
        : 'Block này đang được người khác sửa.'
      return
    }

    prepareBlockForEdit(blockId)
  }

  function handleEditorKeydown(event: KeyboardEvent) {
    if (!canEditDocument.value || isApplyingRemote.value || isLoading.value) return
    if (!isEditingKeyboardIntent(event)) return

    const blockId = getEditorBlockIdFromEvent(event)
    if (!blockId) return

    if (blockInputShouldBeBlocked(blockId)) {
      event.preventDefault()
      const serverBlockId = resolveServerBlockId(blockId)
      error.value = serverBlockId
        ? `${remoteEditorsByBlock.get(serverBlockId)} đang sửa block này.`
        : 'Block này đang được người khác sửa.'
      return
    }

    prepareBlockForEdit(blockId)
  }

  function scheduleDraftBroadcast() {
    if (draftTimer) {
      window.clearTimeout(draftTimer)
    }

    draftTimer = window.setTimeout(async () => {
      draftTimer = null

      if (!props.pageId || !editor.value || !isReady.value) return

      const activeServerBlockId = getActiveServerBlockId()
      if (!activeServerBlockId || !hasCurrentLease(activeServerBlockId)) return

      try {
        const data = await saveEditorSnapshot()
        const activeBlock = data.blocks.find(
          (block) => resolveServerBlockId(block.id) === activeServerBlockId
        )

        if (!activeBlock) return

        const safeBlock = normalizeEditorBlock(activeBlock)

        await realtimeClient.sendBlockDraft({
          pageId: props.pageId,
          blockId: activeServerBlockId,
          editorSessionId: getEditorSessionId(),
          baseRevision: currentRevision.value,
          clientSequence: Date.now(),
          type: safeBlock.type,
          textContent: plainTextFromEditorBlock(safeBlock),
          propsJson: blockToPropsJson(safeBlock),
        })
      } catch {
        // Draft fail không được phá flow save thật.
      }
    }, REMOTE_DRAFT_THROTTLE_MS)
  }

  function handleEditorInput() {
    if (!canEditDocument.value || isApplyingRemote.value || isLoading.value) return

    scheduleSave(SAVE_DEBOUNCE_MS)
    scheduleDraftBroadcast()
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

    lastPointerPayload = {
      pageId: props.pageId,
      blockId: resolveServerBlockId(getEditorBlockIdFromEvent(event)),
      x: Math.max(0, Math.min(100, x)),
      y: Math.max(0, Math.min(100, y)),
    }

    if (pointerSendTimer) return

    pointerSendTimer = window.setTimeout(() => {
      pointerSendTimer = null
      if (!lastPointerPayload) return

      void realtimeClient
        .sendMousePointer({
          ...lastPointerPayload,
          color: getOwnPointerColor(),
        })
        .catch(() => {})
    }, POINTER_THROTTLE_MS)
  }

  function handleEditorPointerLeave() {
    if (!props.pageId || realtimeClient.state.status !== 'connected') return

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

  function clearRemoteTyping(blockId: Guid) {
    const timer = remoteTypingTimers.get(blockId)

    if (timer) {
      window.clearTimeout(timer)
      remoteTypingTimers.delete(blockId)
    }

    const element = getBlockElementByServerId(blockId)
    if (!element) return

    element.classList.remove('ce-block--remote-typing')
    element.removeAttribute('data-remote-user')
  }

  function markRemoteTyping(blockId: Guid, userName?: string | null) {
    const element = getBlockElementByServerId(blockId)
    if (!element) return

    element.classList.add('ce-block--remote-typing')
    element.dataset.remoteUser = userName?.trim() || 'Đang sửa'

    const oldTimer = remoteTypingTimers.get(blockId)

    if (oldTimer) {
      window.clearTimeout(oldTimer)
    }

    const timer = window.setTimeout(() => {
      remoteEditorsByBlock.delete(blockId)
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

  function getTextHtmlFromEditorBlock(block: EditorBlockData) {
    const data = getDataObject(block)

    if (typeof data.text === 'string') return normalizeInlineHtml(data.text)
    if (typeof data.message === 'string') return normalizeInlineHtml(data.message)
    if (typeof data.code === 'string') return data.code

    return escapeHtml(plainTextFromEditorBlock(block))
  }

  function setElementHtmlIfChanged(element: HTMLElement, html: string) {
    const normalized = normalizeInlineHtml(html)

    if (element.innerHTML === normalized) return false

    element.innerHTML = normalized
    return true
  }

  function setElementTextIfChanged(element: HTMLElement, text: string) {
    if (element instanceof HTMLTextAreaElement) {
      if (element.value === text) return false
      element.value = text
      return true
    }

    if (element.textContent === text) return false
    element.textContent = text
    return true
  }

  function applyServerBlockToDomOnly(block: BlockResponse) {
    const editorBlock = backendBlockToEditorBlock(
      block,
      getEditorBlockIdForServerBlock(block.id)
    )

    rememberBlockAlias(editorBlock.id, block.id)

    const blockElement = getBlockElementByServerId(block.id)
    if (!blockElement) return false

    const textSelectors = TEXT_BLOCK_HTML_SELECTORS[editorBlock.type]

    if (textSelectors) {
      const html = getTextHtmlFromEditorBlock(editorBlock)

      for (const selector of textSelectors) {
        const element = blockElement.querySelector<HTMLElement>(selector)

        if (element) {
          return setElementHtmlIfChanged(element, html)
        }
      }
    }

    if (editorBlock.type === 'code') {
      const code = asString(getDataObject(editorBlock).code, '')

      for (const selector of CODE_BLOCK_SELECTORS) {
        const element = blockElement.querySelector<HTMLElement>(selector)

        if (element) {
          return setElementTextIfChanged(element, code)
        }
      }
    }

    return false
  }

  async function findEditorBlockIndexByServerId(serverBlockId: Guid) {
    const data = await saveEditorSnapshot()

    return data.blocks.findIndex((block) => {
      return resolveServerBlockId(block.id) === serverBlockId || block.id === serverBlockId
    })
  }

  async function insertEditorBlock(block: BlockResponse) {
    if (!editor.value || !isReady.value) return false

    if (serverToLocalBlockIds.has(block.id) || getBlockElementByServerId(block.id)) {
      return true
    }

    const beforeData = await saveEditorSnapshot().catch(() => null)
    const beforeIds = new Set(
      beforeData?.blocks
        ?.map((item) => item.id)
        .filter(
          (id): id is string =>
            typeof id === 'string' && id.trim().length > 0
        ) ?? []
    )

    const editorBlock = backendBlockToEditorBlock(block, block.id)
    const safeBlock = normalizeEditorBlock(editorBlock)
    const sortedIds = sortBackendBlocks(Object.values(serverBlocksById.value)).map(
      (item) => item.id
    )
    const index = Math.max(0, sortedIds.indexOf(block.id))

    return runAsRemoteMutation(async () => {
      try {
        const editorAny = editor.value as any

        await editorAny.blocks.insert(
          safeBlock.type,
          safeBlock.data ?? {},
          safeBlock.tunes ?? {},
          index,
          false
        )

        await nextTick()
        await new Promise<void>((resolve) =>
          window.requestAnimationFrame(() => resolve())
        )

        const afterData = await saveEditorSnapshot().catch(() => null)
        const targetSignature = serverBlockSignature(block)

        const insertedBlock =
          afterData?.blocks.find((item) => {
            if (!item.id || beforeIds.has(item.id)) return false
            return editorBlockSignature(item) === targetSignature
          }) ??
          afterData?.blocks[index] ??
          null

        if (insertedBlock?.id) {
          rememberBlockAlias(insertedBlock.id, block.id)
        }

        if (afterData) {
          reconcileEditorBlockAliases(afterData)
        }

        return true
      } catch {
        scheduleSoftSync(1000)
        return false
      }
    })
  }

  async function deleteEditorBlock(blockId: Guid) {
    if (!editor.value || !isReady.value) return false

    const index = await findEditorBlockIndexByServerId(blockId)
    if (index < 0) return true

    return runAsRemoteMutation(async () => {
      try {
        const editorAny = editor.value as any
        await editorAny.blocks.delete(index)
        return true
      } catch {
        scheduleSoftSync(1000)
        return false
      }
    })
  }

  function scheduleRemoteQueue(delay = REMOTE_QUEUE_IDLE_MS) {
    if (remoteQueueTimer) {
      window.clearTimeout(remoteQueueTimer)
    }

    remoteQueueTimer = window.setTimeout(() => {
      remoteQueueTimer = null
      void flushRemoteQueue()
    }, delay)
  }

  async function flushRemoteQueue() {
    if (isDestroyed || !editor.value || !isReady.value) return

    if (isHolderFocused() || isSaving.value) {
      scheduleRemoteQueue(REMOTE_QUEUE_IDLE_MS)
      return
    }

    const deletedIds = [...pendingRemoteDeletedIds]
    pendingRemoteDeletedIds.clear()

    for (const id of deletedIds) {
      if (hasCurrentLease(id)) continue
      await deleteEditorBlock(id)
    }

    const blocks = [...pendingRemoteBlocks.values()]
    pendingRemoteBlocks.clear()

    for (const block of blocks) {
      if (hasCurrentLease(block.id)) continue

      const changedDom = applyServerBlockToDomOnly(block)

      if (!changedDom) {
        const exists = !!getBlockElementByServerId(block.id)

        if (!exists) {
          await insertEditorBlock(block)
        }
      }
    }
  }

  function handleRemoteBlockCreated(envelope: RealtimeEnvelope<unknown>) {
    const payload = getEnvelopePayload(envelope)
    const mutation = normalizeMutationPayload(payload)

    if (!mutation?.block || mutation.pageId !== props.pageId) return

    if (
      serverBlocksById.value[mutation.block.id] ||
      getBlockElementByServerId(mutation.block.id)
    ) {
      upsertServerBlock(mutation.block, mutation.appliedRevision)
      return
    }

    upsertServerBlock(mutation.block, mutation.appliedRevision)

    if (shouldQueueStructuralRemote(mutation.block.id)) {
      pendingRemoteBlocks.set(mutation.block.id, mutation.block)
      scheduleRemoteQueue()
      return
    }

    void insertEditorBlock(mutation.block)
  }

  function handleRemoteBlockUpdated(envelope: RealtimeEnvelope<unknown>) {
    const payload = getEnvelopePayload(envelope)
    const mutation = normalizeMutationPayload(payload)

    if (!mutation?.block || mutation.pageId !== props.pageId) return

    const block = mutation.block
    const oldSignature = serverBlockSignatures.value[block.id]

    upsertServerBlock(block, getEnvelopeRevision(envelope, mutation.appliedRevision))

    // Nếu là echo từ chính mình, signature cũ thường đã giống rồi. Không đụng DOM.
    if (oldSignature === serverBlockSignature(block)) return

    if (hasCurrentLease(block.id) || isActiveServerBlock(block.id)) {
      pendingRemoteBlocks.set(block.id, block)
      scheduleRemoteQueue()
      return
    }

    const patched = applyServerBlockToDomOnly(block)

    if (!patched) {
      pendingRemoteBlocks.set(block.id, block)
      scheduleRemoteQueue()
    }
  }

  function handleRemoteBlockDeleted(envelope: RealtimeEnvelope<unknown>) {
    const payload = getEnvelopePayload(envelope)
    const mutation = normalizeMutationPayload(payload)
    const deletedIds = normalizeDeletedBlockIds(payload)
    const pageId = mutation?.pageId ?? getEnvelopePageId(envelope)

    if (!pageId || pageId !== props.pageId) return

    const ids =
      deletedIds.length > 0
        ? deletedIds
        : mutation?.blockId
          ? [mutation.blockId]
          : []

    const revision = getEnvelopeRevision(envelope, mutation?.appliedRevision ?? null)

    for (const id of ids) {
      removeServerBlock(id, revision)

      if (hasCurrentLease(id) || isActiveServerBlock(id) || shouldQueueStructuralRemote(id)) {
        pendingRemoteDeletedIds.add(id)
        continue
      }

      void deleteEditorBlock(id)
    }

    if (pendingRemoteDeletedIds.size > 0) {
      scheduleRemoteQueue()
    }
  }

  function handleRemoteBlockDraft(envelope: RealtimeEnvelope<BlockDraftPayload>) {
    const payload = envelope.payload
    if (!payload || payload.pageId !== props.pageId) return
    if (payload.connectionId === realtimeClient.state.connectionId) return
    if (payload.editorSessionId === getEditorSessionId()) return

    const blockId = payload.blockId
    const userName = payload.userName?.trim() || 'Người khác'

    remoteEditorsByBlock.set(blockId, userName)
    markRemoteTyping(blockId, userName)

    // Cố tình KHÔNG patch draft text vào EditorJS DOM theo từng phím.
    // EditorJS sẽ phát onChange khi DOM đổi, dễ tạo vòng lặp save -> remote -> save
    // và cảm giác nhấp nháy như reload/F5. Draft chỉ dùng làm typing/lock indicator.
    // Nội dung thật được sync qua BlockUpdated sau autosave và chỉ patch innerHTML của block.
  }

  function handleRemoteEditingState(
    envelope: RealtimeEnvelope<BlockEditingStatePayload>
  ) {
    const payload = envelope.payload
    if (!payload || payload.pageId !== props.pageId) return
    if (payload.editorSessionId === getEditorSessionId()) return
    if (payload.connectionId === realtimeClient.state.connectionId) return

    const userName = payload.userName?.trim() || 'Người khác'

    if (payload.isEditing) {
      remoteEditorsByBlock.set(payload.blockId, userName)
      markRemoteTyping(payload.blockId, userName)
      return
    }

    remoteEditorsByBlock.delete(payload.blockId)
    clearRemoteTyping(payload.blockId)
  }

  function handleRemoteLeaseChanged(envelope: RealtimeEnvelope<unknown>) {
    const raw = getObject(envelope.payload)
    if (!raw) return

    const blockId = asGuid(readCase(raw, 'blockId', 'BlockId'))
    const pageId = asGuid(readCase(raw, 'pageId', 'PageId'))

    if (!blockId || pageId !== props.pageId) return

    const holderDisplayName = asNullableString(
      readCase(raw, 'holderDisplayName', 'HolderDisplayName')
    )
    const isHeldByCurrentUser = readCase(raw, 'isHeldByCurrentUser', 'IsHeldByCurrentUser') === true

    if (isHeldByCurrentUser) return

    if (holderDisplayName) {
      remoteEditorsByBlock.set(blockId, holderDisplayName)
      markRemoteTyping(blockId, holderDisplayName)
    }
  }

  function bindRealtimeHandlers() {
    unbindRealtimeHandlers()

    unsubscribeBlockCreated = realtimeClient.on('BlockCreated', handleRemoteBlockCreated)
    unsubscribeBlockUpdated = realtimeClient.on('BlockUpdated', handleRemoteBlockUpdated)
    unsubscribeBlockDeleted = realtimeClient.on('BlockDeleted', handleRemoteBlockDeleted)
    unsubscribeBlockDraftChanged = realtimeClient.on('BlockDraftChanged', handleRemoteBlockDraft)
    unsubscribeBlockEditingStateChanged = realtimeClient.on(
      'BlockEditingStateChanged',
      handleRemoteEditingState
    )
    unsubscribeBlockLeaseChanged = realtimeClient.on('BlockLeaseChanged', handleRemoteLeaseChanged)
    unsubscribePageMousePointerChanged = realtimeClient.on(
      'PageMousePointerChanged',
      handlePageMousePointerChanged
    )

    // Đăng ký handler no-op để SignalR không cảnh báo thiếu method pagepresencechanged.
    unsubscribePagePresenceChanged = realtimeClient.on('PagePresenceChanged', () => {})
  }

  function unbindRealtimeHandlers() {
    unsubscribeBlockCreated?.()
    unsubscribeBlockUpdated?.()
    unsubscribeBlockDeleted?.()
    unsubscribeBlockDraftChanged?.()
    unsubscribeBlockEditingStateChanged?.()
    unsubscribeBlockLeaseChanged?.()
    unsubscribePageMousePointerChanged?.()
    unsubscribePagePresenceChanged?.()

    unsubscribeBlockCreated = null
    unsubscribeBlockUpdated = null
    unsubscribeBlockDeleted = null
    unsubscribeBlockDraftChanged = null
    unsubscribeBlockEditingStateChanged = null
    unsubscribeBlockLeaseChanged = null
    unsubscribePageMousePointerChanged = null
    unsubscribePagePresenceChanged = null
  }

  async function joinRealtimePage(pageId: Guid) {
    bindRealtimeHandlers()

    try {
      await realtimeClient.start()
      await realtimeClient.joinPage(pageId)
      await realtimeClient.heartbeatPage(pageId)

      if (heartbeatTimer) {
        window.clearInterval(heartbeatTimer)
      }

      heartbeatTimer = window.setInterval(() => {
        if (!props.pageId) return
        void realtimeClient.heartbeatPage(props.pageId).catch(() => {})
      }, HEARTBEAT_INTERVAL_MS)
    } catch (realtimeError) {
      error.value = getApiErrorMessage(realtimeError, 'Không kết nối được realtime.')
    }
  }

  async function leaveRealtimePage(pageId: Guid | null) {
    if (heartbeatTimer) {
      window.clearInterval(heartbeatTimer)
      heartbeatTimer = null
    }

    if (pageId) {
      await realtimeClient.leavePage(pageId).catch(() => {})
    }

    unbindRealtimeHandlers()
  }

  async function loadPageDocument(pageId: Guid) {
    isLoading.value = true
    error.value = null

    try {
      const result = await blockController.listByPage(pageId)

      if (!result.isSuccess || !result.data) {
        throw new Error(getApiResultErrorMessage(result, 'Không tải được nội dung page.'))
      }

      hydrateServerSnapshot(result.data)
      await createEditor(documentToEditorData(result.data))
      await joinRealtimePage(pageId)
      startLeaseRenewal()
    } catch (loadError) {
      error.value = getApiErrorMessage(loadError, 'Không tải được nội dung page.')
      await createEditor(defaultEditorData())
    } finally {
      isLoading.value = false
    }
  }

  function scheduleSoftSync(delay = 800) {
    if (!props.pageId) return

    window.setTimeout(() => {
      void softSyncFromServer()
    }, delay)
  }

  async function softSyncFromServer() {
    if (!props.pageId || !editor.value || !isReady.value) return

    try {
      const result = await blockController.listByPage(props.pageId)

      if (!result.isSuccess || !result.data) return

      const oldIds = new Set(Object.keys(serverBlocksById.value))
      hydrateServerSnapshot(result.data)

      for (const block of result.data.blocks) {
        oldIds.delete(block.id)

        if (hasCurrentLease(block.id) || isActiveServerBlock(block.id)) continue

        const patched = applyServerBlockToDomOnly(block)

        if (!patched && !getBlockElementByServerId(block.id)) {
          pendingRemoteBlocks.set(block.id, block)
        }
      }

      for (const deletedId of oldIds) {
        if (hasCurrentLease(deletedId) || isActiveServerBlock(deletedId)) continue
        pendingRemoteDeletedIds.add(deletedId)
      }

      if (pendingRemoteBlocks.size > 0 || pendingRemoteDeletedIds.size > 0) {
        scheduleRemoteQueue()
      }

      if (error.value?.includes('Revision') || error.value?.includes('lease')) {
        error.value = null
      }
    } catch {
      // Soft sync fail im lặng, không phá UX đang gõ.
    }
  }

  function resetLocalState() {
    if (saveTimer) {
      window.clearTimeout(saveTimer)
      saveTimer = null
    }

    if (draftTimer) {
      window.clearTimeout(draftTimer)
      draftTimer = null
    }

    if (releaseTimer) {
      window.clearTimeout(releaseTimer)
      releaseTimer = null
    }

    if (remoteQueueTimer) {
      window.clearTimeout(remoteQueueTimer)
      remoteQueueTimer = null
    }

    if (pointerSendTimer) {
      window.clearTimeout(pointerSendTimer)
      pointerSendTimer = null
    }

    leasesByBlock.clear()
    localToServerBlockIds.clear()
    serverToLocalBlockIds.clear()
    remoteEditorsByBlock.clear()
    pendingRemoteBlocks.clear()
    pendingRemoteDeletedIds.clear()
    remotePointerMap.clear()

    for (const timer of remotePointerTimers.values()) {
      window.clearTimeout(timer)
    }

    for (const timer of remoteTypingTimers.values()) {
      window.clearTimeout(timer)
    }

    remotePointerTimers.clear()
    remoteTypingTimers.clear()
    remotePointers.value = []
    activeEditorBlockId.value = null
    selectedRange.value = null
    isTextToolbarVisible.value = false
  }

  async function switchPage(pageId: Guid | null, oldPageId: Guid | null) {
    await releaseAllLeases().catch(() => {})
    await leaveRealtimePage(oldPageId).catch(() => {})

    resetLocalState()
    stopLeaseRenewal()
    lastLoadedPageId = pageId

    if (!pageId) {
      await destroyEditor()
      return
    }

    await loadPageDocument(pageId)
  }

  function rememberTextSelection() {
    const selection = window.getSelection()

    if (!selection || selection.rangeCount === 0) {
      isTextToolbarVisible.value = false
      return
    }

    const range = selection.getRangeAt(0)

    if (
      range.collapsed ||
      !holderRef.value ||
      !holderRef.value.contains(range.commonAncestorContainer)
    ) {
      isTextToolbarVisible.value = false
      return
    }

    selectedRange.value = range.cloneRange()

    const rect = range.getBoundingClientRect()
    textToolbarStyle.value = {
      top: `${Math.max(12, rect.top + window.scrollY - 52)}px`,
      left: `${Math.max(12, rect.left + window.scrollX)}px`,
    }
    isTextToolbarVisible.value = true
  }

  function keepTextToolbarOpen(event: MouseEvent) {
    event.preventDefault()
  }

  function restoreTextSelection() {
    const range = selectedRange.value
    if (!range) return false

    const selection = window.getSelection()
    if (!selection) return false

    selection.removeAllRanges()
    selection.addRange(range)
    return true
  }

  function scheduleInlineStyleSave() {
    const blockId = activeEditorBlockId.value
    if (blockId) prepareBlockForEdit(blockId, true)

    scheduleSave(180)
    scheduleDraftBroadcast()
    rememberTextSelection()
  }

  function wrapSelectionWithSpan(style: Partial<CSSStyleDeclaration>) {
    if (!restoreTextSelection()) return

    const selection = window.getSelection()
    if (!selection || selection.rangeCount === 0) return

    const range = selection.getRangeAt(0)
    if (range.collapsed) return

    const span = document.createElement('span')
    span.className = 'editor-inline-style'
    span.dataset.editorInlineStyle = 'true'

    Object.assign(span.style, style)

    try {
      range.surroundContents(span)
    } catch {
      const content = range.extractContents()
      span.appendChild(content)
      range.insertNode(span)
    }

    selection.removeAllRanges()
    const nextRange = document.createRange()
    nextRange.selectNodeContents(span)
    selection.addRange(nextRange)
    selectedRange.value = nextRange.cloneRange()

    scheduleInlineStyleSave()
  }

  function applyFontFamily() {
    if (!selectedFontFamily.value) return
    wrapSelectionWithSpan({ fontFamily: selectedFontFamily.value })
  }

  function applyFontSize() {
    if (!selectedFontSize.value) return
    wrapSelectionWithSpan({ fontSize: selectedFontSize.value, lineHeight: '1.5' })
  }

  function applyTextColor() {
    wrapSelectionWithSpan({ color: selectedTextColor.value })
  }

  function applyHighlightColor() {
    wrapSelectionWithSpan({
      backgroundColor: selectedHighlightColor.value,
      borderRadius: '0.2em',
      padding: '0.02em 0.12em',
    })
  }

  function toggleBold() {
    if (!restoreTextSelection()) return

    document.execCommand('bold')
    scheduleInlineStyleSave()
  }

  function toggleItalic() {
    if (!restoreTextSelection()) return

    document.execCommand('italic')
    scheduleInlineStyleSave()
  }

  function clearInlineStyle() {
    if (!restoreTextSelection()) return

    document.execCommand('removeFormat')
    scheduleInlineStyleSave()
  }

  onMounted(() => {
    void switchPage(props.pageId, null)
  })

  watch(
    () => props.pageId,
    (pageId, oldPageId) => {
      if (pageId === oldPageId) return
      void switchPage(pageId, oldPageId ?? lastLoadedPageId)
    }
  )

  onBeforeUnmount(() => {
    isDestroyed = true

    if (saveTimer) window.clearTimeout(saveTimer)
    if (draftTimer) window.clearTimeout(draftTimer)
    if (releaseTimer) window.clearTimeout(releaseTimer)
    if (remoteQueueTimer) window.clearTimeout(remoteQueueTimer)
    if (pointerSendTimer) window.clearTimeout(pointerSendTimer)
    if (heartbeatTimer) window.clearInterval(heartbeatTimer)

    stopLeaseRenewal()
    unbindRealtimeHandlers()

    for (const timer of remotePointerTimers.values()) {
      window.clearTimeout(timer)
    }

    for (const timer of remoteTypingTimers.values()) {
      window.clearTimeout(timer)
    }

    void releaseAllLeases()
    void leaveRealtimePage(props.pageId)
    void destroyEditor()
  })

  return {
    holderRef,
    holderId,

    isLoading,
    isSaving,
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
