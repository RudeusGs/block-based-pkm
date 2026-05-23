import {
  computed,
  nextTick,
  onBeforeUnmount,
  reactive,
  ref,
  watch,
  type Ref,
} from 'vue'
import { blockController } from '@/api/services/block.api'
import {
  getApiErrorMessage,
  getApiResultErrorMessage,
} from '@/api/utils/api-error.util'
import type { Guid } from '@/api/models/common.model'
import type {
  BlockMutationResponse,
  BlockResponse,
  PageDocumentResponse,
} from '@/api/models/block.model'
import { realtimeClient } from '@/realtime/realtime.client'
import type {
  BlockDraftPayload,
  BlockEditingStatePayload,
  RealtimeEnvelope,
} from '@/realtime/realtime.types'

export type EditableBlockType =
  | 'paragraph'
  | 'heading_1'
  | 'heading_2'
  | 'heading_3'
  | 'todo'
  | 'quote'
  | 'code'
  | string

export interface EditableBlock extends BlockResponse {
  type: EditableBlockType
}

export interface BlockEditorState {
  isLeaseHeld: boolean
  isLeaseDenied: boolean
  isSaving: boolean
  isDirty: boolean
  isRemoteEditing: boolean
  holderDisplayName: string | null
  remoteUserName: string | null
  remoteDraftText: string | null
  remoteDraftSequence: number
  localDraftText: string | null
  lastSavedText: string
  clientSequence: number
  error: string | null
}

interface UseCollaborativePageEditorOptions {
  pageId: Ref<Guid | null>
  holderDisplayName?: Ref<string | null | undefined>
}

const editableTypes = new Set([
  'paragraph',
  'heading_1',
  'heading_2',
  'heading_3',
  'todo',
  'quote',
  'code',
  'editorjs',
])

