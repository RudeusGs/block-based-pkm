<template>
  <Teleport to="body">
    <Transition name="messenger-panel">
      <section
        v-if="open"
        class="messenger-layer"
        role="dialog"
        aria-modal="true"
        aria-label="Messenger"
      >
        <button
          class="messenger-scrim"
          type="button"
          aria-label="Đóng messenger"
          @click="close"
        ></button>

        <aside
          class="messenger-shell"
          @click.stop
          @keydown.esc="close"
        >
          <section class="conversation-sidebar">
            <header class="conversation-head">
              <div>
                <p>Realtime</p>
                <h2>Messages</h2>
              </div>

              <button
                type="button"
                title="Đóng"
                @click="close"
              >
                <span class="material-symbols-outlined">close</span>
              </button>
            </header>

            <div class="conversation-toolbar">
              <button
                type="button"
                :disabled="isLoadingConversations"
                @click="loadConversations"
              >
                <span class="material-symbols-outlined">refresh</span>
                Sync
              </button>

              <span
                class="messenger-status-dot"
                :class="{ connected: realtimeClient.isConnected.value }"
                :title="
                  realtimeClient.isConnected.value
                    ? 'Realtime connected'
                    : 'Realtime disconnected'
                "
              ></span>
            </div>

            <div
              v-if="conversationError"
              class="messenger-error compact"
            >
              {{ conversationError }}
            </div>

            <div
              v-if="isLoadingConversations && !conversations.length"
              class="conversation-skeleton"
            >
              <span v-for="index in 6" :key="index"></span>
            </div>

            <div
              v-else-if="!conversations.length"
              class="conversation-empty"
            >
              <span class="material-symbols-outlined">forum</span>
              <strong>Chưa có tin nhắn</strong>
              <p>Vào Friends rồi bấm Message để mở chat riêng.</p>
            </div>

            <div
              v-else
              class="conversation-list"
            >
              <button
                v-for="conversation in conversations"
                :key="conversation.id"
                type="button"
                class="conversation-row"
                :class="{ active: selectedConversationId === conversation.id }"
                @click="selectConversation(conversation)"
              >
                <img
                  v-if="avatarUrl(conversation.otherUser.avatarUrl)"
                  :src="avatarUrl(conversation.otherUser.avatarUrl) || ''"
                  :alt="conversation.otherUser.fullName"
                  referrerpolicy="no-referrer"
                />

                <span v-else class="conversation-avatar-fallback">
                  {{ initials(conversation.otherUser.fullName || conversation.otherUser.userName) }}
                </span>

                <span class="conversation-copy">
                  <span>
                    <strong>{{ conversation.otherUser.fullName }}</strong>
                    <time>{{ formatConversationTime(conversation.lastMessageAtUtc) }}</time>
                  </span>

                  <small>
                    {{ conversation.lastMessagePreview || 'Bắt đầu cuộc trò chuyện' }}
                  </small>
                </span>

                <span
                  v-if="conversation.unreadCount > 0"
                  class="conversation-badge"
                >
                  {{ conversation.unreadCount > 99 ? '99+' : conversation.unreadCount }}
                </span>
              </button>
            </div>
          </section>

          <section class="chat-pane">
            <template v-if="!selectedConversation">
              <div class="chat-empty-state">
                <span class="material-symbols-outlined">chat</span>
                <strong>Chọn một cuộc trò chuyện</strong>
                <p>Tin nhắn riêng chỉ hoạt động khi hai người đã là bạn bè.</p>
              </div>
            </template>

            <template v-else>
              <header class="chat-head">
                <button
                  type="button"
                  class="chat-back-btn"
                  @click="selectedConversationId = null"
                >
                  <span class="material-symbols-outlined">chevron_left</span>
                </button>

                <img
                  v-if="avatarUrl(selectedConversation.otherUser.avatarUrl)"
                  :src="avatarUrl(selectedConversation.otherUser.avatarUrl) || ''"
                  :alt="selectedConversation.otherUser.fullName"
                  referrerpolicy="no-referrer"
                />

                <span v-else class="chat-avatar-fallback">
                  {{ initials(selectedConversation.otherUser.fullName || selectedConversation.otherUser.userName) }}
                </span>

                <div>
                  <strong>{{ selectedConversation.otherUser.fullName }}</strong>
                  <span>@{{ selectedConversation.otherUser.userName }}</span>
                </div>
              </header>

              <main
                ref="messageListRef"
                class="message-list"
              >
                <button
                  v-if="hasMoreMessages"
                  type="button"
                  class="load-more-messages"
                  :disabled="isLoadingMoreMessages"
                  @click="loadMoreMessages"
                >
                  {{ isLoadingMoreMessages ? 'Loading...' : 'Load older messages' }}
                </button>

                <div
                  v-if="isLoadingMessages && !messages.length"
                  class="message-skeleton"
                >
                  <span v-for="index in 5" :key="index"></span>
                </div>

                <div
                  v-else-if="messageError"
                  class="messenger-error"
                >
                  {{ messageError }}
                </div>

                <div
                  v-else-if="!messages.length"
                  class="chat-empty-row"
                >
                  Hãy gửi tin nhắn đầu tiên. Đừng gửi “alo” rồi offline nha.
                </div>

                <article
                  v-for="message in messages"
                  :key="message.id"
                  class="message-bubble-row"
                  :class="{ mine: message.isMine }"
                >
                  <div
                    class="message-bubble"
                    :class="{ 'workspace-share-bubble': isWorkspaceShareMessage(message) }"
                  >
                    <template v-if="isWorkspaceShareMessage(message)">
                      <div class="message-workspace-card">
                        <span class="message-workspace-icon">
                          {{ workspaceShareInitial(message) }}
                        </span>

                        <div class="message-workspace-copy">
                          <span class="message-workspace-kicker">Workspace</span>
                          <strong>{{ workspaceSharePayload(message)?.workspaceName || 'Workspace' }}</strong>
                          <p>
                            {{ workspaceSharePayload(message)?.workspaceDescription || 'Workspace được chia sẻ qua Messenger.' }}
                          </p>

                          <div class="message-workspace-meta">
                            <span>{{ workspaceShareVisibilityLabel(workspaceSharePayload(message)?.workspaceVisibility) }}</span>
                            <span>·</span>
                            <span>{{ workspaceShareRoleLabel(workspaceSharePayload(message)?.grantedRole) }}</span>
                          </div>
                        </div>
                      </div>

                      <button
                        v-if="!message.isMine"
                        type="button"
                        class="message-workspace-action"
                        :class="{ accepted: isWorkspaceShareAccepted(message) }"
                        :disabled="isWorkspaceShareActionDisabled(message)"
                        :title="workspaceShareActionTitle(message)"
                        @click="acceptWorkspaceShare(message)"
                      >
                        <span
                          v-if="acceptingWorkspaceShareMessageId === message.id"
                          class="message-workspace-action-spinner"
                        ></span>
                        <span v-else class="material-symbols-outlined">
                          {{ workspaceShareActionIcon(message) }}
                        </span>
                        <span>{{ workspaceShareActionLabel(message) }}</span>
                      </button>

                      <span v-else class="message-workspace-sent-note">
                        Bạn đã share workspace này. Người nhận bấm card là vào được.
                      </span>
                    </template>

                    <template v-else>
                      <img
                        v-if="message.imageUrl"
                        class="message-image"
                        :src="normalizeMessageImage(message.imageUrl)"
                        alt="Message image"
                        referrerpolicy="no-referrer"
                      />

                      <p v-if="message.body">
                        {{ message.body }}
                      </p>
                    </template>

                    <time>
                      {{ formatMessageTime(message.createdDate) }}
                      <span v-if="message.isMine && message.readAtUtc">· Seen</span>
                    </time>
                  </div>
                </article>
              </main>

              <div
                v-if="selectedTypingUser"
                class="message-typing-indicator"
              >
                <span class="typing-dots" aria-hidden="true">
                  <i></i>
                  <i></i>
                  <i></i>
                </span>
                <strong>{{ selectedTypingUser }}</strong> đang nhập...
              </div>

              <footer class="message-composer">
                <div
                  v-if="selectedImageFile"
                  class="selected-image-preview"
                >
                  <span>
                    <span class="material-symbols-outlined">image</span>
                    {{ selectedImageFile.name }}
                  </span>

                  <button
                    type="button"
                    @click="clearSelectedImage"
                  >
                    <span class="material-symbols-outlined">close</span>
                  </button>
                </div>

                <div class="message-input-row">
                  <button
                    type="button"
                    title="Gửi ảnh"
                    :disabled="isSendingMessage"
                    @click="triggerImageInput"
                  >
                    <span class="material-symbols-outlined">image</span>
                  </button>

                  <input
                    ref="imageInputRef"
                    type="file"
                    accept="image/*"
                    class="d-none"
                    @change="handleImageSelected"
                  />

                  <textarea
                    ref="messageInputRef"
                    v-model="messageDraft"
                    rows="1"
                    maxlength="2000"
                    placeholder="Nhắn gì đó..."
                    :disabled="isSendingMessage"
                    @keydown.enter.exact.prevent="sendCurrentMessage"
                  ></textarea>

                  <button
                    type="button"
                    title="Gửi"
                    :disabled="!canSendMessage || isSendingMessage"
                    @click="sendCurrentMessage"
                  >
                    <span class="material-symbols-outlined">send</span>
                  </button>
                </div>
              </footer>
            </template>
          </section>
        </aside>
      </section>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import {
  computed,
  nextTick,
  onBeforeUnmount,
  ref,
  watch,
} from 'vue'
import { messagingController } from '@/api/services/messaging.api'
import { meController } from '@/api/services/me.api'
import {
  getApiErrorMessage,
  getApiResultErrorMessage,
} from '@/api/utils/api-error.util'
import type { Guid } from '@/api/models/common.model'
import type {
  ConversationResponse,
  MessageResponse,
  WorkspaceShareMessagePayload,
} from '@/api/models/messaging.model'
import type { WorkspaceResponse } from '@/api/models/workspace.model'
import { realtimeClient } from '@/realtime/realtime.client'
import type { RealtimeEnvelope } from '@/realtime/realtime.types'
import { normalizeImageUrl } from '@/utils/image-url.util'
import { useToast } from '@/components/composables/useToast'

