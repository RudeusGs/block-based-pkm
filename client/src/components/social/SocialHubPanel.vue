<template>
  <Teleport to="body">
    <Transition name="social-hub">
      <section
        v-if="open"
        class="social-hub-layer"
        role="dialog"
        aria-modal="true"
        aria-label="Trung tâm xã hội"
      >
        <button
          class="social-hub-scrim"
          type="button"
          aria-label="Đóng trung tâm xã hội"
          @click="close"
        ></button>

        <aside
          class="social-hub-panel"
          @click.stop
          @keydown.esc="close"
        >
          <header class="social-hub-header">
            <div>
              <p>Kết nối trong không gian</p>
              <h2>Bạn bè và hồ sơ</h2>
            </div>

            <button
              type="button"
              title="Đóng"
              @click="close"
            >
              <span class="material-symbols-outlined">close</span>
            </button>
          </header>

          <nav class="social-tabs" aria-label="Các mục xã hội">
            <button
              v-for="tab in tabs"
              :key="tab.value"
              type="button"
              :class="{ active: activeTab === tab.value }"
              @click="selectTab(tab.value)"
            >
              <span class="material-symbols-outlined">{{ tab.icon }}</span>
              {{ tab.label }}

              <small v-if="tab.value === 'requests' && incomingRequests.length">
                {{ incomingRequests.length }}
              </small>
            </button>
          </nav>

          <main class="social-hub-body">
            <section
              v-if="activeTab === 'search'"
              class="social-section"
            >
              <div class="social-search-box">
                <span class="material-symbols-outlined">search</span>

                <input
                  ref="searchInputRef"
                  v-model="searchKeyword"
                  type="search"
                  placeholder="Tìm người theo tên hoặc tên đăng nhập..."
                  autocomplete="off"
                />

                <button
                  v-if="searchKeyword"
                  type="button"
                  title="Xóa tìm kiếm"
                  @click="searchKeyword = ''"
                >
                  <span class="material-symbols-outlined">close</span>
                </button>
              </div>

              <p class="social-muted">
                Nhập ít nhất 2 ký tự để tìm kiếm bạn bè.
              </p>

              <div
                v-if="searchError"
                class="social-error"
              >
                <span class="material-symbols-outlined">error</span>
                {{ searchError }}
              </div>

              <div
                v-if="isSearching"
                class="social-skeleton-list"
              >
                <span v-for="index in 5" :key="index"></span>
              </div>

              <div
                v-else-if="searchKeyword.trim().length >= 2 && !searchResults.length && !searchError"
                class="social-empty-state"
              >
                <span class="material-symbols-outlined">person_search</span>
                <strong>Không tìm thấy ai</strong>
                <p>Thử nhập tên hoặc tên đăng nhập khác.</p>
              </div>

              <div
                v-else
                class="social-user-list"
              >
                <article
                  v-for="user in searchResults"
                  :key="user.id"
                  class="social-user-card"
                >
                  <button
                    type="button"
                    class="social-user-main"
                    @click="openProfile(user.id)"
                  >
                    <img
                      v-if="avatarUrl(user.avatarUrl)"
                      :src="avatarUrl(user.avatarUrl) || ''"
                      :alt="user.fullName"
                      referrerpolicy="no-referrer"
                    />

                    <span v-else class="social-avatar-fallback">
                      {{ initials(user.fullName || user.userName) }}
                    </span>

                    <span>
                      <strong>{{ user.fullName }}</strong>
                      <small>@{{ user.userName }} · {{ friendshipLabel(user.friendshipStatus) }}</small>
                    </span>
                  </button>

                  <div class="social-user-actions">
                    <button
                      v-if="normalizeFriendship(user.friendshipStatus) === 'none'"
                      type="button"
                      :disabled="mutatingUserId === user.id"
                      @click="sendRequest(user.id)"
                    >
                      Thêm bạn
                    </button>

                    <button
                      v-else-if="normalizeFriendship(user.friendshipStatus) === 'friends'"
                      type="button"
                      @click="emit('open-chat', user.id)"
                    >
                      Nhắn tin
                    </button>

                    <button
                      v-else-if="normalizeFriendship(user.friendshipStatus) === 'request_received'"
                      type="button"
                      @click="activeTab = 'requests'"
                    >
                      Trả lời
                    </button>

                    <button
                      v-else
                      type="button"
                      disabled
                    >
                      {{ friendshipActionLabel(user.friendshipStatus) }}
                    </button>
                  </div>
                </article>
              </div>
            </section>

            <section
              v-else-if="activeTab === 'friends'"
              class="social-section"
            >
              <div class="social-section-head">
                <div>
                  <strong>{{ friends.length }} bạn bè</strong>
                  <span>Chỉ bạn bè mới tạo được cuộc trò chuyện riêng.</span>
                </div>

                <button
                  type="button"
                  :disabled="isLoadingFriends"
                  @click="loadFriends"
                >
                  <span class="material-symbols-outlined">refresh</span>
                </button>
              </div>

              <div
                v-if="friendsError"
                class="social-error"
              >
                <span class="material-symbols-outlined">error</span>
                {{ friendsError }}
              </div>

              <div
                v-if="isLoadingFriends"
                class="social-skeleton-list"
              >
                <span v-for="index in 5" :key="index"></span>
              </div>

              <div
                v-else-if="!friends.length"
                class="social-empty-state"
              >
                <span class="material-symbols-outlined">group_add</span>
                <strong>Chưa có bạn bè</strong>
                <p>Qua tab Tìm kiếm để gửi lời mời kết bạn.</p>
              </div>

              <div
                v-else
                class="social-user-list"
              >
                <article
                  v-for="friend in friends"
                  :key="friend.userId"
                  class="social-user-card"
                >
                  <button
                    type="button"
                    class="social-user-main"
                    @click="openProfile(friend.userId)"
                  >
                    <img
                      v-if="avatarUrl(friend.avatarUrl)"
                      :src="avatarUrl(friend.avatarUrl) || ''"
                      :alt="friend.fullName"
                      referrerpolicy="no-referrer"
                    />

                    <span v-else class="social-avatar-fallback">
                      {{ initials(friend.fullName || friend.userName) }}
                    </span>

                    <span>
                      <strong>{{ friend.fullName }}</strong>
                      <small>@{{ friend.userName }} · Bạn bè từ {{ formatShortDate(friend.friendsSinceUtc) }}</small>
                    </span>
                  </button>

                  <div class="social-user-actions">
                    <button
                      type="button"
                      @click="emit('open-chat', friend.userId)"
                    >
                      Nhắn tin
                    </button>

                    <button
                      type="button"
                      class="danger"
                      :disabled="mutatingUserId === friend.userId"
                      @click="removeFriend(friend.userId)"
                    >
                      Hủy kết bạn
                    </button>
                  </div>
                </article>
              </div>
            </section>

            <section
              v-else-if="activeTab === 'requests'"
              class="social-section"
            >
              <div class="social-section-head">
                <div>
                  <strong>Lời mời kết bạn</strong>
                </div>

                <button
                  type="button"
                  :disabled="isLoadingRequests"
                  @click="loadRequests"
                >
                  <span class="material-symbols-outlined">refresh</span>
                </button>
              </div>

              <div
                v-if="requestsError"
                class="social-error"
              >
                <span class="material-symbols-outlined">error</span>
                {{ requestsError }}
              </div>

              <div
                v-if="isLoadingRequests"
                class="social-skeleton-list"
              >
                <span v-for="index in 4" :key="index"></span>
              </div>

              <template v-else>
                <h3 class="social-subtitle">Đã nhận</h3>

                <div
                  v-if="!incomingRequests.length"
                  class="social-empty-row"
                >
                  Không có lời mời mới.
                </div>

                <article
                  v-for="request in incomingRequests"
                  :key="request.id"
                  class="social-request-card"
                >
                  <button
                    type="button"
                    class="social-user-main"
                    @click="openProfile(request.otherUser.id)"
                  >
                    <img
                      v-if="avatarUrl(request.otherUser.avatarUrl)"
                      :src="avatarUrl(request.otherUser.avatarUrl) || ''"
                      :alt="request.otherUser.fullName"
                      referrerpolicy="no-referrer"
                    />

                    <span v-else class="social-avatar-fallback">
                      {{ initials(request.otherUser.fullName || request.otherUser.userName) }}
                    </span>

                    <span>
                      <strong>{{ request.otherUser.fullName }}</strong>
                      <small>@{{ request.otherUser.userName }} · {{ formatRelativeTime(request.createdDate) }}</small>
                    </span>
                  </button>

                  <div class="social-user-actions">
                    <button
                      type="button"
                      :disabled="mutatingRequestId === request.id"
                      @click="acceptRequest(request.id)"
                    >
                      Chấp nhận
                    </button>

                    <button
                      type="button"
                      class="danger"
                      :disabled="mutatingRequestId === request.id"
                      @click="rejectRequest(request.id)"
                    >
                      Từ chối
                    </button>
                  </div>
                </article>

                <h3 class="social-subtitle mt">Đã gửi</h3>

                <div
                  v-if="!outgoingRequests.length"
                  class="social-empty-row"
                >
                  Không có lời mời đang chờ.
                </div>

                <article
                  v-for="request in outgoingRequests"
                  :key="request.id"
                  class="social-request-card"
                >
                  <button
                    type="button"
                    class="social-user-main"
                    @click="openProfile(request.otherUser.id)"
                  >
                    <img
                      v-if="avatarUrl(request.otherUser.avatarUrl)"
                      :src="avatarUrl(request.otherUser.avatarUrl) || ''"
                      :alt="request.otherUser.fullName"
                      referrerpolicy="no-referrer"
                    />

                    <span v-else class="social-avatar-fallback">
                      {{ initials(request.otherUser.fullName || request.otherUser.userName) }}
                    </span>

                    <span>
                      <strong>{{ request.otherUser.fullName }}</strong>
                      <small>@{{ request.otherUser.userName }} · Đang chờ</small>
                    </span>
                  </button>

                  <div class="social-user-actions">
                    <button
                      type="button"
                      class="danger"
                      :disabled="mutatingRequestId === request.id"
                      @click="cancelRequest(request.id)"
                    >
                      Hủy
                    </button>
                  </div>
                </article>
              </template>
            </section>

            <section
              v-else
              class="social-section"
            >
              <div class="social-section-head">
                <div>
                  <strong>Trang hồ sơ của tôi</strong>
                  <span>Trang cá nhân gắn với không gian công khai/riêng tư.</span>
                </div>

                <button
                  type="button"
                  :disabled="isLoadingProfile"
                  @click="loadMyProfile"
                >
                  <span class="material-symbols-outlined">refresh</span>
                </button>
              </div>

              <div
                v-if="profileError"
                class="social-error"
              >
                <span class="material-symbols-outlined">error</span>
                {{ profileError }}
              </div>

              <div
                v-if="isLoadingProfile && !selectedProfile"
                class="social-profile-skeleton"
              >
                <span></span>
                <span></span>
                <span></span>
              </div>

              <div
                v-else-if="!selectedProfile"
                class="social-empty-state"
              >
                <span class="material-symbols-outlined">badge</span>
                <strong>Chưa mở hồ sơ</strong>
                <p>Bấm tải lại để mở trang cá nhân của bạn hoặc chọn người dùng ở tab Tìm kiếm.</p>
              </div>

              <article
                v-else
                class="profile-card"
              >
                <div
                  class="profile-cover"
                  :class="{ 'is-empty': !coverUrl(selectedProfile.coverImageUrl) }"
                >
                  <img
                    v-if="coverUrl(selectedProfile.coverImageUrl)"
                    :src="coverUrl(selectedProfile.coverImageUrl) || ''"
                    alt="Ảnh bìa hồ sơ"
                    referrerpolicy="no-referrer"
                  />

                  <div
                    v-else
                    class="profile-cover-placeholder"
                    aria-hidden="true"
                  >
                    <span class="material-symbols-outlined">auto_awesome</span>
                  </div>

                  <input
                    id="social-profile-cover-input"
                    ref="coverFileInputRef"
                    type="file"
                    accept="image/*"
                    class="profile-cover-input"
                    :disabled="isUploadingCover"
                    @change="handleCoverSelected"
                  />

                  <label
                    v-if="normalizeFriendship(selectedProfile.friendshipStatus) === 'self'"
                    class="profile-cover-upload"
                    :class="{ 'is-disabled': isUploadingCover }"
                    for="social-profile-cover-input"
                    :aria-disabled="isUploadingCover"
                    @click.stop
                  >
                    <span class="material-symbols-outlined">image</span>
                    {{ isUploadingCover ? 'Đang tải lên...' : 'Ảnh bìa' }}
                  </label>
                </div>

                <div class="profile-main">
                  <div class="profile-avatar-shell">
                    <img
                      v-if="avatarUrl(selectedProfile.avatarUrl)"
                      :src="avatarUrl(selectedProfile.avatarUrl) || ''"
                      :alt="selectedProfile.fullName"
                      referrerpolicy="no-referrer"
                    />

                    <span v-else class="profile-avatar-fallback">
                      {{ initials(selectedProfile.fullName || selectedProfile.userName) }}
                    </span>
                  </div>

                  <div class="profile-identity">
                    <h3>{{ selectedProfile.fullName }}</h3>
                    <p>@{{ selectedProfile.userName }}</p>

                    <div class="profile-meta-row">
                      <span>
                        <span class="material-symbols-outlined">group</span>
                        {{ selectedProfile.friendCount }} bạn bè
                      </span>

                      <span>
                        <span class="material-symbols-outlined">workspaces</span>
                        {{ selectedProfile.workspaces.length }} không gian
                      </span>
                    </div>
                  </div>
                </div>

                <textarea
                  v-if="normalizeFriendship(selectedProfile.friendshipStatus) === 'self'"
                  v-model="profileBioDraft"
                  class="profile-bio-editor"
                  rows="4"
                  maxlength="500"
                  placeholder="Viết vài dòng về bạn và không gian của bạn..."
                ></textarea>

                <p
                  v-else
                  class="profile-bio"
                >
                  {{ selectedProfile.bio || 'Người dùng này chưa viết tiểu sử.' }}
                </p>

                <div
                  v-if="normalizeFriendship(selectedProfile.friendshipStatus) === 'self'"
                  class="profile-actions"
                >
                  <button
                    type="button"
                    :disabled="isSavingProfile"
                    @click="saveMyProfile"
                  >
                    {{ isSavingProfile ? 'Đang lưu...' : 'Lưu hồ sơ' }}
                  </button>
                </div>

                <section class="profile-workspaces">
                  <h4>Không gian công khai/riêng tư</h4>

                  <div
                    v-if="!selectedProfile.workspaces.length"
                    class="social-empty-row"
                  >
                    Chưa có không gian hiển thị ở trang cá nhân.
                  </div>

                  <button
                    v-for="workspace in selectedProfile.workspaces"
                    :key="workspace.id"
                    type="button"
                    class="profile-workspace-card"
                    :class="{ 'is-joining': isJoiningProfileWorkspaceId === workspace.id }"
                    :disabled="isJoiningProfileWorkspaceId === workspace.id"
                    @click="joinProfileWorkspace(workspace)"
                  >
                    <span class="profile-workspace-icon">
                      <span class="material-symbols-outlined">space_dashboard</span>
                    </span>

                    <span class="profile-workspace-copy">
                      <strong>{{ workspace.name }}</strong>
                      <p>{{ workspace.description || 'Chưa có mô tả' }}</p>
                    </span>

                    <span class="profile-workspace-side">
                      <span :class="visibilityClass(workspace.visibility)">
                        {{ visibilityLabel(workspace.visibility) }}
                      </span>

                      <small>
                        {{ workspaceActionLabel(workspace) }}
                      </small>
                    </span>
                  </button>
                </section>
              </article>
            </section>
          </main>
        </aside>
      </section>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import {
  nextTick,
  onBeforeUnmount,
  onMounted,
  ref,
  watch,
} from 'vue'
import { socialController } from '@/api/services/social.api'
import { workspaceController } from '@/api/services/workspace.api'
import { meController } from '@/api/services/me.api'
import { getApiErrorMessage, getApiResultErrorMessage } from '@/api/utils/api-error.util'
import type { Guid } from '@/api/models/common.model'
import type { WorkspaceResponse } from '@/api/models/workspace.model'
import type {
  FriendRequestResponse,
  FriendResponse,
  FriendshipStatus,
  ProfileWorkspaceResponse,
  UserProfilePageResponse,
  UserSearchResultResponse,
} from '@/api/models/social.model'
import { realtimeClient } from '@/realtime/realtime.client'
import { normalizeImageUrl } from '@/utils/image-url.util'
import { useToast } from '@/components/composables/useToast'

