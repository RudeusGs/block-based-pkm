<template>
  <Teleport to="body">
    <Transition name="workspace-members-layer">
      <div
        v-if="props.open"
        class="workspace-members-layer"
      >
        <button
          class="workspace-members-scrim"
          type="button"
          aria-label="Đóng danh sách thành viên"
          @click="emit('close')"
        ></button>

        <aside
          class="workspace-members-sidebar"
          role="dialog"
          aria-modal="true"
          aria-label="Thành viên không gian"
          tabindex="-1"
          @click.stop="activeMemberMenuId = null"
          @keydown.esc="emit('close')"
        >
          <header class="workspace-members-header">
            <div class="workspace-members-title-wrap">
              <div class="workspace-members-icon">
                <span class="material-symbols-outlined">groups</span>
              </div>

              <div class="workspace-members-heading">
                <span :title="props.workspaceName">
                  {{ props.workspaceName }}
                </span>

                <h2>Thành viên</h2>
              </div>
            </div>

            <div class="workspace-members-actions">
              <button
                type="button"
                class="members-icon-btn"
                title="Tải lại"
                :disabled="props.isLoading || props.isMutatingMember"
                @click.stop="emit('refresh')"
              >
                <span class="material-symbols-outlined">refresh</span>
              </button>

              <button
                type="button"
                class="members-icon-btn"
                title="Đóng"
                @click.stop="emit('close')"
              >
                <span class="material-symbols-outlined">close</span>
              </button>
            </div>
          </header>

          <section class="workspace-members-overview">
            <article class="overview-card main">
              <strong>{{ props.members.length }}</strong>
              <span>Tổng thành viên</span>
            </article>

            <article class="overview-card">
              <strong>{{ props.onlineMembers.length }}</strong>
              <span>Đang hoạt động</span>
            </article>

            <article class="overview-card">
              <strong>{{ adminCount }}</strong>
              <span>Quản trị</span>
            </article>
          </section>

          <div class="workspace-members-search">
            <span class="material-symbols-outlined">search</span>

            <input
              v-model="searchTerm"
              type="search"
              placeholder="Tìm theo tên, tên đăng nhập, vai trò..."
              autocomplete="off"
            />

            <button
              v-if="searchTerm"
              type="button"
              title="Xóa tìm kiếm"
              @click.stop="searchTerm = ''"
            >
              <span class="material-symbols-outlined">close</span>
            </button>
          </div>

          <div class="workspace-members-meta">
            <span>{{ props.memberCountLabel }}</span>

            <span class="online-count">
              <span></span>
              {{ props.onlineMembers.length }} đang hoạt động
            </span>
          </div>

          <div
            v-if="props.memberActionError"
            class="member-action-error"
          >
            <span class="material-symbols-outlined">error</span>
            <span>{{ props.memberActionError }}</span>
          </div>

          <main
            v-if="props.isLoading"
            class="workspace-members-list"
          >
            <div
              v-for="index in 6"
              :key="index"
              class="member-skeleton"
            >
              <div class="avatar-skeleton"></div>

              <div class="info-skeleton">
                <div class="line1"></div>
                <div class="line2"></div>
              </div>

              <div class="role-skeleton"></div>
            </div>
          </main>

          <main
            v-else-if="props.error"
            class="workspace-members-state"
          >
            <span class="material-symbols-outlined">group_off</span>
            <strong>Không thể tải thành viên</strong>
            <p>{{ props.error }}</p>

            <button
              type="button"
              @click="emit('refresh')"
            >
              <span class="material-symbols-outlined">refresh</span>
              Thử lại
            </button>
          </main>

          <main
            v-else-if="visibleMembers.length === 0"
            class="workspace-members-state compact"
          >
            <span class="material-symbols-outlined">person_search</span>
            <strong>Không tìm thấy thành viên</strong>
            <p>Thử nhập tên, tên đăng nhập hoặc vai trò khác.</p>
          </main>

          <main
            v-else
            class="workspace-members-list"
          >
            <section
              v-if="visibleOnlineMembers.length"
              class="members-group"
            >
              <div class="members-group-head">
                <h3>Đang hoạt động</h3>
                <span>{{ visibleOnlineMembers.length }}</span>
              </div>

              <article
                v-for="member in visibleOnlineMembers"
                :key="member.userId"
                class="member-row"
                :class="memberRowClass(member)"
              >
                <div class="member-avatar-wrap">
                  <img
                    v-if="canShowMemberAvatar(member)"
                    class="member-avatar"
                    :src="memberAvatarSrc(member)"
                    :alt="memberName(member)"
                    referrerpolicy="no-referrer"
                    @error="markAvatarFailed(member.userId)"
                  />

                  <div
                    v-else
                    class="member-avatar member-avatar-fallback"
                  >
                    {{ member.isCurrentUser ? 'B' : member.initials }}
                  </div>

                  <span
                    class="presence-dot online"
                    title="Đang hoạt động"
                  ></span>
                </div>

                <div class="member-copy">
                  <strong :title="memberName(member)">
                    {{ memberName(member) }}
                  </strong>

                  <span
                    v-if="!member.isCurrentUser"
                    class="member-subline"
                  >
                    {{ memberSubline(member) }}
                  </span>
                </div>

                <span
                  v-if="!member.isCurrentUser"
                  class="member-role"
                  :class="roleClass(member)"
                  :title="roleLabel(member)"
                >
                  {{ roleLabel(member) }}
                </span>

                <div
                  v-if="canShowMemberActions(member)"
                  class="member-action-wrap"
                  @click.stop
                >
                  <button
                    type="button"
                    class="member-more-btn"
                    :class="{ active: activeMemberMenuId === member.userId }"
                    :title="`Quản lý ${memberName(member)}`"
                    :disabled="isMemberMutating(member)"
                    @click="toggleMemberMenu(member)"
                  >
                    <span
                      v-if="isMemberMutating(member)"
                      class="member-action-spinner"
                    ></span>

                    <span
                      v-else
                      class="material-symbols-outlined"
                    >
                      more_horiz
                    </span>
                  </button>

                  <div
                    v-if="activeMemberMenuId === member.userId"
                    class="member-dropdown"
                  >
                    <div class="member-dropdown-label">Đổi vai trò</div>

                    <button
                      v-for="roleOption in roleOptions"
                      :key="roleOption.value"
                      type="button"
                      class="member-dropdown-item"
                      :disabled="isRoleActionDisabled(member, roleOption.value)"
                      @click="handleRoleChange(member, roleOption.value)"
                    >
                      <span class="material-symbols-outlined">
                        {{ isCurrentRole(member, roleOption.value) ? 'check' : roleOption.icon }}
                      </span>

                      <span>
                        <strong>{{ roleOption.label }}</strong>
                        <small>{{ roleOption.description }}</small>
                      </span>
                    </button>

                    <div class="member-dropdown-divider"></div>

                    <button
                      v-if="canTransferOwnerTo(member)"
                      type="button"
                      class="member-dropdown-item"
                      :disabled="isMemberMutating(member)"
                      @click="handleTransferOwnership(member)"
                    >
                      <span class="material-symbols-outlined">workspace_premium</span>

                      <span>
                        <strong>Chuyển chủ sở hữu</strong>
                        <small>Trao quyền sở hữu không gian cho người này.</small>
                      </span>
                    </button>

                    <button
                      type="button"
                      class="member-dropdown-item danger"
                      :disabled="isMemberMutating(member)"
                      @click="openRemoveMemberConfirm(member)"
                    >
                      <span class="material-symbols-outlined">person_remove</span>

                      <span>
                        <strong>Xóa khỏi không gian</strong>
                        <small>Gỡ quyền truy cập của thành viên này.</small>
                      </span>
                    </button>
                  </div>
                </div>
              </article>
            </section>

            <section
              v-if="visibleOfflineMembers.length"
              class="members-group"
            >
              <div class="members-group-head">
                <h3>Vắng mặt</h3>
                <span>{{ visibleOfflineMembers.length }}</span>
              </div>

              <article
                v-for="member in visibleOfflineMembers"
                :key="member.userId"
                class="member-row"
                :class="memberRowClass(member)"
              >
                <div class="member-avatar-wrap">
                  <img
                    v-if="canShowMemberAvatar(member)"
                    class="member-avatar"
                    :src="memberAvatarSrc(member)"
                    :alt="memberName(member)"
                    referrerpolicy="no-referrer"
                    @error="markAvatarFailed(member.userId)"
                  />

                  <div
                    v-else
                    class="member-avatar member-avatar-fallback"
                  >
                    {{ member.isCurrentUser ? 'B' : member.initials }}
                  </div>

                  <span
                    class="presence-dot offline"
                    title="Vắng mặt"
                  ></span>
                </div>

                <div class="member-copy">
                  <strong :title="memberName(member)">
                    {{ memberName(member) }}
                  </strong>

                  <span
                    v-if="!member.isCurrentUser"
                    class="member-subline"
                  >
                    {{ memberSubline(member) }}
                  </span>
                </div>

                <span
                  v-if="!member.isCurrentUser"
                  class="member-role"
                  :class="roleClass(member)"
                  :title="roleLabel(member)"
                >
                  {{ roleLabel(member) }}
                </span>

                <div
                  v-if="canShowMemberActions(member)"
                  class="member-action-wrap"
                  @click.stop
                >
                  <button
                    type="button"
                    class="member-more-btn"
                    :class="{ active: activeMemberMenuId === member.userId }"
                    :title="`Quản lý ${memberName(member)}`"
                    :disabled="isMemberMutating(member)"
                    @click="toggleMemberMenu(member)"
                  >
                    <span
                      v-if="isMemberMutating(member)"
                      class="member-action-spinner"
                    ></span>

                    <span
                      v-else
                      class="material-symbols-outlined"
                    >
                      more_horiz
                    </span>
                  </button>

                  <div
                    v-if="activeMemberMenuId === member.userId"
                    class="member-dropdown"
                  >
                    <div class="member-dropdown-label">Đổi vai trò</div>

                    <button
                      v-for="roleOption in roleOptions"
                      :key="roleOption.value"
                      type="button"
                      class="member-dropdown-item"
                      :disabled="isRoleActionDisabled(member, roleOption.value)"
                      @click="handleRoleChange(member, roleOption.value)"
                    >
                      <span class="material-symbols-outlined">
                        {{ isCurrentRole(member, roleOption.value) ? 'check' : roleOption.icon }}
                      </span>

                      <span>
                        <strong>{{ roleOption.label }}</strong>
                        <small>{{ roleOption.description }}</small>
                      </span>
                    </button>

                    <div class="member-dropdown-divider"></div>

                    <button
                      v-if="canTransferOwnerTo(member)"
                      type="button"
                      class="member-dropdown-item"
                      :disabled="isMemberMutating(member)"
                      @click="handleTransferOwnership(member)"
                    >
                      <span class="material-symbols-outlined">workspace_premium</span>

                      <span>
                        <strong>Chuyển chủ sở hữu</strong>
                        <small>Trao quyền sở hữu không gian cho người này.</small>
                      </span>
                    </button>

                    <button
                      type="button"
                      class="member-dropdown-item danger"
                      :disabled="isMemberMutating(member)"
                      @click="openRemoveMemberConfirm(member)"
                    >
                      <span class="material-symbols-outlined">person_remove</span>

                      <span>
                        <strong>Xóa khỏi không gian</strong>
                        <small>Gỡ quyền truy cập của thành viên này.</small>
                      </span>
                    </button>
                  </div>
                </div>
              </article>
            </section>
          </main>
        </aside>

        <ConfirmActionModal
          :open="Boolean(memberToRemove)"
          title="Xóa thành viên khỏi không gian?"
          :message="removeMemberConfirmMessage"
          :description="removeMemberConfirmDescription"
          confirm-label="Xóa thành viên"
          submitting-label="Đang xóa..."
          :is-submitting="isRemovingSelectedMember"
          :error="props.memberActionError"
          @close="closeRemoveMemberConfirm"
          @confirm="confirmRemoveMember"
        />
      </div>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import ConfirmActionModal from '@/components/shared/ConfirmActionModal.vue'