const props = defineProps<{
  open: boolean
  startUserId?: Guid | null
}>()

const emit = defineEmits<{
  close: []
  started: []
  'workspace-opened': [workspace: WorkspaceResponse]
}>()

const toast = useToast()

const currentUserId = ref<Guid | null>(null)
const conversations = ref<ConversationResponse[]>([])
const selectedConversationId = ref<Guid | null>(null)
const messages = ref<MessageResponse[]>([])
const totalMessages = ref(0)
const messagePageNumber = ref(1)

const isLoadingConversations = ref(false)
const isLoadingMessages = ref(false)
const isLoadingMoreMessages = ref(false)
const isSendingMessage = ref(false)

const conversationError = ref<string | null>(null)
const messageError = ref<string | null>(null)

const messageDraft = ref('')
const selectedImageFile = ref<File | null>(null)
const acceptingWorkspaceShareMessageId = ref<Guid | null>(null)
const joinedWorkspaceIds = ref<Set<Guid>>(new Set())
const typingUsersByConversationId = ref<
  Record<Guid, { userName: string | null; isTyping: boolean }>
>({})

const messageListRef = ref<HTMLElement | null>(null)
const messageInputRef = ref<HTMLTextAreaElement | null>(null)
const imageInputRef = ref<HTMLInputElement | null>(null)