type SocialTab = 'search' | 'friends' | 'requests' | 'profile'

const props = defineProps<{
  open: boolean
}>()

const emit = defineEmits<{
  close: []
  'open-chat': [userId: Guid]
  'workspace-opened': [workspace: WorkspaceResponse]
}>()

const toast = useToast()

const tabs: Array<{
  value: SocialTab
  label: string
  icon: string
}> = [
  { value: 'search', label: 'Tìm kiếm', icon: 'search' },
  { value: 'friends', label: 'Bạn bè', icon: 'group' },
  { value: 'requests', label: 'Lời mời', icon: 'person_add' },
  { value: 'profile', label: 'Hồ sơ', icon: 'badge' },
]

const activeTab = ref<SocialTab>('search')
const searchInputRef = ref<HTMLInputElement | null>(null)
const coverFileInputRef = ref<HTMLInputElement | null>(null)

const searchKeyword = ref('')
const searchResults = ref<UserSearchResultResponse[]>([])
const isSearching = ref(false)
const searchError = ref<string | null>(null)
const mutatingUserId = ref<Guid | null>(null)

const friends = ref<FriendResponse[]>([])
const isLoadingFriends = ref(false)
const friendsError = ref<string | null>(null)

const incomingRequests = ref<FriendRequestResponse[]>([])
const outgoingRequests = ref<FriendRequestResponse[]>([])
const isLoadingRequests = ref(false)
const requestsError = ref<string | null>(null)
const mutatingRequestId = ref<Guid | null>(null)