import { normalizeImageUrl } from '@/utils/image-url.util'
import type { Guid } from '@/api/models/common.model'
import type { WorkspaceRoleValue } from '@/api/models/workspace.model'
import type { WorkspaceMemberListItem } from '@/modules/workspaces/composables/useWorkspaceMembersSidebar'

const props = defineProps<{
  open: boolean
  workspaceName: string
  members: WorkspaceMemberListItem[]
  onlineMembers: WorkspaceMemberListItem[]
  offlineMembers: WorkspaceMemberListItem[]
  memberCountLabel: string
  isLoading: boolean
  error: string | null
  canManageMembers: boolean
  canTransferOwnership: boolean
  isMutatingMember: boolean
  mutatingMemberId: Guid | null
  memberActionError: string | null
}>()

const emit = defineEmits<{
  close: []
  refresh: []
  changeRole: [member: WorkspaceMemberListItem, role: WorkspaceRoleValue]
  removeMember: [member: WorkspaceMemberListItem]
  transferOwnership: [member: WorkspaceMemberListItem]
}>()

const roleOptions: Array<{
  value: WorkspaceRoleValue
  label: string
  description: string
  icon: string
}> = [
  {
    value: 'manager',
    label: 'Quản lý',
    description: 'Quản lý không gian và thành viên.',
    icon: 'admin_panel_settings',
  },
  {
    value: 'member',
    label: 'Thành viên',
    description: 'Tạo, sửa trang và công việc theo quyền.',
    icon: 'edit_note',
  },
  {
    value: 'viewer',
    label: 'Người xem',
    description: 'Chỉ xem nội dung không gian.',
    icon: 'visibility',
  },
]