let joinedConversationId: Guid | null = null
let lastTypingConversationId: Guid | null = null
let lastTypingState = false
let lastTypingSentAt = 0
let typingIdleTimer: number | null = null
const typingClearTimers = new Map<Guid, number>()
const unsubscribeRealtimeHandlers: Array<() => void> = []

const selectedConversation = computed(() => {
  return conversations.value.find(
    (conversation) => conversation.id === selectedConversationId.value
  ) ?? null
})

const selectedTypingUser = computed(() => {
  const conversationId = selectedConversationId.value
  if (!conversationId) return null

  const typingState = typingUsersByConversationId.value[conversationId]
  if (!typingState?.isTyping) return null

  return (
    typingState.userName?.trim() ||
    selectedConversation.value?.otherUser.fullName ||
    selectedConversation.value?.otherUser.userName ||
    'Bạn ấy'
  )
})

const hasMoreMessages = computed(() => messages.value.length < totalMessages.value)

const canSendMessage = computed(() => {
  return Boolean(
    selectedConversationId.value &&
    (messageDraft.value.trim() || selectedImageFile.value)
  )
})

watch(
  () => props.open,
  async (open) => {
    document.body.classList.toggle('messenger-lock', open)

    if (!open) {
      await leaveJoinedConversation()
      return
    }

    bindRealtime()
    await realtimeClient.start()
    await loadCurrentUser()
    await loadJoinedWorkspaceIds()
    await loadConversations()

    if (props.startUserId) {
      await startDirectConversation(props.startUserId)
      emit('started')
    }

    await nextTick()
    messageInputRef.value?.focus()
  }
)

watch(
  () => props.startUserId,
  async (userId) => {
    if (!props.open || !userId) return

    await startDirectConversation(userId)
    emit('started')
  }
)