const selectedProfile = ref<UserProfilePageResponse | null>(null)
const profileBioDraft = ref('')
const isLoadingProfile = ref(false)
const isSavingProfile = ref(false)
const isUploadingCover = ref(false)
const profileError = ref<string | null>(null)
const isJoiningProfileWorkspaceId = ref<Guid | null>(null)

let searchTimer: number | null = null
const unsubscribeRealtimeHandlers: Array<() => void> = []

watch(
  () => props.open,
  async (open) => {
    document.body.classList.toggle('social-hub-lock', open)

    if (!open) return

    await Promise.all([loadFriends(), loadRequests()])
    bindRealtime()
    void realtimeClient.start()

    await nextTick()
    searchInputRef.value?.focus()
  }
)

watch(searchKeyword, () => {
  if (searchTimer !== null) {
    window.clearTimeout(searchTimer)
  }

  searchTimer = window.setTimeout(() => {
    void searchUsers()
  }, 280)
})

watch(activeTab, (tab) => {
  if (tab === 'friends') void loadFriends()
  if (tab === 'requests') void loadRequests()
})

function selectTab(tab: SocialTab) {
  activeTab.value = tab

  if (tab === 'profile') {
    void loadMyProfile()
  }
}

function close() {
  document.body.classList.remove('social-hub-lock')
  emit('close')
}