const searchTerm = ref('')
const failedAvatarUserIds = ref<Set<string>>(new Set())
const activeMemberMenuId = ref<Guid | null>(null)
const memberToRemove = ref<WorkspaceMemberListItem | null>(null)

const normalizedSearchTerm = computed(() => {
  return searchTerm.value.trim().toLowerCase()
})

const adminCount = computed(() => {
  return props.members.filter((member) => {
    const role = member.role?.trim().toLowerCase()

    return member.isOwner || role === 'owner' || role === 'manager'
  }).length
})

const visibleMembers = computed(() => {
  const keyword = normalizedSearchTerm.value

  if (!keyword) {
    return props.members
  }

  return props.members.filter((member) => memberMatchesSearch(member, keyword))
})

const visibleOnlineMembers = computed(() => {
  return visibleMembers.value.filter((member) => member.availability === 'online')
})

const visibleOfflineMembers = computed(() => {
  return visibleMembers.value.filter((member) => member.availability === 'offline')
})

const isRemovingSelectedMember = computed(() => {
  return Boolean(
    memberToRemove.value &&
      props.isMutatingMember &&
      props.mutatingMemberId === memberToRemove.value.userId
  )
})

const removeMemberConfirmMessage = computed(() => {
  const member = memberToRemove.value

  if (!member) return 'Bạn có chắc muốn xóa thành viên này khỏi không gian không?'

  return `Bạn sắp xóa "${member.displayName}" khỏi không gian "${props.workspaceName}".`
})

