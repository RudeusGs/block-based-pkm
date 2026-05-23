<template>
  <Teleport to="body">
    <Transition name="workspace-share-modal">
      <section
        v-if="open"
        class="workspace-share-layer"
        role="dialog"
        aria-modal="true"
        aria-labelledby="workspace-share-title"
      >
        <button
          class="workspace-share-scrim"
          type="button"
          aria-label="Đóng share workspace"
          @click="close"
        ></button>

        <div class="workspace-share-shell" @click.stop>
          <header class="workspace-share-head">
            <div class="workspace-share-kicker">
              <span class="material-symbols-outlined">ios_share</span>
              Workspace share
            </div>

            <button
              type="button"
              class="workspace-share-close"
              title="Đóng"
              @click="close"
            >
              <span class="material-symbols-outlined">close</span>
            </button>
          </header>

          <section class="workspace-share-hero">
            <span class="workspace-share-orb">
              {{ workspaceInitial }}
            </span>

            <div>
              <h2 id="workspace-share-title">
                Share “{{ workspaceName || 'Workspace' }}”
              </h2>
              <p>
                Gửi workspace card vào Messenger. Người nhận bấm card là có thể vào workspace dù private hay public.
              </p>
            </div>
          </section>

          <div v-if="!canShare" class="workspace-share-warning">
            <span class="material-symbols-outlined">lock</span>
            Chỉ Owner hoặc Manager mới được share workspace. Member và Viewer nhìn cho vui thôi, không bấm share được nha.
          </div>

          <template v-else>
            <div class="workspace-share-access-row">
              <div>
                <strong>Quyền khi nhận</strong>
                <span>Viewer chỉ xem, Member có thể tạo page/task theo quyền hiện tại.</span>
              </div>

              <div class="workspace-share-role-toggle" role="group" aria-label="Share role">
                <button
                  type="button"
                  :class="{ active: shareRole === 'viewer' }"
                  @click="shareRole = 'viewer'"
                >
                  Viewer
                </button>
                <button
                  type="button"
                  :class="{ active: shareRole === 'member' }"
                  @click="shareRole = 'member'"
                >
                  Member
                </button>
              </div>
            </div>

            <div class="workspace-share-search">
              <span class="material-symbols-outlined">search</span>
              <input
                v-model="keyword"
                type="search"
                placeholder="Tìm người đã nhắn tin hoặc bạn bè..."
              />
            </div>

            <div class="workspace-share-tabs" role="tablist">
              <button
                type="button"
                :class="{ active: activeTab === 'conversations' }"
                @click="activeTab = 'conversations'"
              >
                Đã nhắn tin
              </button>
              <button
                type="button"
                :class="{ active: activeTab === 'friends' }"
                @click="activeTab = 'friends'"
              >
                Bạn bè
              </button>
            </div>

            <div v-if="isLoading" class="workspace-share-list loading">
              <span v-for="index in 6" :key="index"></span>
            </div>

            <div v-else-if="error" class="workspace-share-empty error">
              <span class="material-symbols-outlined">error</span>
              <strong>Không tải được danh sách</strong>
              <p>{{ error }}</p>
              <button type="button" @click="loadData">Thử lại</button>
            </div>

            <div v-else-if="!visibleTargets.length" class="workspace-share-empty">
              <span class="material-symbols-outlined">forum</span>
              <strong>Chưa có ai để share</strong>
              <p>Hãy kết bạn hoặc mở conversation trước rồi quay lại share workspace.</p>
            </div>

            <div v-else class="workspace-share-list">
              <button
                v-for="target in visibleTargets"
                :key="target.key"
                type="button"
                class="workspace-share-person"
                :disabled="sharingTargetKey === target.key"
                @click="shareToTarget(target)"
              >
                <img
                  v-if="avatarUrl(target.avatarUrl)"
                  :src="avatarUrl(target.avatarUrl) || ''"
                  :alt="target.fullName"
                  referrerpolicy="no-referrer"
                />

                <span v-else class="workspace-share-avatar-fallback">
                  {{ initials(target.fullName || target.userName) }}
                </span>

                <span class="workspace-share-person-copy">
                  <strong>{{ target.fullName || target.userName }}</strong>
                  <small>
                    {{ target.kind === 'conversation' ? 'Conversation' : 'Friend' }} · @{{ target.userName }}
                  </small>
                </span>

                <span class="workspace-share-send-chip">
                  <span
                    v-if="sharingTargetKey === target.key"
                    class="workspace-share-spinner"
                  ></span>
                  <span v-else class="material-symbols-outlined">send</span>
                </span>
              </button>
            </div>
          </template>
        </div>
      </section>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { messagingController } from '@/api/services/messaging.api'