function avatarUrl(value: string | null | undefined) {
  return normalizeImageUrl(value)
}

function coverUrl(value: string | null | undefined) {
  return normalizeImageUrl(value)
}

function initials(value: string) {
  const parts = value.trim().split(/\s+/).filter(Boolean).slice(0, 2)
  return parts.map((part) => part[0]?.toUpperCase()).join('') || '?'
}

function normalizeFriendship(status: FriendshipStatus) {
  return status?.trim().toLowerCase() || 'none'
}

function friendshipLabel(status: FriendshipStatus) {
  const value = normalizeFriendship(status)

  if (value === 'self') return 'Bạn'
  if (value === 'friends') return 'Bạn bè'
  if (value === 'request_sent') return 'Đã gửi lời mời'
  if (value === 'request_received') return 'Đang chờ bạn xác nhận'

  return 'Chưa kết bạn'
}

function friendshipActionLabel(status: FriendshipStatus) {
  const value = normalizeFriendship(status)

  if (value === 'self') return 'Bạn'
  if (value === 'request_sent') return 'Đang chờ'
  if (value === 'request_received') return 'Trả lời'

  return 'Đang chờ'
}

function visibilityClass(value: string) {
  return value.trim().toLowerCase() === 'public' ? 'public' : 'private'
}