const removeMemberConfirmDescription = computed(() => {
  const member = memberToRemove.value

  if (!member) return 'Thành viên này sẽ mất quyền truy cập không gian.'

  return `${member.email || member.userName || member.displayName} sẽ không còn thấy trang, công việc và tài nguyên trong không gian này.`
})

watch(
  () => props.members.map((member) => `${member.userId}:${member.avatarUrl ?? ''}:${member.role}`).join('|'),
  () => {
    failedAvatarUserIds.value = new Set()

    if (
      memberToRemove.value &&
      !props.members.some((member) => member.userId === memberToRemove.value?.userId)
    ) {
      memberToRemove.value = null
    }
  }
)

watch(
  () => props.open,
  (open) => {
    if (!open) {
      activeMemberMenuId.value = null
      memberToRemove.value = null
      searchTerm.value = ''
    }
  }
)

function memberAvatarSrc(member: WorkspaceMemberListItem) {
  return normalizeImageUrl(member.avatarUrl) ?? undefined
}

function canShowMemberAvatar(member: WorkspaceMemberListItem) {
  return Boolean(memberAvatarSrc(member)) && !failedAvatarUserIds.value.has(member.userId)
}

function markAvatarFailed(userId: string) {
  failedAvatarUserIds.value = new Set([...failedAvatarUserIds.value, userId])
}

function memberName(member: WorkspaceMemberListItem) {
  return member.isCurrentUser ? 'Bạn' : member.displayName
}

function memberSubline(member: WorkspaceMemberListItem) {
  const userName = member.userName?.trim()
  const status = availabilityLabel(member)

  if (!userName) {
    return status
  }

  const displayName = member.displayName.trim().toLowerCase()
  const normalizedUserName = userName.toLowerCase()

  if (displayName === normalizedUserName) {
    return status
  }

  return `@${userName} · ${status}`
}

function availabilityLabel(member: WorkspaceMemberListItem) {
  return member.availability === 'online' ? 'đang hoạt động' : 'vắng mặt'
}

function roleLabel(member: WorkspaceMemberListItem) {
  if (member.isOwner) return 'Chủ sở hữu'

  const role = member.role?.trim().toLowerCase()

  if (role === 'manager') return 'Quản lý'
  if (role === 'viewer') return 'Người xem'

  return 'Thành viên'
}

function normalizedRole(member: WorkspaceMemberListItem) {
  const role = member.role?.trim().toLowerCase()

  if (role === 'manager' || role === 'viewer') return role

  return 'member'
}

function roleClass(member: WorkspaceMemberListItem) {
  if (member.isOwner) return 'owner'

  const role = normalizedRole(member)

  if (role === 'manager') return 'manager'
  if (role === 'viewer') return 'viewer'

  return 'member'
}

function canShowMemberActions(member: WorkspaceMemberListItem) {
  return props.canManageMembers && !member.isCurrentUser && !member.isOwner
}

function memberRowClass(member: WorkspaceMemberListItem) {
  return {
    current: member.isCurrentUser,
    owner: member.isOwner,
    'has-actions': canShowMemberActions(member),
  }
}

function isMemberMutating(member: WorkspaceMemberListItem) {
  return props.isMutatingMember && props.mutatingMemberId === member.userId
}

function isCurrentRole(member: WorkspaceMemberListItem, role: WorkspaceRoleValue) {
  return normalizedRole(member) === role
}

function isRoleActionDisabled(
  member: WorkspaceMemberListItem,
  role: WorkspaceRoleValue
) {
  return (
    isMemberMutating(member) ||
    !canShowMemberActions(member) ||
    isCurrentRole(member, role)
  )
}

function toggleMemberMenu(member: WorkspaceMemberListItem) {
  activeMemberMenuId.value =
    activeMemberMenuId.value === member.userId ? null : member.userId
}

function handleRoleChange(
  member: WorkspaceMemberListItem,
  role: WorkspaceRoleValue
) {
  if (isRoleActionDisabled(member, role)) return

  activeMemberMenuId.value = null
  emit('changeRole', member, role)
}