watch(messageDraft, (value) => {
  const conversationId = selectedConversationId.value
  if (!props.open || !conversationId) return

  if (value.trim()) {
    publishTyping(conversationId, true)
    scheduleTypingIdleStop(conversationId)
  } else {
    clearTypingIdleTimer()
    void publishTyping(conversationId, false)
  }
})

watch(selectedConversationId, async (conversationId, previousConversationId) => {
  if (previousConversationId) {
    clearTypingState(previousConversationId)
    await publishTyping(previousConversationId, false)
    await realtimeClient.leaveConversation(previousConversationId).catch(() => undefined)
  }

  messages.value = []
  totalMessages.value = 0
  messagePageNumber.value = 1
  messageDraft.value = ''
  selectedImageFile.value = null
  joinedConversationId = null

  if (!conversationId) return

  await joinConversation(conversationId)
  await loadMessages(conversationId)
  await markConversationRead(conversationId)
  await nextTick()
  scrollMessagesToBottom()
  messageInputRef.value?.focus()
})

function close() {
  const conversationId = selectedConversationId.value
  if (conversationId) {
    void publishTyping(conversationId, false)
  }

  clearAllTypingState()
  document.body.classList.remove('messenger-lock')
  emit('close')
}

function avatarUrl(value: string | null | undefined) {
  return normalizeImageUrl(value)
}

function initials(value: string) {
  const parts = value.trim().split(/\s+/).filter(Boolean).slice(0, 2)
  return parts.map((part) => part[0]?.toUpperCase()).join('') || '?'
}

function normalizeMessageImage(value: string) {
  return normalizeImageUrl(value) ?? value
}

function isWorkspaceShareMessage(message: MessageResponse) {
  return message.type?.toString().toLowerCase() === 'workspaceshare' && Boolean(workspaceSharePayload(message))
}

function workspaceSharePayload(message: MessageResponse): WorkspaceShareMessagePayload | null {
  if (!message.body) return null

  try {
    const parsed = JSON.parse(message.body) as Partial<WorkspaceShareMessagePayload>

    if (!parsed.workspaceId || !parsed.workspaceName) return null

    return {
      workspaceId: String(parsed.workspaceId),
      workspaceName: String(parsed.workspaceName),
      workspaceDescription: parsed.workspaceDescription ?? null,
      workspaceVisibility: String(parsed.workspaceVisibility ?? 'Private'),
      grantedRole: String(parsed.grantedRole ?? 'Viewer'),
      sharedByUserId: String(parsed.sharedByUserId ?? ''),
      sharedByDisplayName: String(parsed.sharedByDisplayName ?? 'Một thành viên'),
      sharedAtUtc: String(parsed.sharedAtUtc ?? message.createdDate),
    }
  } catch {
    return null
  }
}

function workspaceShareInitial(message: MessageResponse) {
  return workspaceSharePayload(message)?.workspaceName.trim().charAt(0).toUpperCase() || 'W'
}

function workspaceShareVisibilityLabel(value: string | null | undefined) {
  const normalized = value?.trim().toLowerCase()
  return normalized === 'public' ? 'Public' : 'Private'
}

function workspaceShareRoleLabel(value: string | null | undefined) {
  const normalized = value?.trim().toLowerCase()
  return normalized === 'member' ? 'Join as Member' : 'View as Viewer'
}

function isWorkspaceShareAccepted(message: MessageResponse) {
  const payload = workspaceSharePayload(message)
  return Boolean(payload?.workspaceId && joinedWorkspaceIds.value.has(payload.workspaceId))
}

function isWorkspaceShareActionDisabled(message: MessageResponse) {
  return acceptingWorkspaceShareMessageId.value === message.id || isWorkspaceShareAccepted(message)
}

function workspaceShareActionIcon(message: MessageResponse) {
  return isWorkspaceShareAccepted(message) ? 'check_circle' : 'login'
}

function workspaceShareActionLabel(message: MessageResponse) {
  if (acceptingWorkspaceShareMessageId.value === message.id) return 'Đang mở...'
  return isWorkspaceShareAccepted(message) ? 'Đã tham gia' : 'Mở workspace'
}

function workspaceShareActionTitle(message: MessageResponse) {
  return isWorkspaceShareAccepted(message)
    ? 'Bạn đã ở trong workspace này rồi'
    : 'Tham gia workspace từ lời mời Messenger'
}