function visibilityLabel(value: string) {
  return value.trim().toLowerCase() === 'public' ? 'Công khai' : 'Riêng tư'
}

function workspaceActionLabel(workspace: ProfileWorkspaceResponse) {
  if (isJoiningProfileWorkspaceId.value === workspace.id) return 'Đang mở...'

  const isSelf = selectedProfile.value
    ? normalizeFriendship(selectedProfile.value.friendshipStatus) === 'self'
    : false

  if (isSelf) return 'Mở'

  return workspace.visibility.trim().toLowerCase() === 'public'
    ? 'Tham gia để xem'
    : 'Mở'
}

function workspaceRoleLabel(value: string | null | undefined) {
  const normalized = value?.trim().toLowerCase()

  if (normalized === 'owner') return 'chủ sở hữu'
  if (normalized === 'manager') return 'quản lý'
  if (normalized === 'member') return 'thành viên'

  return 'người xem'
}

async function joinProfileWorkspace(workspace: ProfileWorkspaceResponse) {
  if (isJoiningProfileWorkspaceId.value) return

  isJoiningProfileWorkspaceId.value = workspace.id

  try {
    const result = await workspaceController.joinAsViewer(workspace.id)

    if (!result.isSuccess || !result.data) {
      toast.warning(
        'Không mở được không gian',
        getApiResultErrorMessage(result, 'Không gian này không còn công khai hoặc bạn chưa có quyền vào.')
      )
      return
    }

    const role = workspaceRoleLabel(result.data.currentUserRole?.toString())

    toast.success(
      'Đã mở không gian',
      `Bạn đang vào “${result.data.name}” với quyền ${role}.`
    )

    emit('workspace-opened', result.data)
  } catch (error) {
    toast.warning(
      'Không mở được không gian',
      getApiErrorMessage(error, 'Không gian này không còn công khai hoặc bạn chưa có quyền vào.')
    )
  } finally {
    isJoiningProfileWorkspaceId.value = null
  }
}