import { socialController } from '@/api/services/social.api'
import {
  getApiErrorMessage,
  getApiResultErrorMessage,
} from '@/api/utils/api-error.util'
import type { Guid } from '@/api/models/common.model'
import type { ConversationResponse } from '@/api/models/messaging.model'
import type { FriendResponse } from '@/api/models/social.model'
import type { WorkspaceShareRoleRequest } from '@/api/models/messaging.model'
import { normalizeImageUrl } from '@/utils/image-url.util'
import { useToast } from '@/components/composables/useToast'

interface ShareTarget {
  key: string
  kind: 'conversation' | 'friend'
  userId: Guid
  userName: string
  fullName: string
  avatarUrl: string | null
  conversationId?: Guid | null
}

const props = defineProps<{
  open: boolean
  workspaceId: Guid | null
  workspaceName: string
  canShare: boolean
}>()

const emit = defineEmits<{
  close: []
  shared: [conversationId: Guid]
}>()

const toast = useToast()

const activeTab = ref<'conversations' | 'friends'>('conversations')
const keyword = ref('')
const shareRole = ref<WorkspaceShareRoleRequest>('viewer')
const conversations = ref<ConversationResponse[]>([])
const friends = ref<FriendResponse[]>([])
const isLoading = ref(false)
const error = ref<string | null>(null)
const sharingTargetKey = ref<string | null>(null)

const workspaceInitial = computed(() => {
  return props.workspaceName.trim().charAt(0).toUpperCase() || 'W'
})

const conversationTargets = computed<ShareTarget[]>(() => {
  return conversations.value.map((conversation) => ({
    key: `conversation:${conversation.id}`,
    kind: 'conversation',
    userId: conversation.otherUser.id,
    userName: conversation.otherUser.userName,
    fullName: conversation.otherUser.fullName,
    avatarUrl: conversation.otherUser.avatarUrl,
    conversationId: conversation.id,
  }))
})

const friendTargets = computed<ShareTarget[]>(() => {
  const existingConversationUserIds = new Set(
    conversationTargets.value.map((target) => target.userId)
  )

  return friends.value
    .map<ShareTarget>((friend) => {
      const existingConversation = conversationTargets.value.find(
        (target) => target.userId === friend.userId
      )

      return {
        key: `friend:${friend.userId}`,
        kind: 'friend',
        userId: friend.userId,
        userName: friend.userName,
        fullName: friend.fullName,
        avatarUrl: friend.avatarUrl,
        conversationId: existingConversation?.conversationId ?? null,
      }
    })
    .filter(
      (target) =>
        activeTab.value === 'friends' ||
        !existingConversationUserIds.has(target.userId)
    )
})

const visibleTargets = computed(() => {
  const source = activeTab.value === 'conversations'
    ? conversationTargets.value
    : friendTargets.value

  const query = keyword.value.trim().toLowerCase()

  if (!query) return source

  return source.filter((target) => {
    return [target.fullName, target.userName]
      .filter(Boolean)
      .some((value) => value.toLowerCase().includes(query))
  })
})

watch(
  () => props.open,
  (open) => {
    if (!open) return

    keyword.value = ''
    activeTab.value = 'conversations'
    void loadData()
  }
)

function close() {
  if (sharingTargetKey.value) return
  emit('close')
}

function avatarUrl(value: string | null | undefined) {
  return normalizeImageUrl(value)
}

function initials(value: string) {
  const parts = value.trim().split(/\s+/).filter(Boolean).slice(0, 2)
  return parts.map((part) => part[0]?.toUpperCase()).join('') || '?'
}

async function loadData() {
  if (!props.canShare) return

  isLoading.value = true
  error.value = null

  try {
    const [conversationResult, friendResult] = await Promise.all([
      messagingController.listConversations({ pageNumber: 1, pageSize: 50 }),
      socialController.listFriends(1, 100),
    ])

    if (!conversationResult.isSuccess || !conversationResult.data) {
      error.value = getApiResultErrorMessage(
        conversationResult,
        'Không thể tải danh sách conversation.'
      )
      conversations.value = []
      friends.value = []
      return
    }

    if (!friendResult.isSuccess || !friendResult.data) {
      error.value = getApiResultErrorMessage(
        friendResult,
        'Không thể tải danh sách bạn bè.'
      )
      conversations.value = []
      friends.value = []
      return
    }

    conversations.value = conversationResult.data.items
    friends.value = friendResult.data
  } catch (loadError) {
    error.value = getApiErrorMessage(loadError, 'Không thể tải danh sách share.')
    conversations.value = []
    friends.value = []
  } finally {
    isLoading.value = false
  }
}