function emptyBlockState(text = ''): BlockEditorState {
  return {
    isLeaseHeld: false,
    isLeaseDenied: false,
    isSaving: false,
    isDirty: false,
    isRemoteEditing: false,
    holderDisplayName: null,
    remoteUserName: null,
    remoteDraftText: null,
    remoteDraftSequence: -1,
    localDraftText: null,
    lastSavedText: text,
    clientSequence: 0,
    error: null,
  }
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

function getObject(value: unknown): Record<string, unknown> | null {
  return value && typeof value === 'object'
    ? (value as Record<string, unknown>)
    : null
}

function pick<T>(
  payload: Record<string, unknown>,
  camelKey: string,
  pascalKey: string
): T | null {
  return (payload[camelKey] ?? payload[pascalKey] ?? null) as T | null
}

function normalizeRevision(value: unknown) {
  if (typeof value === 'number' && Number.isFinite(value)) return value

  if (typeof value === 'string' && value.trim()) {
    const parsed = Number(value)
    return Number.isFinite(parsed) ? parsed : null
  }

  return null
}

function normalizeMutationPayload(
  envelope: RealtimeEnvelope<unknown>
): BlockMutationResponse | null {
  const payload = getObject(envelope.payload)
  if (!payload) return null

  const block = pick<BlockResponse>(payload, 'block', 'Block')
  const appliedRevision =
    normalizeRevision(payload.appliedRevision ?? payload.AppliedRevision) ??
    envelope.revision ??
    null

  const pageId =
    pick<Guid>(payload, 'pageId', 'PageId') ??
    ((block as any)?.pageId as Guid | undefined) ??
    ((block as any)?.PageId as Guid | undefined) ??
    null

  const blockId =
    pick<Guid>(payload, 'blockId', 'BlockId') ??
    ((block as any)?.id as Guid | undefined) ??
    ((block as any)?.Id as Guid | undefined) ??
    null

  if (!pageId || appliedRevision === null) return null

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

  const deletedIds = raw.deletedBlockIds ?? raw.DeletedBlockIds

  if (Array.isArray(deletedIds)) {
    return deletedIds.filter((item): item is Guid => typeof item === 'string')
  }

  const blockId = raw.blockId ?? raw.BlockId

  return typeof blockId === 'string' ? [blockId] : []
}

function stripHtml(value: string) {
  const container = document.createElement('div')
  container.innerHTML = value
  return container.textContent ?? container.innerText ?? ''
}

function plainTextFromEditorJs(propsJson: string | null) {
  if (!propsJson) return ''

  try {
    const parsed = JSON.parse(propsJson)

    if (!parsed || typeof parsed !== 'object' || !Array.isArray(parsed.blocks)) {
      return ''
    }

    return parsed.blocks
      .map((block: any) => {
        const data = block?.data

        if (typeof data?.text === 'string') {
          return stripHtml(data.text)
        }

        if (Array.isArray(data?.items)) {
          return data.items
            .map((item: any) => {
              if (typeof item === 'string') return stripHtml(item)
              if (typeof item?.text === 'string') return stripHtml(item.text)
              return ''
            })
            .filter(Boolean)
            .join(' ')
        }

        return ''
      })
      .filter(Boolean)
      .join('\n')
  } catch {
    return ''
  }
}

function normalizeBlock(block: BlockResponse): EditableBlock {
  if (block.type === 'editorjs') {
    return {
      ...block,
      type: 'paragraph',
      textContent:
        block.textContent?.trim() ||
        plainTextFromEditorJs(block.propsJson) ||
        '',
      propsJson: null,
    }
  }

  return block as EditableBlock
}

function sortBlocks(blocks: EditableBlock[]) {
  return [...blocks].sort((left, right) => {
    const byParent = String(left.parentBlockId ?? '').localeCompare(
      String(right.parentBlockId ?? '')
    )

    if (byParent !== 0) return byParent

    const byOrder = String(left.orderKey ?? '').localeCompare(
      String(right.orderKey ?? '')
    )

    if (byOrder !== 0) return byOrder

    return left.createdDate.localeCompare(right.createdDate)
  })
}

function getBlockText(block: EditableBlock) {
  return block.textContent ?? ''
}

function buildPropsJson(block: EditableBlock, text: string) {
  if (block.type === 'todo') {
    return JSON.stringify({
      checked: false,
      text,
    })
  }

  return null
}

export function useCollaborativePageEditor(
  options: UseCollaborativePageEditorOptions
) {
  const blocks = ref<EditableBlock[]>([])
  const currentRevision = ref(0)

  const isLoading = ref(false)
  const isReloading = ref(false)
  const error = ref<string | null>(null)
  const joinedPageId = ref<Guid | null>(null)
  const activeBlockId = ref<Guid | null>(null)

  const blockStates = reactive<Record<Guid, BlockEditorState>>({})
  const blockElements = new Map<Guid, HTMLElement>()

  const saveTimers = new Map<Guid, number>()
  const draftTimers = new Map<Guid, number>()

  let leaseRenewTimer: number | null = null
  let heartbeatTimer: number | null = null

  let unsubscribeBlockCreated: (() => void) | null = null
  let unsubscribeBlockUpdated: (() => void) | null = null
  let unsubscribeBlockDeleted: (() => void) | null = null
  let unsubscribeBlockDraftChanged: (() => void) | null = null
  let unsubscribeBlockEditingStateChanged: (() => void) | null = null
  let unsubscribePagePresenceChanged: (() => void) | null = null

  const hasPage = computed(() => Boolean(options.pageId.value))
  const isConnected = computed(() => realtimeClient.state.status === 'connected')

  function getDisplayName() {
    return options.holderDisplayName?.value || 'Editor'
  }

  function getEditorSessionId() {
    const connectionId = realtimeClient.state.connectionId

    if (!connectionId) {
      throw new Error('Chưa có realtime connectionId.')
    }

    return connectionId
  }

  function getBlockState(blockId: Guid, fallbackText = '') {
    if (!blockStates[blockId]) {
      blockStates[blockId] = emptyBlockState(fallbackText)
    }

    return blockStates[blockId]
  }

  function setBlockRef(blockId: Guid, element: Element | null) {
    if (element instanceof HTMLElement) {
      blockElements.set(blockId, element)
      return
    }

    blockElements.delete(blockId)
  }

  function clearTimers() {
    for (const timer of saveTimers.values()) {
      window.clearTimeout(timer)
    }

    for (const timer of draftTimers.values()) {
      window.clearTimeout(timer)
    }

    saveTimers.clear()
    draftTimers.clear()

    if (leaseRenewTimer) {
      window.clearInterval(leaseRenewTimer)
      leaseRenewTimer = null
    }

    if (heartbeatTimer) {
      window.clearInterval(heartbeatTimer)
      heartbeatTimer = null
    }
  }

  function resetLocalState() {
    blocks.value = []
    currentRevision.value = 0
    activeBlockId.value = null
    error.value = null

    for (const key of Object.keys(blockStates)) {
      delete blockStates[key]
    }

    clearTimers()
  }

  function startHeartbeat(pageId: Guid) {
    if (heartbeatTimer) {
      window.clearInterval(heartbeatTimer)
    }

    heartbeatTimer = window.setInterval(() => {
      void realtimeClient.heartbeatPage(pageId).catch(() => {
        // heartbeat fail thì reconnect flow sẽ lo, đừng spam UI.
      })
    }, 15_000)
  }

  function startLeaseRenewal(blockId: Guid) {
    if (leaseRenewTimer) {
      window.clearInterval(leaseRenewTimer)
    }

    leaseRenewTimer = window.setInterval(() => {
      const state = getBlockState(blockId)

      if (!state.isLeaseHeld) return

      const editorSessionId = realtimeClient.state.connectionId

      if (!editorSessionId) return

      void blockController
        .renewLease(blockId, {
          editorSessionId,
        })
        .then((result) => {
          if (!result.isSuccess || !result.data?.granted) {
            state.isLeaseHeld = false
            state.isLeaseDenied = true
            state.holderDisplayName =
              result.data?.holderDisplayName ?? 'người khác'
          }
        })
        .catch(() => {
          // save kế tiếp sẽ báo rõ hơn.
        })
    }, 18_000)
  }

  async function ensureRealtimeJoined(pageId: Guid) {
    await realtimeClient.start()
    await realtimeClient.joinPage(pageId)

    joinedPageId.value = pageId
    startHeartbeat(pageId)
  }

  async function releaseBlockLease(blockId: Guid) {
    const state = getBlockState(blockId)
    const editorSessionId = realtimeClient.state.connectionId

    if (!editorSessionId || !state.isLeaseHeld) {
      state.isLeaseHeld = false
      return
    }

    try {
      await realtimeClient.sendBlockEditingState({
        pageId: options.pageId.value as Guid,
        blockId,
        editorSessionId,
        isEditing: false,
      })
    } catch {
      // ignore
    }

    try {
      await blockController.releaseLease(blockId, {
        editorSessionId,
      })
    } catch {
      // server vẫn tự clear khi disconnect hoặc lease hết hạn.
    } finally {
      state.isLeaseHeld = false
      state.isLeaseDenied = false
      state.localDraftText = null

      if (activeBlockId.value === blockId) {
        activeBlockId.value = null
      }

      if (leaseRenewTimer) {
        window.clearInterval(leaseRenewTimer)
        leaseRenewTimer = null
      }
    }
  }

  async function releaseActiveBlock(exceptBlockId?: Guid) {
    const current = activeBlockId.value

    if (!current || current === exceptBlockId) return

    await flushBlock(current)
    await releaseBlockLease(current)
  }

  function syncDomText(blockId: Guid, text: string) {
    const element = blockElements.get(blockId)

    if (!element) return
    if (document.activeElement === element) return
    if (element.innerText === text) return

    element.innerText = text
  }

  function setBlocksFromDocument(document: PageDocumentResponse) {
    currentRevision.value = document.currentRevision

    const nextBlocks = document.blocks
      .map(normalizeBlock)
      .filter((block) => editableTypes.has(block.type))

    blocks.value = sortBlocks(nextBlocks)

    for (const block of blocks.value) {
      const state = getBlockState(block.id, getBlockText(block))
      state.lastSavedText = getBlockText(block)

      if (!state.isLeaseHeld) {
        state.localDraftText = null
      }

      if (!state.isLeaseHeld && !state.remoteDraftText) {
        syncDomText(block.id, getBlockText(block))
      }
    }
  }

  async function createInitialBlock(pageId: Guid) {
    const result = await blockController.create(pageId, {
      expectedRevision: currentRevision.value,
      type: 'paragraph',
      textContent: '',
      propsJson: null,
      parentBlockId: null,
      previousBlockId: null,
      nextBlockId: null,
      schemaVersion: 1,
    })

    if (!result.isSuccess || !result.data?.block) {
      throw new Error(
        getApiResultErrorMessage(result, 'Không tạo được block đầu tiên.')
      )
    }

    currentRevision.value = result.data.appliedRevision

    const block = normalizeBlock(result.data.block)
    blocks.value = sortBlocks([block])
    getBlockState(block.id, '')

    return block
  }

  async function loadPageDocument(pageId: Guid) {
    const result = await blockController.listByPage(pageId)

    if (!result.isSuccess || !result.data) {
      throw new Error(
        getApiResultErrorMessage(result, 'Không tải được document.')
      )
    }

    setBlocksFromDocument(result.data)

    if (blocks.value.length === 0) {
      await createInitialBlock(pageId)
    }

    await nextTick()

    for (const block of blocks.value) {
      const state = getBlockState(block.id, getBlockText(block))
      const text = getVisibleText(block)
      syncDomText(block.id, text)
      state.error = null
    }
  }

  async function reloadPageDocument() {
    const pageId = options.pageId.value

    if (!pageId || isReloading.value) return

    isReloading.value = true

    try {
      await loadPageDocument(pageId)
    } catch (reloadError) {
      error.value = getApiErrorMessage(
        reloadError,
        'Không reload được document.'
      )
    } finally {
      isReloading.value = false
    }
  }

  async function load() {
    const pageId = options.pageId.value

    resetLocalState()

    if (!pageId) return

    isLoading.value = true

    try {
      // Register handlers before starting connection to avoid missing server broadcasts
      bindRealtimeEvents()
      await ensureRealtimeJoined(pageId)
      await loadPageDocument(pageId)
    } catch (loadError) {
      error.value = getApiErrorMessage(loadError, 'Không tải được page này.')
    } finally {
      isLoading.value = false
    }
  }

  function getVisibleText(block: EditableBlock) {
    const state = getBlockState(block.id, getBlockText(block))

    if (state.localDraftText !== null) {
      return state.localDraftText
    }

    if (state.remoteDraftText !== null && !state.isLeaseHeld) {
      return state.remoteDraftText
    }

    return getBlockText(block)
  }

  function getBlockClass(block: EditableBlock) {
    const state = getBlockState(block.id, getBlockText(block))

    return {
      'collab-block--editing': state.isLeaseHeld,
      'collab-block--locked': state.isLeaseDenied,
      'collab-block--remote-editing': state.isRemoteEditing,
      'collab-block--remote-draft': Boolean(
        state.remoteDraftText !== null && !state.isLeaseHeld
      ),
      'collab-block--saving': state.isSaving,
      'collab-block--dirty': state.isDirty,
    }
  }

  function canType(block: EditableBlock) {
    const state = getBlockState(block.id, getBlockText(block))
    return state.isLeaseHeld && !state.isSaving
  }

  function isEmptyText(value: string) {
    return value.replace(/\u00a0/g, ' ').trim().length === 0
  }

  async function acquireBlock(block: EditableBlock) {
    const pageId = options.pageId.value

    if (!pageId) return false

    await ensureRealtimeJoined(pageId)
    await releaseActiveBlock(block.id)

    const state = getBlockState(block.id, getBlockText(block))

    if (state.isLeaseHeld) return true

    try {
      const editorSessionId = getEditorSessionId()

      const result = await blockController.acquireLease(block.id, {
        editorSessionId,
        holderDisplayName: getDisplayName(),
      })

      if (!result.isSuccess || !result.data?.granted) {
        state.isLeaseHeld = false
        state.isLeaseDenied = true
        state.holderDisplayName =
          result.data?.holderDisplayName ?? 'người khác'
        return false
      }

      state.isLeaseHeld = true
      state.isLeaseDenied = false
      state.holderDisplayName = null
      state.error = null
      state.localDraftText = getVisibleText(block)
      state.lastSavedText = getBlockText(block)

      activeBlockId.value = block.id
      startLeaseRenewal(block.id)

      await realtimeClient.sendBlockEditingState({
        pageId,
        blockId: block.id,
        editorSessionId,
        isEditing: true,
      })

      return true
    } catch (acquireError: any) {
      if (getHttpStatus(acquireError) === 409) {
        state.isLeaseHeld = false
        state.isLeaseDenied = true
        state.holderDisplayName =
          acquireError?.data?.data?.holderDisplayName ??
          acquireError?.data?.holderDisplayName ??
          'người khác'
        return false
      }

      state.error = getApiErrorMessage(
        acquireError,
        'Không lấy được quyền sửa block.'
      )
      return false
    }
  }

  function scheduleDraft(block: EditableBlock) {
    const state = getBlockState(block.id, getBlockText(block))

    if (!state.isLeaseHeld) return

    const oldTimer = draftTimers.get(block.id)

    if (oldTimer) {
      window.clearTimeout(oldTimer)
    }

    const timer = window.setTimeout(() => {
      draftTimers.delete(block.id)

      const pageId = options.pageId.value
      const editorSessionId = realtimeClient.state.connectionId

      if (!pageId || !editorSessionId || !state.isLeaseHeld) return

      state.clientSequence += 1

      void realtimeClient
        .sendBlockDraft({
          pageId,
          blockId: block.id,
          editorSessionId,
          baseRevision: currentRevision.value,
          clientSequence: state.clientSequence,
          type: block.type,
          textContent: state.localDraftText ?? '',
          propsJson: buildPropsJson(block, state.localDraftText ?? ''),
        })
        .catch(() => {
          // draft realtime fail không được phá flow save thật.
        })
    }, 90)

    draftTimers.set(block.id, timer)
  }

  function scheduleSave(block: EditableBlock) {
    const state = getBlockState(block.id, getBlockText(block))

    if (!state.isLeaseHeld) return

    const oldTimer = saveTimers.get(block.id)

    if (oldTimer) {
      window.clearTimeout(oldTimer)
    }

    const timer = window.setTimeout(() => {
      saveTimers.delete(block.id)
      void saveBlock(block.id)
    }, 900)

    saveTimers.set(block.id, timer)
  }

  async function flushBlock(blockId: Guid) {
    const timer = saveTimers.get(blockId)

    if (timer) {
      window.clearTimeout(timer)
      saveTimers.delete(blockId)
    }

    await saveBlock(blockId)
  }

  function findBlock(blockId: Guid) {
    return blocks.value.find((block) => block.id === blockId) ?? null
  }

  async function saveBlock(blockId: Guid, retryOnConflict = true) {
    const block = findBlock(blockId)
    if (!block) return

    const state = getBlockState(block.id, getBlockText(block))

    if (state.isSaving || !state.isDirty) return

    // Ensure lease is held before attempting to save
    if (!state.isLeaseHeld) {
      const acquired = await acquireBlock(block)
      if (!acquired) {
        state.error = 'Không thể lấy quyền sửa block. Vui lòng thử lại.'
        return
      }
    }

    const pageId = options.pageId.value
    const editorSessionId = realtimeClient.state.connectionId
    const text = state.localDraftText ?? ''

    if (!pageId || !editorSessionId) return

    state.isSaving = true
    state.error = null

    try {
      // Try to renew lease immediately before saving to avoid TTL race.
      try {
        const renewResult = await blockController.renewLease(block.id, {
          editorSessionId,
        })

        if (!renewResult.isSuccess || !renewResult.data?.granted) {
          // If we can't renew, mark lease lost and try to reacquire before saving.
          state.isLeaseHeld = false
          state.isLeaseDenied = true
          state.holderDisplayName =
            renewResult.data?.holderDisplayName ?? 'người khác'

          const reacquired = await acquireBlock(block)
          if (!reacquired) {
            state.error = 'Không thể gia hạn hoặc lấy lại lease. Vui lòng thử lại.'
            return
          }
        }
      } catch {
        // ignore renew errors; update will surface failure if necessary
      }
      const result = await blockController.update(block.id, {
        expectedRevision: currentRevision.value,
        editorSessionId,
        type: block.type === 'editorjs' ? 'paragraph' : block.type,
        textContent: text,
        propsJson: buildPropsJson(block, text),
      })

      if (!result.isSuccess || !result.data?.block) {
        throw new Error(
          getApiResultErrorMessage(result, 'Không lưu được block.')
        )
      }

      applyMutation(result.data)
      state.lastSavedText = text
      state.isDirty = false
      state.localDraftText = text
      state.remoteDraftText = null
      state.remoteUserName = null
    } catch (saveError: any) {
      if (getHttpStatus(saveError) === 409 && retryOnConflict) {
        // On conflict, reload and reacquire lease
        await reloadPageDocument()

        const latestBlock = findBlock(blockId)

        if (latestBlock) {
          const latestState = getBlockState(blockId, getBlockText(latestBlock))
          latestState.localDraftText = text
          latestState.isDirty = text !== latestState.lastSavedText
          
          // Reacquire lease before retry
          const reacquired = await acquireBlock(latestBlock)
          if (reacquired) {
            await saveBlock(blockId, false)
          }
        }

        return
      }

      state.error = getApiErrorMessage(saveError, 'Không lưu được block.')

      if (getHttpStatus(saveError) === 409) {
        state.isLeaseHeld = false
        state.isLeaseDenied = true
        state.holderDisplayName = 'người khác'
      }
    } finally {
      state.isSaving = false
    }
  }

  function applyMutation(mutation: BlockMutationResponse) {
    if (mutation.appliedRevision > currentRevision.value) {
      currentRevision.value = mutation.appliedRevision
    }

    if (!mutation.block) return

    const block = normalizeBlock(mutation.block)
    const index = blocks.value.findIndex((item) => item.id === block.id)

    if (index >= 0) {
      blocks.value.splice(index, 1, block)
    } else {
      blocks.value.push(block)
    }

    blocks.value = sortBlocks(blocks.value)

    const state = getBlockState(block.id, getBlockText(block))
    state.lastSavedText = getBlockText(block)

    if (!state.isLeaseHeld) {
      state.localDraftText = null
      state.remoteDraftText = null
      state.remoteUserName = null

      nextTick(() => {
        syncDomText(block.id, getBlockText(block))
      })
    }
  }

  async function createBlockAfter(block: EditableBlock | null) {
    const pageId = options.pageId.value

    if (!pageId) return

    if (block) {
      await flushBlock(block.id)
    }

    try {
      const result = await blockController.create(pageId, {
        expectedRevision: currentRevision.value,
        type: 'paragraph',
        textContent: '',
        propsJson: null,
        parentBlockId: block?.parentBlockId ?? null,
        previousBlockId: block?.id ?? null,
        nextBlockId: null,
        schemaVersion: 1,
      })

      if (!result.isSuccess || !result.data?.block) {
        throw new Error(
          getApiResultErrorMessage(result, 'Không tạo được block mới.')
        )
      }

      applyMutation(result.data)

      await nextTick()

      const newBlockId = result.data.block.id
      const element = blockElements.get(newBlockId)

      element?.focus()

      const newBlock = findBlock(newBlockId)

      if (newBlock) {
        await acquireBlock(newBlock)
      }
    } catch (createError: any) {
      if (getHttpStatus(createError) === 409) {
        await reloadPageDocument()
        return
      }

      error.value = getApiErrorMessage(createError, 'Không tạo được block mới.')
    }
  }

  async function deleteBlock(block: EditableBlock, focusIndexAfterDelete: number) {
    const pageId = options.pageId.value
    const state = getBlockState(block.id, getBlockText(block))
    const editorSessionId = realtimeClient.state.connectionId

    if (!pageId || !editorSessionId || !state.isLeaseHeld) return

    if (blocks.value.length <= 1) {
      state.localDraftText = ''
      state.isDirty = state.lastSavedText !== ''
      syncDomText(block.id, '')
      scheduleSave(block)
      return
    }

    try {
      const result = await blockController.delete(block.id, {
        expectedRevision: currentRevision.value,
        editorSessionId,
        note: 'Deleted from collaborative editor',
      })

      if (!result.isSuccess || !result.data) {
        throw new Error(
          getApiResultErrorMessage(result, 'Không xóa được block.')
        )
      }

      currentRevision.value = result.data.appliedRevision
      blocks.value = blocks.value.filter((item) => item.id !== block.id)
      delete blockStates[block.id]

      await nextTick()

      const nextBlock =
        blocks.value[Math.max(0, focusIndexAfterDelete - 1)] ?? blocks.value[0]

      if (nextBlock) {
        blockElements.get(nextBlock.id)?.focus()
      }
    } catch (deleteError: any) {
      if (getHttpStatus(deleteError) === 409) {
        await reloadPageDocument()
        return
      }

      state.error = getApiErrorMessage(deleteError, 'Không xóa được block.')
    }
  }

  async function handleFocus(block: EditableBlock) {
    await acquireBlock(block)
  }

  function handleBeforeInput(event: InputEvent, block: EditableBlock) {
    if (canType(block)) return

    event.preventDefault()

    const state = getBlockState(block.id, getBlockText(block))

    if (state.isLeaseDenied) {
      state.error = `Block đang được ${state.holderDisplayName ?? 'người khác'} chỉnh sửa.`
    }
  }

  function handleInput(event: Event, block: EditableBlock) {
    const state = getBlockState(block.id, getBlockText(block))

    if (!state.isLeaseHeld) return

    const element = event.currentTarget as HTMLElement
    const text = element.innerText.replace(/\n{3,}/g, '\n\n')

    state.localDraftText = text
    state.isDirty = text !== state.lastSavedText
    state.error = null

    scheduleDraft(block)
    scheduleSave(block)
  }

  async function handleBlur(block: EditableBlock) {
    await flushBlock(block.id)
  }

  async function handleEnter(
    event: KeyboardEvent,
    block: EditableBlock
  ) {
    if (event.shiftKey && block.type === 'code') return

    event.preventDefault()
    await createBlockAfter(block)
  }

  async function handleBackspace(
    event: KeyboardEvent,
    block: EditableBlock,
    index: number
  ) {
    const state = getBlockState(block.id, getBlockText(block))
    const text = state.localDraftText ?? getBlockText(block)

    if (!isEmptyText(text)) return

    event.preventDefault()

    if (!state.isLeaseHeld) {
      await acquireBlock(block)
    }

    await deleteBlock(block, index)
  }

  async function changeBlockType(block: EditableBlock, type: EditableBlockType) {
    const acquired = await acquireBlock(block)

    if (!acquired) return

    block.type = type

    const state = getBlockState(block.id, getBlockText(block))
    state.isDirty = true

    await saveBlock(block.id)
  }

  function getPlaceholder(block: EditableBlock) {
    if (block.type === 'heading_1') return 'Heading 1'
    if (block.type === 'heading_2') return 'Heading 2'
    if (block.type === 'heading_3') return 'Heading 3'
    if (block.type === 'quote') return 'Quote'
    if (block.type === 'code') return 'Code'
    if (block.type === 'todo') return 'Todo'
    return "Gõ gì đó, hoặc Enter để tạo block mới..."
  }

  function handleRemoteDraft(envelope: RealtimeEnvelope<BlockDraftPayload>) {
    const payload = envelope.payload
    const pageId = options.pageId.value

    if (!pageId || !payload || payload.pageId !== pageId) return
    if (payload.connectionId === realtimeClient.state.connectionId) return

    const block = findBlock(payload.blockId)
    if (!block) return

    const state = getBlockState(payload.blockId, getBlockText(block))

    if (payload.clientSequence <= state.remoteDraftSequence) return

    state.remoteDraftSequence = payload.clientSequence
    state.remoteDraftText = payload.textContent ?? ''
    state.remoteUserName = payload.userName ?? 'người khác'
    state.isRemoteEditing = true

    if (!state.isLeaseHeld) {
      nextTick(() => {
        syncDomText(payload.blockId, state.remoteDraftText ?? '')
      })
    }
  }

  function handleRemoteEditingState(
    envelope: RealtimeEnvelope<BlockEditingStatePayload>
  ) {
    const payload = envelope.payload
    const pageId = options.pageId.value

    if (!pageId || !payload || payload.pageId !== pageId) return
    if (payload.connectionId === realtimeClient.state.connectionId) return

    const block = findBlock(payload.blockId)
    if (!block) return

    const state = getBlockState(payload.blockId, getBlockText(block))
    state.isRemoteEditing = payload.isEditing
    state.remoteUserName = payload.userName ?? 'người khác'

    if (!payload.isEditing) {
      state.remoteDraftText = null
      state.remoteDraftSequence = -1
    }
  }

  function handlePagePresenceChanged(envelope: RealtimeEnvelope<unknown>) {
    // Page presence change - currently just a notification
    // Can be used for showing who's currently viewing the page
    // For now, we don't need to do anything specific
  }

  async function handleBlockMutation(envelope: RealtimeEnvelope<unknown>) {
    const pageId = options.pageId.value

    if (!pageId) return

    const mutation = normalizeMutationPayload(envelope)

    if (!mutation || mutation.pageId !== pageId) return

    const revision = envelope.revision ?? mutation.appliedRevision

    if (revision <= currentRevision.value) return

    if (revision !== currentRevision.value + 1) {
      await reloadPageDocument()
      return
    }

    applyMutation(mutation)
  }

  async function handleBlockDeleted(envelope: RealtimeEnvelope<unknown>) {
    const pageId = options.pageId.value

    if (!pageId) return

    if (envelope.pageId && envelope.pageId !== pageId) return

    const revision = envelope.revision

    if (revision !== null && revision !== undefined) {
      if (revision <= currentRevision.value) return

      if (revision !== currentRevision.value + 1) {
        await reloadPageDocument()
        return
      }

      currentRevision.value = revision
    }

    const deletedIds = normalizeDeletedBlockIds(envelope.payload)

    if (deletedIds.length === 0) {
      await reloadPageDocument()
      return
    }

    blocks.value = blocks.value.filter((block) => !deletedIds.includes(block.id))

    for (const id of deletedIds) {
      delete blockStates[id]
      blockElements.delete(id)
    }

    if (blocks.value.length === 0) {
      await reloadPageDocument()
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
        handleRemoteDraft
      )
    }

    if (!unsubscribeBlockEditingStateChanged) {
      unsubscribeBlockEditingStateChanged = realtimeClient.on(
        'BlockEditingStateChanged',
        handleRemoteEditingState
      )
    }

    if (!unsubscribePagePresenceChanged) {
      unsubscribePagePresenceChanged = realtimeClient.on(
        'PagePresenceChanged',
        handlePagePresenceChanged
      )
    }
  }

  function unbindRealtimeEvents() {
    unsubscribeBlockCreated?.()
    unsubscribeBlockUpdated?.()
    unsubscribeBlockDeleted?.()
    unsubscribeBlockDraftChanged?.()
    unsubscribeBlockEditingStateChanged?.()
    unsubscribePagePresenceChanged?.()

    unsubscribeBlockCreated = null
    unsubscribeBlockUpdated = null
    unsubscribeBlockDeleted = null
    unsubscribeBlockDraftChanged = null
    unsubscribeBlockEditingStateChanged = null
    unsubscribePagePresenceChanged = null
  }

  watch(
    () => options.pageId.value,
    async (nextPageId, previousPageId) => {
      if (previousPageId) {
        await releaseActiveBlock()

        try {
          await realtimeClient.leavePage(previousPageId)
        } catch {
          // ignore
        }
      }

      await load()
    },
    { immediate: true }
  )

  watch(
    () => realtimeClient.state.status,
    (status) => {
      if (status === 'connected' && options.pageId.value) {
        void ensureRealtimeJoined(options.pageId.value)
        void reloadPageDocument()
      }
    }
  )

  onBeforeUnmount(() => {
    unbindRealtimeEvents()
    clearTimers()

    void releaseActiveBlock()

    if (joinedPageId.value) {
      void realtimeClient.leavePage(joinedPageId.value).catch(() => {
        // ignore
      })
    }
  })

  return {
    blocks,
    blockStates,
    currentRevision,
    isLoading,
    isReloading,
    isConnected,
    error,
    hasPage,

    setBlockRef,
    getBlockState,
    getVisibleText,
    getBlockClass,
    getPlaceholder,
    canType,

    load,
    reloadPageDocument,
    acquireBlock,
    releaseBlockLease,
    createBlockAfter,
    changeBlockType,

    handleFocus,
    handleBeforeInput,
    handleInput,
    handleBlur,
    handleEnter,
    handleBackspace,
  }
}