async function searchUsers() {
  const keyword = searchKeyword.value.trim()

  if (keyword.length < 2) {
    searchResults.value = []
    searchError.value = null
    return
  }

  isSearching.value = true
  searchError.value = null

  try {
    const result = await socialController.searchUsers({
      keyword,
      pageNumber: 1,
      pageSize: 20,
    })

    if (!result.isSuccess || !result.data) {
      searchError.value = getApiResultErrorMessage(
        result,
        'Không thể tìm người dùng.'
      )
      searchResults.value = []
      return
    }

    searchResults.value = result.data
  } catch (error) {
    searchError.value = getApiErrorMessage(error, 'Không thể tìm người dùng.')
    searchResults.value = []
  } finally {
    isSearching.value = false
  }
}

async function loadFriends() {
  isLoadingFriends.value = true
  friendsError.value = null

  try {
    const result = await socialController.listFriends(1, 80)

    if (!result.isSuccess || !result.data) {
      friendsError.value = getApiResultErrorMessage(
        result,
        'Không thể tải danh sách bạn bè.'
      )
      friends.value = []
      return
    }

    friends.value = result.data
  } catch (error) {
    friendsError.value = getApiErrorMessage(
      error,
      'Không thể tải danh sách bạn bè.'
    )
    friends.value = []
  } finally {
    isLoadingFriends.value = false
  }
}

async function loadRequests() {
  isLoadingRequests.value = true
  requestsError.value = null

  try {
    const [incomingResult, outgoingResult] = await Promise.all([
      socialController.listIncomingRequests(1, 80),
      socialController.listOutgoingRequests(1, 80),
    ])

    if (!incomingResult.isSuccess || !incomingResult.data) {
      requestsError.value = getApiResultErrorMessage(
        incomingResult,
        'Không thể tải lời mời kết bạn.'
      )
      incomingRequests.value = []
    } else {
      incomingRequests.value = incomingResult.data
    }

    if (!outgoingResult.isSuccess || !outgoingResult.data) {
      requestsError.value = getApiResultErrorMessage(
        outgoingResult,
        'Không thể tải lời mời đã gửi.'
      )
      outgoingRequests.value = []
    } else {
      outgoingRequests.value = outgoingResult.data
    }
  } catch (error) {
    requestsError.value = getApiErrorMessage(
      error,
      'Không thể tải lời mời kết bạn.'
    )
    incomingRequests.value = []
    outgoingRequests.value = []
  } finally {
    isLoadingRequests.value = false
  }
}

