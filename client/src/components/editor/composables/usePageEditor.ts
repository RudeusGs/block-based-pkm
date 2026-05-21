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

  surround() {}

  checkState() {
    return false
  }
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

  const serverBlocksById = ref<Record<Guid, BlockResponse>>({})
  const serverBlockSignatures = ref<Record<Guid, string>>({})
  const heldLeaseBlockIds = ref<Set<Guid>>(new Set())
  const activeEditorBlockId = ref<Guid | null>(null)

  const heldLeaseSessionIds = new Map<Guid, string>()
  const acquiringLeasePromises = new Map<Guid, Promise<boolean>>()

  let saveTimer: number | null = null
  let draftTimer: number | null = null
  let leaseRenewTimer: number | null = null
  let toolbarHideTimer: number | null = null
  let remoteApplyTimer: number | null = null
  let heartbeatTimer: number | null = null
  let pendingRemoteRefresh = false
  let draftSequence = 0

  let unsubscribeBlockCreated: (() => void) | null = null
  let unsubscribeBlockUpdated: (() => void) | null = null
  let unsubscribeBlockDeleted: (() => void) | null = null
  let unsubscribeBlockDraftChanged: (() => void) | null = null
  let unsubscribeBlockEditingStateChanged: (() => void) | null = null

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
      : container.parentElement
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
    if (
      !selectedRange.value ||
      !isTextToolbarVisible.value ||
      !canEditDocument.value
    ) {
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

  function getEditorBlockIdFromEvent(event: Event) {
    const target = event.target

    if (!(target instanceof Node)) return null

    const blockElement = getClosestEditorBlock(target) as HTMLElement | null

    return blockElement?.dataset?.id ?? null
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
    return value && typeof value === 'object'
      ? (value as Record<string, unknown>)
      : null
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

  function getRevisionFromEnvelope(
    envelope: RealtimeEnvelope<unknown>,
    fallbackRevision?: number | null
  ) {
    if (typeof envelope.revision === 'number') return envelope.revision
    if (typeof fallbackRevision === 'number') return fallbackRevision
    return null
  }

  function plainTextFromHtml(value: string) {
    const element = document.createElement('div')
    element.innerHTML = value
    return element.textContent ?? element.innerText ?? ''
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

  function parsePropsJson(propsJson: string | null): StoredEditorJsBlockProps | null {
    if (!propsJson) return null

    try {
      const parsed = JSON.parse(propsJson)

      if (parsed && typeof parsed === 'object') {
        return parsed as StoredEditorJsBlockProps
      }
    } catch {
      return null
    }

    return null
  }

  function backendBlockToEditorBlock(block: BlockResponse): EditorBlockData {
    const propsJson = parsePropsJson(block.propsJson)
    const stored = propsJson?.editorjs

    if (stored?.type) {
      return {
        id: block.id,
        type: stored.type,
        data: stored.data ?? {},
        tunes: stored.tunes ?? undefined,
      } as EditorBlockData
    }

    if (block.type === 'editorjs' && block.propsJson) {
      try {
        const legacy = JSON.parse(block.propsJson) as OutputData

        if (Array.isArray(legacy.blocks) && legacy.blocks[0]) {
          return {
            ...legacy.blocks[0],
            id: block.id,
          } as EditorBlockData
        }
      } catch {
        // fallback below
      }
    }

    return {
      id: block.id,
      type: block.type === 'paragraph' ? 'paragraph' : block.type,
      data: {
        text: block.textContent ?? '',
      },
    } as EditorBlockData
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

      return left.createdDate.localeCompare(right.createdDate)
    })
  }

  function documentToEditorData(document: PageDocumentResponse): OutputData {
    const blocks = sortBackendBlocks(document.blocks)

    return {
      time: Date.now(),
      version: '2.31.0',
      blocks: blocks.map(backendBlockToEditorBlock),
    }
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

  function getEditorSessionId() {
    const connectionId = realtimeClient.state.connectionId

    if (!connectionId) {
      throw new Error('Chưa kết nối được phiên chỉnh sửa.')
    }

    return connectionId
  }

  function getHttpStatus(errorLike: any) {
    return (
      errorLike?.status ??
      errorLike?.response?.status ??
      errorLike?.data?.status ??
      errorLike?.data?.statusCode ??
      null
    )
  }

  function getLeaseHolderFromError(errorLike: any) {
    return (
      errorLike?.data?.data?.holderDisplayName ??
      errorLike?.data?.holderDisplayName ??
      errorLike?.holderDisplayName ??
      'người khác'
    )
  }

  async function ensureRealtimePageJoined(pageId: Guid) {
    await realtimeClient.start()
    await realtimeClient.joinPage(pageId)

    if (heartbeatTimer) {
      window.clearInterval(heartbeatTimer)
    }

    heartbeatTimer = window.setInterval(() => {
      void realtimeClient.heartbeatPage(pageId).catch(() => {})
    }, 15_000)
  }

  function markLeaseHeld(blockId: Guid, editorSessionId: string) {
    heldLeaseBlockIds.value.add(blockId)
    heldLeaseSessionIds.set(blockId, editorSessionId)
  }

  function clearLease(blockId: Guid) {
    heldLeaseBlockIds.value.delete(blockId)
    heldLeaseSessionIds.delete(blockId)
  }

  function startLeaseRenewal() {
    stopLeaseRenewal()

    leaseRenewTimer = window.setInterval(() => {
      const currentEditorSessionId = realtimeClient.state.connectionId

      if (!currentEditorSessionId || heldLeaseBlockIds.value.size === 0) return

      for (const blockId of [...heldLeaseBlockIds.value]) {
        const heldSessionId = heldLeaseSessionIds.get(blockId)

        if (heldSessionId !== currentEditorSessionId) {
          clearLease(blockId)
          continue
        }

        void blockController
          .renewLease(blockId, {
            editorSessionId: currentEditorSessionId,
          })
          .then((result) => {
            if (!result.isSuccess || !result.data?.granted) {
              clearLease(blockId)
            }
          })
          .catch(() => {
            clearLease(blockId)
          })
      }
    }, 18_000)
  }

  function stopLeaseRenewal() {
    if (!leaseRenewTimer) return

    window.clearInterval(leaseRenewTimer)
    leaseRenewTimer = null
  }

  async function acquireBlockLease(blockId: Guid) {
    await realtimeClient.start()

    const editorSessionId = getEditorSessionId()

    const oldSessionId = heldLeaseSessionIds.get(blockId)

    if (oldSessionId && oldSessionId !== editorSessionId) {
      try {
        await blockController.releaseLease(blockId, {
          editorSessionId: oldSessionId,
        })
      } catch {
        // lease cũ có thể đã chết, bỏ qua.
      }

      clearLease(blockId)
    }

    try {
      const result = await blockController.acquireLease(blockId, {
        editorSessionId,
        holderDisplayName: 'Editor',
      })

      if (!result.isSuccess || !result.data?.granted) {
        const holder = result.data?.holderDisplayName ?? 'người khác'
        throw new Error(`Block này đang được ${holder} chỉnh sửa.`)
      }

      markLeaseHeld(blockId, editorSessionId)
      startLeaseRenewal()

      if (props.pageId) {
        await realtimeClient.sendBlockEditingState({
          pageId: props.pageId,
          blockId,
          editorSessionId,
          isEditing: true,
        })
      }

      return true
    } catch (leaseError: any) {
      clearLease(blockId)

      if (getHttpStatus(leaseError) === 409) {
        const holder = getLeaseHolderFromError(leaseError)
        throw new Error(`Block này đang được ${holder} chỉnh sửa.`)
      }

      throw leaseError
    }
  }

  async function ensureBlockLease(blockId: Guid) {
    const currentEditorSessionId = realtimeClient.state.connectionId
    const heldSessionId = heldLeaseSessionIds.get(blockId)

    if (
      currentEditorSessionId &&
      heldLeaseBlockIds.value.has(blockId) &&
      heldSessionId === currentEditorSessionId
    ) {
      try {
        const renewed = await blockController.renewLease(blockId, {
          editorSessionId: currentEditorSessionId,
        })

        if (renewed.isSuccess && renewed.data?.granted) {
          return true
        }
      } catch {
        // stale lease, acquire lại bên dưới.
      }

      clearLease(blockId)
    }

    const existingPromise = acquiringLeasePromises.get(blockId)

    if (existingPromise) {
      return existingPromise
    }

    const promise = acquireBlockLease(blockId).finally(() => {
      acquiringLeasePromises.delete(blockId)
    })

    acquiringLeasePromises.set(blockId, promise)

    return promise
  }

  async function releaseHeldLeases() {
    const blockIds = [...heldLeaseBlockIds.value]

    for (const blockId of blockIds) {
      const editorSessionId =
        heldLeaseSessionIds.get(blockId) ?? realtimeClient.state.connectionId

      if (!editorSessionId) {
        clearLease(blockId)
        continue
      }

      try {
        if (props.pageId) {
          await realtimeClient.sendBlockEditingState({
            pageId: props.pageId,
            blockId,
            editorSessionId,
            isEditing: false,
          })
        }
      } catch {
        // ignore
      }

      try {
        await blockController.releaseLease(blockId, {
          editorSessionId,
        })
      } catch {
        // server vẫn tự cleanup khi disconnect hoặc lease hết hạn.
      } finally {
        clearLease(blockId)
      }
    }

    heldLeaseBlockIds.value.clear()
    heldLeaseSessionIds.clear()
    acquiringLeasePromises.clear()
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
      // fallback bên dưới.
    }

    holderRef.value
      ?.querySelectorAll<HTMLElement>('[contenteditable]')
      .forEach((element) => {
        element.setAttribute('contenteditable', readOnly ? 'false' : 'true')
      })
  }

  async function patchEditorBlock(block: EditorBlockData) {
    if (!editor.value || !isReady.value) return false

    const editorAny = editor.value as any

    if (typeof editorAny.blocks?.update !== 'function') return false

    try {
      await editorAny.blocks.update(block.id, block.data)
      return true
    } catch {
      return false
    }
  }

  async function renderEditorData(data: OutputData) {
    if (!editor.value) return

    isApplyingRemote.value = true

    try {
      await editor.value.render(data)

      if (!canEditDocument.value) {
        await setEditorReadOnly(true)
      }
    } finally {
      window.setTimeout(() => {
        isApplyingRemote.value = false
      }, 120)
    }
  }

  async function reloadFromServer(render = true) {
    if (!props.pageId) return null

    const result = await blockController.listByPage(props.pageId)

    if (!result.isSuccess || !result.data) {
      throw new Error(
        getApiResultErrorMessage(result, 'Không tải lại được nội dung page.')
      )
    }

    hydrateServerSnapshot(result.data)

    if (render) {
      await renderEditorData(documentToEditorData(result.data))
    }

    return result.data
  }

  async function createInitialEditorBlock(pageId: Guid, revision: number) {
    const initialBlock: EditorBlockData = {
      id: crypto.randomUUID(),
      type: 'paragraph',
      data: {
        text: '',
      },
    } as EditorBlockData

    const result = await blockController.create(pageId, {
      expectedRevision: revision,
      type: initialBlock.type,
      textContent: plainTextFromEditorBlock(initialBlock),
      propsJson: blockToPropsJson(initialBlock),
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

    const document = await reloadFromServer(false)

    return document ? documentToEditorData(document) : defaultEditorData()
  }

  async function migrateLegacyEditorJsBlockIfNeeded(
    document: PageDocumentResponse
  ) {
    if (!props.pageId) return document

    const legacyBlocks = document.blocks.filter((block) => block.type === 'editorjs')
    const normalBlocks = document.blocks.filter((block) => block.type !== 'editorjs')

    if (normalBlocks.length > 0 || legacyBlocks.length !== 1) {
      return document
    }

    const legacyBlock = legacyBlocks[0]

    if (!legacyBlock?.propsJson) return document

    let legacyData: OutputData | null = null

    try {
      const parsed = JSON.parse(legacyBlock.propsJson)

      if (parsed && typeof parsed === 'object' && Array.isArray(parsed.blocks)) {
        legacyData = parsed as OutputData
      }
    } catch {
      legacyData = null
    }

    if (!legacyData) return document

    await ensureBlockLease(legacyBlock.id)

    let previousBlockId: Guid | null = null

    const blocksToCreate =
      legacyData.blocks.length > 0
        ? legacyData.blocks
        : [
            {
              id: crypto.randomUUID(),
              type: 'paragraph',
              data: { text: '' },
            } as EditorBlockData,
          ]

    for (const editorBlock of blocksToCreate) {
      const result = await blockController.create(props.pageId, {
        expectedRevision: currentRevision.value,
        type: editorBlock.type,
        textContent: plainTextFromEditorBlock(editorBlock),
        propsJson: blockToPropsJson(editorBlock),
        parentBlockId: null,
        previousBlockId,
        nextBlockId: null,
        schemaVersion: 1,
      })

      if (!result.isSuccess || !result.data?.block) {
        throw new Error(
          getApiResultErrorMessage(result, 'Không migrate được EditorJS block.')
        )
      }

      currentRevision.value = result.data.appliedRevision
      previousBlockId = result.data.block.id
    }

    const editorSessionId = getEditorSessionId()

    await blockController.delete(legacyBlock.id, {
      expectedRevision: currentRevision.value,
      editorSessionId,
      note: 'Migrate legacy editorjs document block to block-level EditorJS data',
    })

    await blockController
      .releaseLease(legacyBlock.id, {
        editorSessionId,
      })
      .catch(() => {})

    clearLease(legacyBlock.id)

    const migratedDocument = await reloadFromServer(false)

    return migratedDocument ?? document
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
        scheduleDraft()
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

    if (draftTimer) {
      window.clearTimeout(draftTimer)
      draftTimer = null
    }

    if (remoteApplyTimer) {
      window.clearTimeout(remoteApplyTimer)
      remoteApplyTimer = null
    }

    if (editor.value) {
      try {
        editor.value.destroy()
      } catch {
        // ignore
      }
    }

    editor.value = null
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

      hydrateServerSnapshot(result.data)

      let document = result.data

      if (document.blocks.some((block) => block.type === 'editorjs')) {
        document = await migrateLegacyEditorJsBlockIfNeeded(document)
        hydrateServerSnapshot(document)
      }

      const data =
        document.blocks.length > 0
          ? documentToEditorData(document)
          : await createInitialEditorBlock(pageId, document.currentRevision)

      await initEditor(data)
    } catch (loadError) {
      error.value = getApiErrorMessage(loadError, 'Không tải được page này.')
    } finally {
      isLoading.value = false
    }
  }

  function isServerKnownBlockId(id: string | undefined): id is Guid {
    return Boolean(id && serverBlocksById.value[id])
  }

  function getChangedExistingBlocks(data: OutputData) {
    return data.blocks.filter((block) => {
      if (!isServerKnownBlockId(block.id)) return false

      const serverBlock = serverBlocksById.value[block.id]
      if (!serverBlock) return false

      const nextServerLikeSignature = JSON.stringify({
        type: block.type,
        textContent: plainTextFromEditorBlock(block),
        propsJson: blockToPropsJson(block),
      })

      return nextServerLikeSignature !== serverBlockSignatures.value[serverBlock.id]
    })
  }

  function getCreatedBlocks(data: OutputData) {
    return data.blocks.filter((block) => !isServerKnownBlockId(block.id))
  }

  function getDeletedBlockIds(data: OutputData) {
    const currentIds = new Set(
      data.blocks
        .map((block) => block.id)
        .filter((id): id is string => Boolean(id))
    )

    return Object.keys(serverBlocksById.value).filter(
      (serverId) => !currentIds.has(serverId)
    )
  }

  async function syncEditorDataToServer(data: OutputData) {
    if (!props.pageId) return

    const editorSessionId = getEditorSessionId()

    const deletedBlockIds = getDeletedBlockIds(data)
    const changedExistingBlocks = getChangedExistingBlocks(data)
    const createdBlocks = getCreatedBlocks(data)

    for (const blockId of deletedBlockIds) {
      await ensureBlockLease(blockId)

      const activeSessionId = getEditorSessionId()

      const result = await blockController.delete(blockId, {
        expectedRevision: currentRevision.value,
        editorSessionId: activeSessionId,
        note: 'Deleted from EditorJS',
      })

      if (!result.isSuccess || !result.data) {
        throw new Error(getApiResultErrorMessage(result, 'Không xóa được block.'))
      }

      clearLease(blockId)
      currentRevision.value = result.data.appliedRevision
    }

    let previousBlockId: Guid | null = null

    for (const editorBlock of data.blocks) {
      if (isServerKnownBlockId(editorBlock.id)) {
        previousBlockId = editorBlock.id
        continue
      }

      if (!createdBlocks.includes(editorBlock)) continue

      const result = await blockController.create(props.pageId, {
        expectedRevision: currentRevision.value,
        type: editorBlock.type,
        textContent: plainTextFromEditorBlock(editorBlock),
        propsJson: blockToPropsJson(editorBlock),
        parentBlockId: null,
        previousBlockId,
        nextBlockId: null,
        schemaVersion: 1,
      })

      if (!result.isSuccess || !result.data?.block) {
        throw new Error(getApiResultErrorMessage(result, 'Không tạo được block.'))
      }

      currentRevision.value = result.data.appliedRevision
      previousBlockId = result.data.block.id
    }

    for (const editorBlock of changedExistingBlocks) {
      if (!isServerKnownBlockId(editorBlock.id)) continue

      await ensureBlockLease(editorBlock.id)

      const activeSessionId =
        heldLeaseSessionIds.get(editorBlock.id) ?? editorSessionId

      const result = await blockController.update(editorBlock.id, {
        expectedRevision: currentRevision.value,
        editorSessionId: activeSessionId,
        type: editorBlock.type,
        textContent: plainTextFromEditorBlock(editorBlock),
        propsJson: blockToPropsJson(editorBlock),
      })

      if (!result.isSuccess || !result.data?.block) {
        throw new Error(getApiResultErrorMessage(result, 'Không lưu được block.'))
      }

      markLeaseHeld(editorBlock.id, activeSessionId)
      currentRevision.value = result.data.appliedRevision
      serverBlocksById.value[result.data.block.id] = result.data.block
      serverBlockSignatures.value[result.data.block.id] =
        serverBlockSignature(result.data.block)
    }

    if (deletedBlockIds.length > 0 || createdBlocks.length > 0) {
      const document = await reloadFromServer(false)

      if (document) {
        await renderEditorData(documentToEditorData(document))
      }

      return
    }

    const document = await reloadFromServer(false)

    if (document) {
      hydrateServerSnapshot(document)
    }

    if (pendingRemoteRefresh) {
      pendingRemoteRefresh = false
      await reloadFromServer(true)
    }
  }

  async function saveEditorData() {
    if (!canEditDocument.value) return
    if (!props.pageId || !editor.value || isSaving.value || isApplyingRemote.value) {
      return
    }

    isSaving.value = true
    error.value = null

    try {
      await realtimeClient.start()

      const data = await editor.value.save()

      await syncEditorDataToServer(data)
    } catch (saveError: any) {
      if (getHttpStatus(saveError) === 409) {
        try {
          const data = editor.value ? await editor.value.save() : null

          await reloadFromServer(false)

          if (data) {
            await syncEditorDataToServer(data)
          }

          return
        } catch (retryError) {
          error.value = getApiErrorMessage(
            retryError,
            'Revision bị lệch. Page đã reload để tránh ghi đè dữ liệu.'
          )

          await reloadFromServer(true).catch(() => {})
          return
        }
      }

      error.value = getApiErrorMessage(saveError, 'Không lưu được nội dung.')
    } finally {
      isSaving.value = false
    }
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

  function scheduleDraft() {
    if (!props.pageId || !activeEditorBlockId.value) return
    if (!heldLeaseBlockIds.value.has(activeEditorBlockId.value)) return

    if (draftTimer) {
      window.clearTimeout(draftTimer)
    }

    draftTimer = window.setTimeout(() => {
      void sendActiveBlockDraft()
    }, 140)
  }

  async function sendActiveBlockDraft() {
    if (!props.pageId || !editor.value || !activeEditorBlockId.value) return

    const blockId = activeEditorBlockId.value

    if (!heldLeaseBlockIds.value.has(blockId)) return

    try {
      const data = await editor.value.save()
      const block = data.blocks.find((item) => item.id === blockId)

      if (!block) return

      draftSequence += 1

      await realtimeClient.sendBlockDraft({
        pageId: props.pageId,
        blockId,
        editorSessionId: getEditorSessionId(),
        baseRevision: currentRevision.value,
        clientSequence: draftSequence,
        type: block.type,
        textContent: plainTextFromEditorBlock(block),
        propsJson: blockToPropsJson(block),
      })
    } catch {
      // draft realtime fail không được phá flow save thật.
    }
  }

  async function handleEditorFocusIn(event: FocusEvent) {
    const blockId = getEditorBlockIdFromEvent(event)

    if (!blockId) return

    activeEditorBlockId.value = blockId

    if (!isServerKnownBlockId(blockId)) return

    try {
      await ensureBlockLease(blockId)
    } catch (leaseError) {
      error.value = getApiErrorMessage(
        leaseError,
        'Block này đang được người khác chỉnh sửa.'
      )
    }
  }

  function handleEditorInput(event: Event) {
    const blockId = getEditorBlockIdFromEvent(event)

    if (blockId) {
      activeEditorBlockId.value = blockId
    }

    scheduleDraft()
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
        pendingRemoteRefresh = true

        if (!isSaving.value && !activeEditorBlockId.value) {
          await reloadFromServer(true)
          pendingRemoteRefresh = false
        }

        return
      }

      currentRevision.value = revision
    }

    if (!mutation.block) return

    serverBlocksById.value[mutation.block.id] = mutation.block
    serverBlockSignatures.value[mutation.block.id] =
      serverBlockSignature(mutation.block)

    if (mutation.block.id === activeEditorBlockId.value || isSaving.value) {
      pendingRemoteRefresh = true
      return
    }

    const editorBlock = backendBlockToEditorBlock(mutation.block)
    const patched = await patchEditorBlock(editorBlock)

    if (!patched) {
      pendingRemoteRefresh = true

      if (!activeEditorBlockId.value) {
        await reloadFromServer(true)
        pendingRemoteRefresh = false
      }
    }
  }

  async function handleBlockDeleted(envelope: RealtimeEnvelope<unknown>) {
    if (!props.pageId) return

    const envelopePageId = getEnvelopePageId(envelope)
    const payload = getEnvelopePayload(envelope)

    if (envelopePageId && envelopePageId !== props.pageId) return

    const deletedIds = normalizeDeletedBlockIds(payload)

    if (deletedIds.length === 0) {
      pendingRemoteRefresh = true
      return
    }

    const activeDeleted = activeEditorBlockId.value
      ? deletedIds.includes(activeEditorBlockId.value)
      : false

    for (const id of deletedIds) {
      delete serverBlocksById.value[id]
      delete serverBlockSignatures.value[id]
      clearLease(id)
    }

    if (activeDeleted || isSaving.value) {
      error.value = 'Block bạn đang sửa vừa bị xóa ở phiên khác. Đang đồng bộ lại.'
      pendingRemoteRefresh = true
      await reloadFromServer(true)
      pendingRemoteRefresh = false
      return
    }

    await reloadFromServer(true)
  }

  async function handleBlockDraftChanged(
    envelope: RealtimeEnvelope<BlockDraftPayload>
  ) {
    const payload = envelope.payload

    if (!payload || !props.pageId) return
    if (payload.pageId !== props.pageId) return
    if (payload.connectionId === realtimeClient.state.connectionId) return
    if (payload.blockId === activeEditorBlockId.value) return

    const fakeBlock: BlockResponse = {
      id: payload.blockId,
      pageId: payload.pageId,
      parentBlockId: null,
      type: payload.type ?? 'paragraph',
      textContent: payload.textContent ?? null,
      propsJson: payload.propsJson ?? null,
      schemaVersion: 1,
      orderKey: '',
      createdBy: payload.userId,
      lastModifiedBy: payload.userId,
      createdDate: payload.occurredAtUtc,
      updatedDate: payload.occurredAtUtc,
    }

    await patchEditorBlock(backendBlockToEditorBlock(fakeBlock))
  }

  function handleBlockEditingStateChanged(
    envelope: RealtimeEnvelope<BlockEditingStatePayload>
  ) {
    const payload = envelope.payload

    if (!payload || !props.pageId) return
    if (payload.pageId !== props.pageId) return
    if (payload.connectionId === realtimeClient.state.connectionId) return

    if (payload.isEditing && payload.blockId === activeEditorBlockId.value) {
      error.value = `${
        payload.userName ?? 'Người khác'
      } cũng đang mở block này. Nếu save bị chặn thì page sẽ tự đồng bộ.`
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
  }

  function unbindRealtimeEvents() {
    unsubscribeBlockCreated?.()
    unsubscribeBlockUpdated?.()
    unsubscribeBlockDeleted?.()
    unsubscribeBlockDraftChanged?.()
    unsubscribeBlockEditingStateChanged?.()

    unsubscribeBlockCreated = null
    unsubscribeBlockUpdated = null
    unsubscribeBlockDeleted = null
    unsubscribeBlockDraftChanged = null
    unsubscribeBlockEditingStateChanged = null
  }

  onMounted(() => {
    document.addEventListener('selectionchange', handleDocumentSelectionChange)
    window.addEventListener('scroll', handleFloatingToolbarReposition, true)
    window.addEventListener('resize', handleFloatingToolbarReposition)
  })

  watch(
    () => props.pageId,
    async (nextPageId, previousPageId) => {
      await releaseHeldLeases()
      stopLeaseRenewal()

      if (heartbeatTimer) {
        window.clearInterval(heartbeatTimer)
        heartbeatTimer = null
      }

      error.value = null
      canEditDocument.value = true
      currentRevision.value = 0
      activeEditorBlockId.value = null
      serverBlocksById.value = {}
      serverBlockSignatures.value = {}
      selectedRange.value = null
      isTextToolbarVisible.value = false
      pendingRemoteRefresh = false
      heldLeaseSessionIds.clear()
      acquiringLeasePromises.clear()

      if (previousPageId) {
        try {
          await realtimeClient.leavePage(previousPageId)
        } catch {
          // ignore
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

    if (saveTimer) {
      window.clearTimeout(saveTimer)
      saveTimer = null
    }

    if (draftTimer) {
      window.clearTimeout(draftTimer)
      draftTimer = null
    }

    if (remoteApplyTimer) {
      window.clearTimeout(remoteApplyTimer)
      remoteApplyTimer = null
    }

    if (heartbeatTimer) {
      window.clearInterval(heartbeatTimer)
      heartbeatTimer = null
    }

    unbindRealtimeEvents()

    void releaseHeldLeases()
    stopLeaseRenewal()

    if (props.pageId) {
      void realtimeClient.leavePage(props.pageId).catch(() => {})
    }

    void destroyEditor()
  })

  return {
    holderRef,
    holderId,

    isLoading,
    isSaving,
    error,
    canEditDocument,

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
    handleEditorInput,
  }
}