function canTransferOwnerTo(member: WorkspaceMemberListItem) {
  return (
    props.canTransferOwnership &&
    props.canManageMembers &&
    !member.isCurrentUser &&
    !member.isOwner
  )
}

function handleTransferOwnership(member: WorkspaceMemberListItem) {
  if (!canTransferOwnerTo(member) || isMemberMutating(member)) return

  activeMemberMenuId.value = null
  emit('transferOwnership', member)
}

function openRemoveMemberConfirm(member: WorkspaceMemberListItem) {
  if (!canShowMemberActions(member) || isMemberMutating(member)) return

  activeMemberMenuId.value = null
  memberToRemove.value = member
}

function closeRemoveMemberConfirm() {
  if (isRemovingSelectedMember.value) return

  memberToRemove.value = null
}

function confirmRemoveMember() {
  if (!memberToRemove.value || isRemovingSelectedMember.value) return

  emit('removeMember', memberToRemove.value)
}

function memberMatchesSearch(member: WorkspaceMemberListItem, keyword: string) {
  return [
    memberName(member),
    member.userName,
    member.email,
    roleLabel(member),
    availabilityLabel(member),
  ]
    .filter(Boolean)
    .some((value) => value.toLowerCase().includes(keyword))
}
</script>

<style scoped>
.workspace-members-layer {
  --members-bg: #0f0f0f;
  --members-panel: #191919;
  --members-panel-soft: #202020;
  --members-panel-hover: #242424;
  --members-border: #2f2f2f;
  --members-border-soft: #262626;
  --members-text: #f1f1f1;
  --members-muted: #a3a3a3;
  --members-faint: #737373;
  --members-success: #75b798;

  position: fixed;
  inset: 48px 0 0;
  z-index: 900;
  pointer-events: none;
}

.workspace-members-layer,
.workspace-members-layer * {
  box-sizing: border-box;
}

.workspace-members-scrim {
  position: absolute;
  inset: 0;
  border: 0;
  background: rgba(0, 0, 0, 0.2);
  pointer-events: auto;
}

.workspace-members-sidebar {
  position: absolute;
  inset: 0 0 0 auto;
  width: min(408px, 100vw);
  height: 100%;

  display: flex;
  flex-direction: column;

  color: var(--members-text);
  background:
    radial-gradient(circle at top right, rgba(255, 255, 255, 0.055), transparent 32%),
    var(--members-panel);
  border-left: 1px solid var(--members-border);
  box-shadow: -24px 0 70px rgba(0, 0, 0, 0.42);

  pointer-events: auto;
  outline: 0;
}

.workspace-members-header {
  min-height: 68px;
  padding: 14px 14px 12px;

  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;

  border-bottom: 1px solid var(--members-border-soft);
}

.workspace-members-title-wrap {
  min-width: 0;
  display: flex;
  align-items: center;
  gap: 10px;
}

.workspace-members-icon {
  width: 34px;
  height: 34px;
  border-radius: 9px;

  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;

  color: #d8d8d8;
  background: #242424;
  border: 1px solid #303030;
}

.workspace-members-icon .material-symbols-outlined {
  font-size: 19px;
}

.workspace-members-heading {
  min-width: 0;
}

.workspace-members-heading span {
  display: block;
  overflow: hidden;

  color: var(--members-faint);
  font-size: 12px;
  font-weight: 520;
  line-height: 1.3;

  white-space: nowrap;
  text-overflow: ellipsis;
}

.workspace-members-heading h2 {
  margin: 3px 0 0;

  color: var(--members-text);
  font-size: 18px;
  font-weight: 680;
  line-height: 1.22;
  letter-spacing: -0.018em;
}

.workspace-members-actions {
  display: inline-flex;
  align-items: center;
  gap: 3px;
  flex-shrink: 0;
}

.members-icon-btn {
  width: 31px;
  height: 31px;

  border: 0;
  border-radius: 7px;

  display: inline-flex;
  align-items: center;
  justify-content: center;

  color: var(--members-muted);
  background: transparent;

  cursor: pointer;
  transition:
    background 140ms ease,
    color 140ms ease,
    transform 140ms ease;
}

.members-icon-btn:hover:not(:disabled) {
  color: var(--members-text);
  background: var(--members-panel-hover);
}

.members-icon-btn:active:not(:disabled) {
  transform: scale(0.96);
}

.members-icon-btn:disabled {
  cursor: not-allowed;
  opacity: 0.45;
}

.members-icon-btn .material-symbols-outlined {
  font-size: 18px;
}

.workspace-members-overview {
  padding: 12px 14px 10px;

  display: grid;
  grid-template-columns: 1.25fr 1fr 1fr;
  gap: 8px;

  border-bottom: 1px solid var(--members-border-soft);
}

.overview-card {
  min-width: 0;
  padding: 10px 10px 9px;

  border: 1px solid #292929;
  border-radius: 10px;

  background: rgba(255, 255, 255, 0.025);
}

.overview-card.main {
  background: #202020;
  border-color: #333333;
}