async function resolveConversationId(target: ShareTarget) {
  if (target.conversationId) return target.conversationId

  const result = await messagingController.createOrGetDirectConversation({
    recipientUserId: target.userId,
  })

  if (!result.isSuccess || !result.data) {
    throw new Error(
      getApiResultErrorMessage(result, 'Không thể mở conversation với người này.')
    )
  }

  conversations.value = [result.data, ...conversations.value]
  return result.data.id
}

async function shareToTarget(target: ShareTarget) {
  if (!props.workspaceId || sharingTargetKey.value) return

  sharingTargetKey.value = target.key

  try {
    const conversationId = await resolveConversationId(target)
    const result = await messagingController.sendWorkspaceShareMessage(
      conversationId,
      {
        workspaceId: props.workspaceId,
        role: shareRole.value,
      }
    )

    if (!result.isSuccess || !result.data) {
      toast.warning(
        'Share thất bại',
        getApiResultErrorMessage(result, 'Không gửi được workspace card.')
      )
      return
    }

    toast.success(
      'Đã gửi workspace',
      `Workspace “${props.workspaceName}” đã được gửi cho ${target.fullName || target.userName}.`
    )

    emit('shared', conversationId)
    emit('close')
  } catch (shareError) {
    toast.warning(
      'Share thất bại',
      getApiErrorMessage(shareError, 'Không gửi được workspace card.')
    )
  } finally {
    sharingTargetKey.value = null
  }
}
</script>

<style scoped>
.workspace-share-layer,
.workspace-share-scrim {
  position: fixed;
  inset: 0;
}

.workspace-share-layer {
  z-index: 2130;
  display: grid;
  place-items: center;
  padding: 18px;
}

.workspace-share-scrim {
  border: 0;
  background: rgba(0, 0, 0, 0.54);
}

.workspace-share-shell {
  position: relative;
  width: min(540px, calc(100vw - 36px));
  max-height: min(720px, calc(100vh - 36px));
  overflow: hidden;
  border: 1px solid #2f2f2f;
  border-radius: 14px;
  color: #ededed;
  background: #191919;
  box-shadow: 0 26px 100px rgba(0, 0, 0, 0.52);
}

.workspace-share-head {
  min-height: 52px;
  border-bottom: 1px solid #2b2b2b;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 0 12px 0 16px;
}