async function acceptWorkspaceShare(message: MessageResponse) {
  if (message.isMine || acceptingWorkspaceShareMessageId.value || isWorkspaceShareAccepted(message)) return

  acceptingWorkspaceShareMessageId.value = message.id

  try {
    const result = await messagingController.acceptWorkspaceShare(message.id)

    if (!result.isSuccess || !result.data) {
      toast.warning(
        'Không mở được workspace',
        getApiResultErrorMessage(result, 'Workspace share không còn hợp lệ.')
      )
      return
    }

    joinedWorkspaceIds.value = new Set([
      ...joinedWorkspaceIds.value,
      result.data.id,
    ])

    toast.success(
      'Đã mở workspace',
      `Workspace “${result.data.name}” đã được thêm vào sidebar.`
    )

    emit('workspace-opened', result.data)
  } catch (error) {
    toast.warning(
      'Không mở được workspace',
      getApiErrorMessage(error, 'Workspace share không còn hợp lệ.')
    )
  } finally {
    acceptingWorkspaceShareMessageId.value = null
  }
}

function sortConversations(items: ConversationResponse[]) {
  return [...items].sort((left, right) => {
    const leftTime = new Date(
      left.lastMessageAtUtc ?? left.updatedDate ?? left.createdDate
    ).getTime()
    const rightTime = new Date(
      right.lastMessageAtUtc ?? right.updatedDate ?? right.createdDate
    ).getTime()

    return rightTime - leftTime
  })
}

function upsertConversation(conversation: ConversationResponse) {
  const existingIndex = conversations.value.findIndex(
    (item) => item.id === conversation.id
  )

  if (existingIndex >= 0) {
    const current = conversations.value[existingIndex]

    conversations.value.splice(existingIndex, 1, {
      ...(current ?? conversation),
      ...conversation,
    })
  } else {
    conversations.value.unshift(conversation)
  }

  conversations.value = sortConversations(conversations.value)
}

function upsertMessage(message: MessageResponse) {
  if (message.conversationId !== selectedConversationId.value) return

  const existingIndex = messages.value.findIndex((item) => item.id === message.id)

  if (existingIndex >= 0) {
    const current = messages.value[existingIndex]

    messages.value.splice(existingIndex, 1, {
      ...(current ?? message),
      ...message,
    })
    return
  }

  messages.value = [...messages.value, message].sort(
    (left, right) =>
      new Date(left.createdDate).getTime() - new Date(right.createdDate).getTime()
  )
}

async function loadCurrentUser() {
  if (currentUserId.value) return

  try {
    const result = await meController.getMyProfile()

    if (result.isSuccess && result.data) {
      currentUserId.value = result.data.id
    }
  } catch {
    // Chat still works without this, but read/mine rendering is more accurate when it is available.
  }
}

async function loadJoinedWorkspaceIds() {
  try {
    const result = await meController.listMyWorkspaces({
      pageNumber: 1,
      pageSize: 200,
    })

    if (result.isSuccess && result.data) {
      joinedWorkspaceIds.value = new Set(
        result.data.items.map((workspace) => workspace.id)
      )
    }
  } catch {
    // Workspace share cards still call backend for final authorization.
    // This cache is only to disable duplicate joins in the UI.
  }
}

async function loadConversations() {
  isLoadingConversations.value = true
  conversationError.value = null

  try {
    const result = await messagingController.listConversations({
      pageNumber: 1,
      pageSize: 40,
    })

    if (!result.isSuccess || !result.data) {
      conversationError.value = getApiResultErrorMessage(
        result,
        'Không thể tải conversations.'
      )
      conversations.value = []
      return
    }

    conversations.value = sortConversations(result.data.items)
  } catch (error) {
    conversationError.value = getApiErrorMessage(
      error,
      'Không thể tải conversations.'
    )
    conversations.value = []
  } finally {
    isLoadingConversations.value = false
  }
}

async function loadMessages(conversationId: Guid) {
  isLoadingMessages.value = true
  messageError.value = null

  try {
    const result = await messagingController.listMessages(conversationId, {
      pageNumber: 1,
      pageSize: 30,
    })

    if (!result.isSuccess || !result.data) {
      messageError.value = getApiResultErrorMessage(
        result,
        'Không thể tải tin nhắn.'
      )
      messages.value = []
      totalMessages.value = 0
      return
    }

    messagePageNumber.value = 1
    totalMessages.value = result.data.totalCount
    messages.value = [...result.data.items].sort(
      (left, right) =>
        new Date(left.createdDate).getTime() - new Date(right.createdDate).getTime()
    )
  } catch (error) {
    messageError.value = getApiErrorMessage(error, 'Không thể tải tin nhắn.')
    messages.value = []
    totalMessages.value = 0
  } finally {
    isLoadingMessages.value = false
  }
}