.overview-card strong {
  display: block;
  color: var(--members-text);
  font-size: 18px;
  font-weight: 720;
  line-height: 1;
}

.overview-card span {
  display: block;
  margin-top: 6px;

  overflow: hidden;
  color: var(--members-faint);
  font-size: 11.5px;
  font-weight: 540;
  white-space: nowrap;
  text-overflow: ellipsis;
}

.workspace-members-search {
  margin: 12px 14px 10px;
  min-height: 36px;
  padding: 0 8px 0 10px;

  display: flex;
  align-items: center;
  gap: 7px;

  border: 1px solid #2a2a2a;
  border-radius: 9px;
  background: #121212;

  transition:
    border-color 140ms ease,
    background 140ms ease,
    box-shadow 140ms ease;
}

.workspace-members-search:focus-within {
  border-color: #3f3f3f;
  background: #151515;
  box-shadow: 0 0 0 3px rgba(255, 255, 255, 0.035);
}

.workspace-members-search > .material-symbols-outlined {
  color: var(--members-faint);
  font-size: 17px;
}

.workspace-members-search input {
  min-width: 0;
  flex: 1;
  height: 34px;

  border: 0;
  outline: 0;

  color: var(--members-text);
  background: transparent;

  font: inherit;
  font-size: 13px;
}

.workspace-members-search input::placeholder {
  color: #666666;
}

.workspace-members-search button {
  width: 23px;
  height: 23px;
  border: 0;
  border-radius: 6px;

  display: inline-flex;
  align-items: center;
  justify-content: center;

  color: var(--members-faint);
  background: transparent;
  cursor: pointer;
}

.workspace-members-search button:hover {
  color: var(--members-text);
  background: #252525;
}

.workspace-members-search button .material-symbols-outlined {
  font-size: 15px;
}

.workspace-members-meta {
  padding: 0 14px 10px;

  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;

  color: var(--members-faint);
  font-size: 12px;
  font-weight: 540;
}

.online-count {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  flex-shrink: 0;
  color: var(--members-muted);
}

.online-count > span {
  width: 7px;
  height: 7px;
  border-radius: 999px;
  background: var(--members-success);
  box-shadow: 0 0 0 3px rgba(117, 183, 152, 0.13);
}

.workspace-members-list {
  flex: 1;
  overflow-y: auto;
  padding: 3px 8px 16px;
}

.workspace-members-list::-webkit-scrollbar {
  width: 10px;
}

.workspace-members-list::-webkit-scrollbar-track {
  background: transparent;
}

.workspace-members-list::-webkit-scrollbar-thumb {
  border: 3px solid transparent;
  border-radius: 999px;
  background: #3a3a3a;
  background-clip: content-box;
}

.workspace-members-list::-webkit-scrollbar-thumb:hover {
  background: #4a4a4a;
  background-clip: content-box;
}

.members-group + .members-group {
  margin-top: 14px;
}