.workspace-share-kicker {
  display: inline-flex;
  align-items: center;
  gap: 7px;
  color: #a3a3a3;
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.workspace-share-kicker .material-symbols-outlined {
  font-size: 17px;
}

.workspace-share-close {
  width: 32px;
  height: 32px;
  border: 0;
  border-radius: 7px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: #a3a3a3;
  background: transparent;
}

.workspace-share-close:hover {
  color: #ededed;
  background: #242424;
}

.workspace-share-hero {
  display: flex;
  gap: 13px;
  padding: 16px;
  border-bottom: 1px solid #242424;
}

.workspace-share-orb {
  width: 44px;
  height: 44px;
  border-radius: 10px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  color: #ededed;
  background: #2a2a2a;
  font-size: 18px;
  font-weight: 800;
}

.workspace-share-hero h2 {
  margin: 0;
  color: #ededed;
  font-size: 18px;
  font-weight: 760;
  line-height: 1.25;
}

.workspace-share-hero p {
  margin: 5px 0 0;
  color: #8a8a8a;
  font-size: 12.5px;
  line-height: 1.45;
}

.workspace-share-warning {
  margin: 14px;
  border: 1px solid #3a2f1d;
  border-radius: 10px;
  padding: 12px;
  display: flex;
  gap: 9px;
  color: #d6b15d;
  background: #1f1b13;
  font-size: 13px;
  line-height: 1.45;
}

.workspace-share-access-row {
  margin: 14px;
  border: 1px solid #303030;
  border-radius: 11px;
  padding: 11px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  background: #202020;
}

.workspace-share-access-row strong {
  display: block;
  color: #ededed;
  font-size: 13px;
  font-weight: 700;
}

.workspace-share-access-row span {
  display: block;
  margin-top: 2px;
  color: #858585;
  font-size: 11.5px;
  line-height: 1.4;
}

.workspace-share-role-toggle {
  flex-shrink: 0;
  border: 1px solid #303030;
  border-radius: 9px;
  padding: 3px;
  display: inline-flex;
  gap: 2px;
  background: #191919;
}

.workspace-share-role-toggle button {
  border: 0;
  border-radius: 7px;
  padding: 6px 9px;
  color: #858585;
  background: transparent;
  font-size: 12px;
  font-weight: 650;
}

.workspace-share-role-toggle button.active {
  color: #191919;
  background: #ededed;
}

.workspace-share-search {
  margin: 0 14px 10px;
  min-height: 38px;
  border: 1px solid #303030;
  border-radius: 10px;
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 0 11px;
  color: #858585;
  background: #202020;
}

.workspace-share-search .material-symbols-outlined {
  font-size: 18px;
}

.workspace-share-search input {
  min-width: 0;
  flex: 1;
  border: 0;
  outline: 0;
  color: #ededed;
  background: transparent;
  font-size: 13px;
}

.workspace-share-search input::placeholder {
  color: #666;
}

.workspace-share-tabs {
  margin: 0 14px 10px;
  display: flex;
  gap: 6px;
}

.workspace-share-tabs button {
  border: 0;
  border-radius: 8px;
  padding: 7px 10px;
  color: #858585;
  background: transparent;
  font-size: 12px;
  font-weight: 650;
}

.workspace-share-tabs button:hover,
.workspace-share-tabs button.active {
  color: #ededed;
  background: #242424;
}

.workspace-share-list {
  max-height: min(330px, calc(100vh - 360px));
  overflow-y: auto;
  padding: 0 8px 10px;
}

.workspace-share-person {
  width: 100%;
  min-height: 58px;
  border: 0;
  border-radius: 10px;
  display: grid;
  grid-template-columns: 38px minmax(0, 1fr) auto;
  align-items: center;
  gap: 10px;
  padding: 8px;
  color: inherit;
  background: transparent;
  text-align: left;
}

.workspace-share-person:hover:not(:disabled) {
  background: #242424;
}

.workspace-share-person:disabled {
  opacity: 0.64;
  cursor: wait;
}

.workspace-share-person img,
.workspace-share-avatar-fallback {
  width: 38px;
  height: 38px;
  border-radius: 10px;
  object-fit: cover;
}

.workspace-share-avatar-fallback {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: #ededed;
  background: #2a2a2a;
  font-size: 13px;
  font-weight: 750;
}

.workspace-share-person-copy {
  min-width: 0;
}

.workspace-share-person-copy strong,
.workspace-share-person-copy small {
  display: block;
  overflow: hidden;
  white-space: nowrap;
  text-overflow: ellipsis;
}

.workspace-share-person-copy strong {
  color: #ededed;
  font-size: 13px;
  font-weight: 720;
}

.workspace-share-person-copy small {
  margin-top: 3px;
  color: #858585;
  font-size: 12px;
}

.workspace-share-send-chip {
  width: 30px;
  height: 30px;
  border-radius: 8px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: #a3a3a3;
  background: #202020;
}

.workspace-share-send-chip .material-symbols-outlined {
  font-size: 17px;
}

.workspace-share-spinner {
  width: 14px;
  height: 14px;
  border: 2px solid #4a4a4a;
  border-top-color: #ededed;
  border-radius: 999px;
  animation: workspace-share-spin 0.75s linear infinite;
}

.workspace-share-list.loading {
  display: grid;
  gap: 8px;
  padding: 0 14px 14px;
}

.workspace-share-list.loading span {
  height: 54px;
  border-radius: 10px;
  background: linear-gradient(90deg, #202020, #292929, #202020);
  background-size: 200% 100%;
  animation: workspace-share-shimmer 1.1s ease-in-out infinite;
}

.workspace-share-empty {
  margin: 14px;
  border: 1px solid #303030;
  border-radius: 12px;
  padding: 24px 16px;
  display: grid;
  place-items: center;
  text-align: center;
  background: #202020;
}

.workspace-share-empty .material-symbols-outlined {
  color: #858585;
  font-size: 32px;
}

.workspace-share-empty strong {
  margin-top: 8px;
  color: #ededed;
  font-size: 14px;
}

.workspace-share-empty p {
  margin: 5px 0 0;
  color: #858585;
  font-size: 12.5px;
  line-height: 1.45;
}

.workspace-share-empty button {
  margin-top: 11px;
  border: 0;
  border-radius: 7px;
  padding: 7px 10px;
  color: #191919;
  background: #ededed;
  font-size: 12px;
  font-weight: 700;
}

.workspace-share-modal-enter-active,
.workspace-share-modal-leave-active {
  transition: opacity 0.16s ease;
}

.workspace-share-modal-enter-from,
.workspace-share-modal-leave-to {
  opacity: 0;
}

@keyframes workspace-share-shimmer {
  to {
    background-position: -200% 0;
  }
}

@keyframes workspace-share-spin {
  to {
    transform: rotate(360deg);
  }
}

@media (max-width: 560px) {
  .workspace-share-shell {
    width: 100%;
  }

  .workspace-share-access-row {
    align-items: flex-start;
    flex-direction: column;
  }

  .workspace-share-role-toggle {
    width: 100%;
  }

  .workspace-share-role-toggle button {
    flex: 1;
  }
}
</style>