async function loadMoreMessages() {
  const conversationId = selectedConversationId.value
  if (!conversationId || isLoadingMoreMessages.value || !hasMoreMessages.value) return

  isLoadingMoreMessages.value = true

  try {
    const nextPage = messagePageNumber.value + 1
    const result = await messagingController.listMessages(conversationId, {
      pageNumber: nextPage,
      pageSize: 30,
    })

    if (!result.isSuccess || !result.data) {
      messageError.value = getApiResultErrorMessage(
        result,
        'Không thể tải thêm tin nhắn.'
      )
      return
    }

    messagePageNumber.value = nextPage
    totalMessages.value = result.data.totalCount
    const merged = [...result.data.items, ...messages.value]
    const map = new Map<Guid, MessageResponse>()

    for (const item of merged) {
      map.set(item.id, item)
    }

    messages.value = Array.from(map.values()).sort(
      (left, right) =>
        new Date(left.createdDate).getTime() - new Date(right.createdDate).getTime()
    )
  } catch (error) {
    messageError.value = getApiErrorMessage(
      error,
      'Không thể tải thêm tin nhắn.'
    )
  } finally {
    isLoadingMoreMessages.value = false
  }
}

async function startDirectConversation(userId: Guid) {
  conversationError.value = null

  try {
    const result = await messagingController.createOrGetDirectConversation({
      recipientUserId: userId,
    })

    if (!result.isSuccess || !result.data) {
      conversationError.value = getApiResultErrorMessage(
        result,
        'Không thể mở cuộc trò chuyện. Hai người cần là bạn bè trước.'
      )
      toast.warning('Không thể mở chat', conversationError.value ?? undefined)
      return
    }

    upsertConversation(result.data)
    selectedConversationId.value = result.data.id
  } catch (error) {
    conversationError.value = getApiErrorMessage(
      error,
      'Không thể mở cuộc trò chuyện. Hai người cần là bạn bè trước.'
    )
    toast.warning('Không thể mở chat', conversationError.value ?? undefined)
  }
}

function selectConversation(conversation: ConversationResponse) {
  selectedConversationId.value = conversation.id
}

async function joinConversation(conversationId: Guid) {
  try {
    await realtimeClient.joinConversation(conversationId)
    joinedConversationId = conversationId
  } catch (error) {
    console.warn('[Messenger] join conversation failed', error)
  }
}

async function leaveJoinedConversation() {
  if (!joinedConversationId) return

  const conversationId = joinedConversationId
  joinedConversationId = null

  clearTypingState(conversationId)
  await publishTyping(conversationId, false)
  await realtimeClient.leaveConversation(conversationId).catch(() => undefined)
}

async function markConversationRead(conversationId: Guid) {
  try {
    await messagingController.markRead(conversationId)

    const conversation = conversations.value.find((item) => item.id === conversationId)
    if (conversation) {
      conversation.unreadCount = 0
    }
  } catch {
    // Silent: read receipt can retry on next open.
  }
}

async function sendCurrentMessage() {
  const conversationId = selectedConversationId.value
  if (!conversationId || !canSendMessage.value || isSendingMessage.value) return

  isSendingMessage.value = true
  messageError.value = null
  clearTypingIdleTimer()
  await publishTyping(conversationId, false)

  try {
    let result

    if (selectedImageFile.value) {
      result = await messagingController.sendImageMessage(
        conversationId,
        selectedImageFile.value,
        messageDraft.value.trim() || null
      )
    } else {
      result = await messagingController.sendTextMessage(conversationId, {
        body: messageDraft.value.trim(),
      })
    }

    if (!result.isSuccess || !result.data) {
      messageError.value = getApiResultErrorMessage(
        result,
        'Không gửi được tin nhắn.'
      )
      return
    }

    upsertMessage(result.data)
    messageDraft.value = ''
    selectedImageFile.value = null

    await loadConversations()
    await nextTick()
    scrollMessagesToBottom()
  } catch (error) {
    messageError.value = getApiErrorMessage(error, 'Không gửi được tin nhắn.')
  } finally {
    isSendingMessage.value = false
  }
}

function triggerImageInput() {
  imageInputRef.value?.click()
}

function handleImageSelected(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]

  input.value = ''

  if (!file) return

  selectedImageFile.value = file
}

function clearSelectedImage() {
  selectedImageFile.value = null
}

function clearTypingIdleTimer() {
  if (typingIdleTimer !== null) {
    window.clearTimeout(typingIdleTimer)
    typingIdleTimer = null
  }
}

function scheduleTypingIdleStop(conversationId: Guid) {
  clearTypingIdleTimer()
  typingIdleTimer = window.setTimeout(() => {
    void publishTyping(conversationId, false)
  }, 1600)
}