.members-group-head {
  min-height: 30px;
  padding: 7px 8px 5px;

  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

.members-group-head h3 {
  margin: 0;

  color: #858585;
  font-size: 11px;
  font-weight: 690;
  line-height: 1;
  letter-spacing: 0.045em;
  text-transform: uppercase;
}

.members-group-head span {
  min-width: 22px;
  height: 20px;
  padding: 0 7px;
  border-radius: 999px;

  display: inline-flex;
  align-items: center;
  justify-content: center;

  color: #8f8f8f;
  background: #202020;

  font-size: 11px;
  font-weight: 650;
}

.member-row {
  min-width: 0;
  min-height: 54px;
  padding: 8px;

  display: grid;
  grid-template-columns: 36px minmax(0, 1fr) max-content;
  align-items: center;
  gap: 10px;

  border: 1px solid transparent;
  border-radius: 9px;

  cursor: default;
  transition:
    background 140ms ease,
    border-color 140ms ease;
}

.member-row:hover {
  background: var(--members-panel-hover);
  border-color: #303030;
}

.member-row.current {
  grid-template-columns: 36px minmax(0, 1fr);
  background: rgba(255, 255, 255, 0.035);
}

.member-avatar-wrap {
  position: relative;
  width: 34px;
  height: 34px;
}

.member-avatar {
  width: 34px;
  height: 34px;

  border-radius: 8px;

  display: inline-flex;
  align-items: center;
  justify-content: center;

  object-fit: cover;
  color: var(--members-text);
  background:
    linear-gradient(135deg, rgba(255, 255, 255, 0.12), rgba(255, 255, 255, 0.045)),
    #2a2a2a;
  border: 1px solid rgba(255, 255, 255, 0.055);

  font-size: 12px;
  font-weight: 720;
}

.member-avatar-fallback {
  text-transform: uppercase;
}

.presence-dot {
  position: absolute;
  right: -2px;
  bottom: -2px;

  width: 11px;
  height: 11px;

  border: 2px solid var(--members-panel);
  border-radius: 999px;
  background: #565656;
}

.presence-dot.online {
  background: var(--members-success);
}

.presence-dot.offline {
  background: #5f5f5f;
}

.member-copy {
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.member-copy strong {
  overflow: hidden;

  color: #e8e8e8;
  font-size: 13.5px;
  font-weight: 650;
  line-height: 1.25;

  white-space: nowrap;
  text-overflow: ellipsis;
}

.member-row.current .member-copy strong {
  font-size: 14px;
}

.member-subline {
  overflow: hidden;

  color: var(--members-faint);
  font-size: 12px;
  line-height: 1.25;

  white-space: nowrap;
  text-overflow: ellipsis;
}

.member-role {
  max-width: 86px;
  min-height: 26px;
  padding: 0 8px;

  border-radius: 7px;

  display: inline-flex;
  align-items: center;
  justify-content: center;

  overflow: hidden;
  color: #bdbdbd;
  background: #222222;
  border: 1px solid #2d2d2d;

  font-size: 11.5px;
  font-weight: 620;
  line-height: 1;

  white-space: nowrap;
  text-overflow: ellipsis;
}

.member-role.owner {
  color: #ffdca8;
  background: rgba(255, 209, 102, 0.095);
  border-color: rgba(255, 209, 102, 0.16);
}

.member-role.manager {
  color: #c7d2fe;
  background: rgba(129, 140, 248, 0.11);
  border-color: rgba(129, 140, 248, 0.18);
}

.member-role.viewer {
  color: #b5b5b5;
  background: rgba(255, 255, 255, 0.045);
}

.workspace-members-state {
  margin: 16px 14px;
  padding: 30px 18px;

  border: 1px solid #2b2b2b;
  border-radius: 12px;

  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;

  color: var(--members-faint);
  background:
    radial-gradient(circle at top, rgba(255, 255, 255, 0.055), transparent 55%),
    rgba(255, 255, 255, 0.025);
}

.workspace-members-state.compact {
  margin-top: 22px;
}

.workspace-members-state > .material-symbols-outlined {
  width: 42px;
  height: 42px;
  margin-bottom: 12px;
  border-radius: 12px;

  display: inline-flex;
  align-items: center;
  justify-content: center;

  color: #bdbdbd;
  background: #222222;

  font-size: 23px;
}

.workspace-members-state strong {
  color: #e8e8e8;
  font-size: 14px;
  font-weight: 690;
}

.workspace-members-state p {
  max-width: 270px;
  margin: 7px 0 0;

  font-size: 13px;
  line-height: 1.5;
}

.workspace-members-state button {
  margin-top: 15px;
  min-height: 32px;
  padding: 0 11px;

  border: 1px solid #353535;
  border-radius: 7px;

  display: inline-flex;
  align-items: center;
  gap: 6px;

  color: var(--members-text);
  background: #232323;

  font: inherit;
  font-size: 13px;
  font-weight: 590;

  cursor: pointer;
}

.workspace-members-state button:hover {
  background: #2b2b2b;
}

.workspace-members-state button .material-symbols-outlined {
  font-size: 16px;
}

.member-skeleton {
  min-height: 56px;
  padding: 8px;

  display: grid;
  grid-template-columns: 36px 1fr 74px;
  gap: 10px;
  align-items: center;
}

.avatar-skeleton,
.info-skeleton .line1,
.info-skeleton .line2,
.role-skeleton {
  border-radius: 999px;
  background: linear-gradient(
    90deg,
    rgba(255, 255, 255, 0.045),
    rgba(255, 255, 255, 0.085),
    rgba(255, 255, 255, 0.045)
  );
  background-size: 220% 100%;
  animation: member-skeleton-loading 1.2s ease-in-out infinite;
}

.avatar-skeleton {
  width: 34px;
  height: 34px;
  border-radius: 8px;
}

.info-skeleton .line1 {
  width: 72%;
  height: 10px;
}

.info-skeleton .line2 {
  width: 52%;
  height: 9px;
  margin-top: 8px;
}

.role-skeleton {
  width: 70px;
  height: 24px;
  border-radius: 7px;
}

.workspace-members-layer-enter-active,
.workspace-members-layer-leave-active {
  transition: opacity 160ms ease;
}

.workspace-members-layer-enter-active .workspace-members-sidebar,
.workspace-members-layer-leave-active .workspace-members-sidebar {
  transition:
    transform 190ms ease,
    opacity 160ms ease;
}

.workspace-members-layer-enter-from,
.workspace-members-layer-leave-to {
  opacity: 0;
}

.workspace-members-layer-enter-from .workspace-members-sidebar,
.workspace-members-layer-leave-to .workspace-members-sidebar {
  opacity: 0;
  transform: translateX(100%);
}

@keyframes member-skeleton-loading {
  0% {
    background-position: 120% 0;
  }

  100% {
    background-position: -120% 0;
  }
}

@media (max-width: 575.98px) {
  .workspace-members-layer {
    inset: 0;
    z-index: 900;
  }

  .workspace-members-sidebar {
    width: 100vw;
    border-left: 0;
  }

  .workspace-members-overview {
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }

  .member-row {
    grid-template-columns: 36px minmax(0, 1fr);
  }

  .member-row:not(.current) .member-role {
    grid-column: 2;
    width: fit-content;
    max-width: 100%;
    margin-top: -2px;
  }
}

@media (max-width: 390px) {
  .workspace-members-overview {
    grid-template-columns: 1fr 1fr;
  }

  .overview-card.main {
    grid-column: 1 / -1;
  }
}

@media (prefers-reduced-motion: reduce) {
  .workspace-members-layer-enter-active,
  .workspace-members-layer-leave-active,
  .workspace-members-layer-enter-active .workspace-members-sidebar,
  .workspace-members-layer-leave-active .workspace-members-sidebar,
  .avatar-skeleton,
  .info-skeleton .line1,
  .info-skeleton .line2,
  .role-skeleton {
    transition: none;
    animation: none;
  }
}

.member-row.has-actions {
  grid-template-columns: 36px minmax(0, 1fr) max-content 30px;
}

.member-action-wrap {
  position: relative;
  display: inline-flex;
  justify-content: flex-end;
}

.member-more-btn {
  width: 28px;
  height: 28px;
  border: 0;
  border-radius: 7px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: var(--members-faint);
  background: transparent;
  cursor: pointer;
  transition:
    background 140ms ease,
    color 140ms ease,
    opacity 140ms ease;
}

.member-more-btn:hover:not(:disabled),
.member-more-btn.active {
  color: var(--members-text);
  background: #2a2a2a;
}

.member-more-btn:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}

.member-more-btn .material-symbols-outlined {
  font-size: 18px;
}

.member-dropdown {
  position: absolute;
  right: 0;
  top: calc(100% + 6px);
  z-index: 20;
  width: 214px;
  padding: 6px;
  border: 1px solid #333333;
  border-radius: 10px;
  background: #1b1b1b;
  box-shadow:
    0 18px 52px rgba(0, 0, 0, 0.46),
    0 0 0 1px rgba(255, 255, 255, 0.03);
}

.member-dropdown-label {
  padding: 6px 8px 5px;
  color: #7f7f7f;
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.035em;
  text-transform: uppercase;
}

.member-dropdown-item {
  width: 100%;
  min-height: 34px;
  border: 0;
  border-radius: 7px;
  display: grid;
  grid-template-columns: 18px minmax(0, 1fr);
  align-items: center;
  gap: 8px;
  padding: 6px 8px;
  color: #d7d7d7;
  background: transparent;
  text-align: left;
  cursor: pointer;
}

.member-dropdown-item:hover:not(:disabled) {
  color: #f1f1f1;
  background: #292929;
}

.member-dropdown-item:disabled {
  opacity: 0.48;
  cursor: not-allowed;
}

.member-dropdown-item .material-symbols-outlined {
  color: #9a9a9a;
  font-size: 17px;
}

.member-dropdown-item strong {
  display: block;
  color: inherit;
  font-size: 12.5px;
  font-weight: 650;
  line-height: 1.2;
}

.member-dropdown-item small {
  display: block;
  margin-top: 2px;
  color: #777777;
  font-size: 11px;
  line-height: 1.25;
}

.member-dropdown-item.danger {
  color: #f0a7a7;
}

.member-dropdown-item.danger:hover:not(:disabled) {
  color: #ffc7c7;
  background: rgba(239, 68, 68, 0.11);
}

.member-dropdown-divider {
  height: 1px;
  margin: 6px 2px;
  background: #303030;
}

.member-action-error {
  margin: 0 14px 10px;
  padding: 8px 10px;
  border: 1px solid rgba(248, 113, 113, 0.22);
  border-radius: 8px;
  display: flex;
  align-items: flex-start;
  gap: 8px;
  color: #f0a7a7;
  background: rgba(239, 68, 68, 0.075);
  font-size: 12px;
  line-height: 1.45;
}

.member-action-error .material-symbols-outlined {
  margin-top: 1px;
  font-size: 16px;
}

.member-action-spinner {
  width: 14px;
  height: 14px;
  border-radius: 999px;
  border: 2px solid #3a3a3a;
  border-top-color: #f1f1f1;
  animation: member-skeleton-loading 0.75s linear infinite;
}

@media (max-width: 575.98px) {
  .member-row.has-actions {
    grid-template-columns: 36px minmax(0, 1fr) 30px;
  }

  .member-row.has-actions .member-role {
    grid-column: 2;
  }

  .member-row.has-actions .member-action-wrap {
    grid-column: 3;
    grid-row: 1;
  }

  .member-dropdown {
    right: 0;
    width: min(214px, calc(100vw - 28px));
  }
}

</style>