async function loadMyProfile() {
  profileError.value = null
  isLoadingProfile.value = true

  try {
    const meResult = await meController.getMyProfile()

    if (!meResult.isSuccess || !meResult.data) {
      profileError.value = getApiResultErrorMessage(
        meResult,
        'Không lấy được thông tin người dùng hiện tại.'
      )
      return
    }

    await openProfile(meResult.data.id)
  } catch (error) {
    profileError.value = getApiErrorMessage(
      error,
      'Không lấy được thông tin người dùng hiện tại.'
    )
  } finally {
    isLoadingProfile.value = false
  }
}

async function openProfile(userId: Guid) {
  isLoadingProfile.value = true
  profileError.value = null

  try {
    const result = await socialController.getProfile(userId)

    if (!result.isSuccess || !result.data) {
      profileError.value = getApiResultErrorMessage(
        result,
        'Không thể tải trang cá nhân.'
      )
      return
    }

    selectedProfile.value = result.data
    profileBioDraft.value = result.data.bio ?? ''
    activeTab.value = 'profile'
  } catch (error) {
    profileError.value = getApiErrorMessage(
      error,
      'Không thể tải trang cá nhân.'
    )
  } finally {
    isLoadingProfile.value = false
  }
}

async function sendRequest(userId: Guid) {
  mutatingUserId.value = userId

  try {
    const result = await socialController.sendFriendRequest({
      addresseeUserId: userId,
    })

    if (!result.isSuccess) {
      toast.error('Không gửi được lời mời', getApiResultErrorMessage(result))
      return
    }

    toast.success('Đã gửi lời mời', 'Người kia sẽ nhận thông báo thời gian thực.')
    await Promise.all([searchUsers(), loadRequests()])
  } catch (error) {
    toast.error('Không gửi được lời mời', getApiErrorMessage(error))
  } finally {
    mutatingUserId.value = null
  }
}

async function acceptRequest(requestId: Guid) {
  mutatingRequestId.value = requestId

  try {
    const result = await socialController.acceptFriendRequest(requestId)

    if (!result.isSuccess) {
      toast.error('Không thể xác nhận', getApiResultErrorMessage(result))
      return
    }

    toast.success('Đã kết bạn', 'Hai người giờ có thể nhắn tin riêng.')
    await Promise.all([loadRequests(), loadFriends(), searchUsers()])
  } catch (error) {
    toast.error('Không thể xác nhận', getApiErrorMessage(error))
  } finally {
    mutatingRequestId.value = null
  }
}

async function rejectRequest(requestId: Guid) {
  mutatingRequestId.value = requestId

  try {
    const result = await socialController.rejectFriendRequest(requestId)

    if (!result.isSuccess) {
      toast.error('Không thể từ chối', getApiResultErrorMessage(result))
      return
    }

    await Promise.all([loadRequests(), searchUsers()])
  } catch (error) {
    toast.error('Không thể từ chối', getApiErrorMessage(error))
  } finally {
    mutatingRequestId.value = null
  }
}

async function cancelRequest(requestId: Guid) {
  mutatingRequestId.value = requestId

  try {
    const result = await socialController.cancelFriendRequest(requestId)

    if (!result.isSuccess) {
      toast.error('Không thể hủy lời mời', getApiResultErrorMessage(result))
      return
    }

    await Promise.all([loadRequests(), searchUsers()])
  } catch (error) {
    toast.error('Không thể hủy lời mời', getApiErrorMessage(error))
  } finally {
    mutatingRequestId.value = null
  }
}

async function removeFriend(friendUserId: Guid) {
  mutatingUserId.value = friendUserId

  try {
    const result = await socialController.removeFriend(friendUserId)

    if (!result.isSuccess) {
      toast.error('Không thể xóa bạn', getApiResultErrorMessage(result))
      return
    }

    toast.success('Đã xóa bạn', 'Người này không còn nhắn tin riêng được nữa.')
    await Promise.all([loadFriends(), searchUsers()])
  } catch (error) {
    toast.error('Không thể xóa bạn', getApiErrorMessage(error))
  } finally {
    mutatingUserId.value = null
  }
}