async function publishTyping(conversationId: Guid, isTyping: boolean) {
  const now = Date.now()

  if (
    lastTypingConversationId === conversationId &&
    lastTypingState === isTyping &&
    (isTyping ? now - lastTypingSentAt < 1200 : now - lastTypingSentAt < 250)
  ) {
    return
  }

  lastTypingConversationId = conversationId
  lastTypingState = isTyping
  lastTypingSentAt = now

  try {
    await realtimeClient.sendConversationTyping(conversationId, isTyping)
  } catch {
    // Typing indicator is a nice-to-have realtime hint; failing silently keeps chat usable.
  }
}

function setTypingState(conversationId: Guid, userName: string | null, isTyping: boolean) {
  if (typingClearTimers.has(conversationId)) {
    window.clearTimeout(typingClearTimers.get(conversationId))
    typingClearTimers.delete(conversationId)
  }

  typingUsersByConversationId.value = {
    ...typingUsersByConversationId.value,
    [conversationId]: { userName, isTyping },
  }

  if (isTyping) {
    const timerId = window.setTimeout(() => {
      clearTypingState(conversationId)
    }, 2600)

    typingClearTimers.set(conversationId, timerId)
  }
}

function clearTypingState(conversationId: Guid) {
  if (typingClearTimers.has(conversationId)) {
    window.clearTimeout(typingClearTimers.get(conversationId))
    typingClearTimers.delete(conversationId)
  }

  if (!typingUsersByConversationId.value[conversationId]) return

  const next = { ...typingUsersByConversationId.value }
  delete next[conversationId]
  typingUsersByConversationId.value = next
}

function clearAllTypingState() {
  clearTypingIdleTimer()

  for (const timerId of typingClearTimers.values()) {
    window.clearTimeout(timerId)
  }

  typingClearTimers.clear()
  typingUsersByConversationId.value = {}
  lastTypingConversationId = null
  lastTypingState = false
  lastTypingSentAt = 0
}

function normalizeTypingPayload(envelope: RealtimeEnvelope<unknown>) {
  const raw = normalizePayloadObject(envelope)
  if (!raw) return null

  const conversationId = String(raw.conversationId ?? raw.ConversationId ?? '')
  const senderUserId = String(raw.senderUserId ?? raw.SenderUserId ?? '')

  if (!conversationId || !senderUserId) return null

  return {
    conversationId,
    senderUserId,
    senderDisplayName: (
      raw.senderDisplayName ??
      raw.SenderDisplayName ??
      raw.userName ??
      raw.UserName ??
      raw.senderUserName ??
      raw.SenderUserName ??
      null
    ) as string | null,
    isTyping: Boolean(raw.isTyping ?? raw.IsTyping ?? false),
  }
}

function normalizePayloadObject(envelope: RealtimeEnvelope<unknown>) {
  const raw = envelope.payload

  if (!raw || typeof raw !== 'object') return null

  return raw as Record<string, unknown>
}

function normalizeMessagePayload(envelope: RealtimeEnvelope<unknown>): MessageResponse | null {
  const raw = normalizePayloadObject(envelope)
  if (!raw) return null

  const id = String(raw.id ?? raw.Id ?? '')
  const conversationId = String(raw.conversationId ?? raw.ConversationId ?? '')
  const senderUserId = String(raw.senderUserId ?? raw.SenderUserId ?? '')
  const recipientUserId = String(raw.recipientUserId ?? raw.RecipientUserId ?? '')

  if (!id || !conversationId || !senderUserId || !recipientUserId) return null

  return {
    id,
    conversationId,
    senderUserId,
    recipientUserId,
    type: String(raw.type ?? raw.Type ?? 'Text'),
    body: (raw.body ?? raw.Body ?? null) as string | null,
    imageUrl: (raw.imageUrl ?? raw.ImageUrl ?? null) as string | null,
    attachmentFileId: (raw.attachmentFileId ?? raw.AttachmentFileId ?? null) as Guid | null,
    isMine: currentUserId.value ? senderUserId === currentUserId.value : Boolean(raw.isMine ?? raw.IsMine ?? false),
    readAtUtc: (raw.readAtUtc ?? raw.ReadAtUtc ?? null) as string | null,
    createdDate:
      String(raw.createdDate ?? raw.CreatedDate ?? envelope.occurredAtUtc ?? new Date().toISOString()),
    updatedDate: (raw.updatedDate ?? raw.UpdatedDate ?? null) as string | null,
  }
}

function normalizeConversationPayload(envelope: RealtimeEnvelope<unknown>): ConversationResponse | null {
  const raw = normalizePayloadObject(envelope)
  if (!raw) return null

  const id = String(raw.id ?? raw.Id ?? '')
  const otherUserRaw = (raw.otherUser ?? raw.OtherUser) as Record<string, unknown> | null

  if (!id || !otherUserRaw) return null

  const otherUser = {
    id: String(otherUserRaw.id ?? otherUserRaw.Id ?? ''),
    userName: String(otherUserRaw.userName ?? otherUserRaw.UserName ?? ''),
    fullName: String(otherUserRaw.fullName ?? otherUserRaw.FullName ?? ''),
    avatarUrl: (otherUserRaw.avatarUrl ?? otherUserRaw.AvatarUrl ?? null) as string | null,
  }

  if (!otherUser.id) return null

  // Backend realtime payloads are built from the sender perspective.
  // If that makes the "other user" equal to me, reload the canonical list instead.
  if (currentUserId.value && otherUser.id === currentUserId.value) {
    return null
  }

  return {
    id,
    otherUser,
    lastMessagePreview: (raw.lastMessagePreview ?? raw.LastMessagePreview ?? null) as string | null,
    lastMessageAtUtc: (raw.lastMessageAtUtc ?? raw.LastMessageAtUtc ?? null) as string | null,
    unreadCount: Number(raw.unreadCount ?? raw.UnreadCount ?? 0),
    createdDate:
      String(raw.createdDate ?? raw.CreatedDate ?? envelope.occurredAtUtc ?? new Date().toISOString()),
    updatedDate: (raw.updatedDate ?? raw.UpdatedDate ?? null) as string | null,
  }
}

function bindRealtime() {
  if (unsubscribeRealtimeHandlers.length > 0) return

  unsubscribeRealtimeHandlers.push(
    realtimeClient.on('ConversationUpserted', async (envelope) => {
      const conversation = normalizeConversationPayload(envelope)

      if (conversation) {
        upsertConversation(conversation)
      } else {
        await loadConversations()
      }
    })
  )

  unsubscribeRealtimeHandlers.push(
    realtimeClient.on('MessageCreated', async (envelope) => {
      const message = normalizeMessagePayload(envelope)

      if (!message) {
        await loadConversations()
        if (selectedConversationId.value) {
          await loadMessages(selectedConversationId.value)
        }
        return
      }

      if (message.conversationId === selectedConversationId.value) {
        upsertMessage(message)
        await nextTick()
        scrollMessagesToBottom()
        await markConversationRead(message.conversationId)
      } else {
        const conversation = conversations.value.find(
          (item) => item.id === message.conversationId
        )

        if (conversation && !message.isMine) {
          conversation.unreadCount += 1
        }
      }

      await loadConversations()
    })
  )

  unsubscribeRealtimeHandlers.push(
    realtimeClient.on('ConversationRead', async () => {
      await loadConversations()

      if (selectedConversationId.value) {
        await loadMessages(selectedConversationId.value)
      }
    })
  )

  unsubscribeRealtimeHandlers.push(
    realtimeClient.on('ConversationTyping', (envelope) => {
      const typing = normalizeTypingPayload(envelope)
      if (!typing || typing.senderUserId === currentUserId.value) return

      setTypingState(
        typing.conversationId,
        typing.senderDisplayName,
        typing.isTyping
      )
    })
  )
}

function unbindRealtime() {
  while (unsubscribeRealtimeHandlers.length) {
    unsubscribeRealtimeHandlers.pop()?.()
  }
}

function scrollMessagesToBottom() {
  const element = messageListRef.value
  if (!element) return

  element.scrollTop = element.scrollHeight
}

function formatConversationTime(value: string | null) {
  if (!value) return ''

  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''

  const diffMs = Date.now() - date.getTime()
  const minutes = Math.max(0, Math.floor(diffMs / 60000))

  if (minutes < 1) return 'now'
  if (minutes < 60) return `${minutes}m`

  const hours = Math.floor(minutes / 60)
  if (hours < 24) return `${hours}h`

  return date.toLocaleDateString()
}

function formatMessageTime(value: string) {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''

  return date.toLocaleTimeString([], {
    hour: '2-digit',
    minute: '2-digit',
  })
}

onBeforeUnmount(() => {
  clearAllTypingState()
  unbindRealtime()
  void leaveJoinedConversation()
  document.body.classList.remove('messenger-lock')
})
</script>

<style scoped src="./css/MessengerPanel.css"></style>