async function saveMyProfile() {
  const profile = selectedProfile.value
  if (!profile || normalizeFriendship(profile.friendshipStatus) !== 'self') return

  isSavingProfile.value = true
  profileError.value = null

  try {
    const result = await socialController.updateMyProfilePage({
      bio: profileBioDraft.value.trim() || null,
      coverImageUrl: profile.coverImageUrl,
    })

    if (!result.isSuccess || !result.data) {
      profileError.value = getApiResultErrorMessage(
        result,
        'Không thể lưu hồ sơ.'
      )
      return
    }

    selectedProfile.value = result.data
    profileBioDraft.value = result.data.bio ?? ''
    toast.success('Đã lưu hồ sơ', 'Trang cá nhân của bạn đã được cập nhật.')
  } catch (error) {
    profileError.value = getApiErrorMessage(error, 'Không thể lưu hồ sơ.')
  } finally {
    isSavingProfile.value = false
  }
}

async function handleCoverSelected(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]

  input.value = ''

  if (!file) return

  isUploadingCover.value = true
  profileError.value = null

  try {
    const result = await socialController.uploadMyProfileCoverImage(file)

    if (!result.isSuccess || !result.data) {
      profileError.value = getApiResultErrorMessage(
        result,
        'Không thể tải ảnh bìa lên.'
      )
      return
    }

    selectedProfile.value = result.data
    profileBioDraft.value = result.data.bio ?? ''
    toast.success('Đã đổi ảnh bìa', 'Ảnh bìa hồ sơ đã được cập nhật.')
  } catch (error) {
    profileError.value = getApiErrorMessage(error, 'Không thể tải ảnh bìa lên.')
  } finally {
    isUploadingCover.value = false
  }
}

function bindRealtime() {
  if (unsubscribeRealtimeHandlers.length > 0) return

  const refreshSocial = async () => {
    await Promise.all([loadRequests(), loadFriends(), searchUsers()])
  }

  unsubscribeRealtimeHandlers.push(
    realtimeClient.on('FriendRequestReceived', async () => {
      toast.info('Lời mời kết bạn mới', 'Có người vừa gửi lời mời cho bạn.')
      await refreshSocial()
    })
  )

  unsubscribeRealtimeHandlers.push(
    realtimeClient.on('FriendRequestSent', refreshSocial)
  )

  unsubscribeRealtimeHandlers.push(
    realtimeClient.on('FriendRequestAccepted', async () => {
      toast.success('Đã trở thành bạn bè', 'Bạn có thể nhắn tin riêng ngay bây giờ.')
      await refreshSocial()
    })
  )

  unsubscribeRealtimeHandlers.push(
    realtimeClient.on('FriendRequestRejected', refreshSocial)
  )

  unsubscribeRealtimeHandlers.push(
    realtimeClient.on('FriendRequestCancelled', refreshSocial)
  )

  unsubscribeRealtimeHandlers.push(
    realtimeClient.on('FriendshipChanged', refreshSocial)
  )

  unsubscribeRealtimeHandlers.push(
    realtimeClient.on('FriendRemoved', async () => {
      toast.info('Bạn bè đã cập nhật', 'Danh sách bạn bè đã thay đổi.')
      await refreshSocial()
    })
  )
}

function unbindRealtime() {
  while (unsubscribeRealtimeHandlers.length) {
    unsubscribeRealtimeHandlers.pop()?.()
  }
}

function formatShortDate(value: string) {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''

  return date.toLocaleDateString('vi-VN')
}

function formatRelativeTime(value: string) {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''

  const diffMs = Date.now() - date.getTime()
  const minutes = Math.max(0, Math.floor(diffMs / 60000))

  if (minutes < 1) return 'Vừa xong'
  if (minutes < 60) return `${minutes} phút trước`

  const hours = Math.floor(minutes / 60)
  if (hours < 24) return `${hours} giờ trước`

  return `${Math.floor(hours / 24)} ngày trước`
}

onMounted(() => {
  bindRealtime()
})

onBeforeUnmount(() => {
  unbindRealtime()

  if (searchTimer !== null) {
    window.clearTimeout(searchTimer)
  }

  document.body.classList.remove('social-hub-lock')
})
</script>

<style scoped src="./css/SocialHubPanel.css"></style>
