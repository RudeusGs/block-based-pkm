<template>
  <Transition name="workspace-members-sidebar">
    <aside
      v-if="open"
      class="workspace-members-sidebar"
      role="dialog"
      aria-label="Thành viên workspace"
    >
      <header class="workspace-members-header">
        <div class="workspace-members-heading">
          <span>{{ workspaceName }}</span>
          <h2>Thành viên</h2>
        </div>

        <div class="workspace-members-actions">
          <button
            type="button"
            class="icon-btn"
            title="Tải lại"
            :disabled="isLoading"
            @click="emit('refresh')"
          >
            <span class="material-symbols-outlined">refresh</span>
          </button>

          <button
            type="button"
            class="icon-btn"
            title="Đóng"
            @click="emit('close')"
          >
            <span class="material-symbols-outlined">close</span>
          </button>
        </div>
      </header>

      <div class="workspace-members-summary">
        <span>{{ memberCountLabel }}</span>

        <span class="online-count">
          <span></span>
          {{ onlineMembers.length }} online
        </span>
      </div>

      <div v-if="isLoading" class="workspace-members-list">
        <div
          v-for="index in 5"
          :key="index"
          class="member-skeleton"
        >
          <div class="avatar-skeleton"></div>
          <div class="info-skeleton">
            <div class="line1"></div>
            <div class="line2"></div>
          </div>
        </div>
      </div>

      <div v-else-if="error" class="empty-state">
        <span class="material-symbols-outlined">group_off</span>
        <strong>Không thể tải thành viên</strong>
        <p>{{ error }}</p>
        <button type="button" @click="emit('refresh')">
          Thử lại
        </button>
      </div>

      <div v-else-if="!members.length" class="empty-state">
        <span class="material-symbols-outlined">groups</span>
        <strong>Chưa có thành viên</strong>
        <p>Danh sách thành viên workspace sẽ xuất hiện ở đây.</p>
      </div>

      <div v-else class="workspace-members-list">
        <section v-if="onlineMembers.length" class="members-group">
          <h3>Online</h3>

          <WorkspaceMemberRow
            v-for="member in onlineMembers"
            :key="member.userId"
            :member="member"
          />
        </section>

        <section v-if="offlineMembers.length" class="members-group">
          <h3>Offline</h3>

          <WorkspaceMemberRow
            v-for="member in offlineMembers"
            :key="member.userId"
            :member="member"
          />
        </section>
      </div>
    </aside>
  </Transition>
</template>

<script setup lang="ts">
import { defineComponent, h } from 'vue'
import type { WorkspaceMemberListItem } from '@/modules/workspaces/composables/useWorkspaceMembersSidebar'

defineProps<{
  open: boolean
  workspaceName: string
  members: WorkspaceMemberListItem[]
  onlineMembers: WorkspaceMemberListItem[]
  offlineMembers: WorkspaceMemberListItem[]
  memberCountLabel: string
  isLoading: boolean
  error: string | null
}>()

const emit = defineEmits<{
  close: []
  refresh: []
}>()

function roleLabel(member: WorkspaceMemberListItem) {
  if (member.isOwner) return 'Owner'

  const role = member.role?.trim()
  return role
    ? role.charAt(0).toUpperCase() + role.slice(1).toLowerCase()
    : 'Member'
}

const WorkspaceMemberRow = defineComponent({
  name: 'WorkspaceMemberRow',
  props: {
    member: {
      type: Object as () => WorkspaceMemberListItem,
      required: true,
    },
  },
  setup(props) {
    return () =>
      h('article', { class: 'member-row' }, [
        h('div', { class: 'avatar-wrapper' }, [
          props.member.avatarUrl
            ? h('img', {
                class: 'avatar',
                src: props.member.avatarUrl,
                alt: props.member.isCurrentUser
                  ? 'You'
                  : props.member.displayName,
              })
            : h(
                'div',
                { class: 'avatar avatar-fallback' },
                props.member.isCurrentUser ? 'Y' : props.member.initials
              ),

          h('span', {
            class: [
              'presence-dot',
              props.member.availability === 'online' ? 'online' : 'offline',
            ],
          }),
        ]),

        h('div', { class: 'member-info' }, [
          h('div', { class: 'member-name-row' }, [
            h(
              'strong',
              props.member.isCurrentUser ? 'You' : props.member.displayName
            ),
          ]),
        ]),

        h('span', { class: 'member-role' }, roleLabel(props.member)),
      ])
  },
})
</script>

<style scoped>
.workspace-members-sidebar {
  position: fixed;
  top: 48px;
  right: 0;
  z-index: 80;

  width: min(380px, 100vw);
  height: calc(100vh - 48px);

  display: flex;
  flex-direction: column;

  color: rgba(255, 255, 255, 0.88);
  background: #191919;
  border-left: 1px solid rgba(255, 255, 255, 0.08);
  box-shadow:
    -1px 0 0 rgba(255, 255, 255, 0.03),
    -18px 0 44px rgba(0, 0, 0, 0.32);
}

.workspace-members-header {
  min-height: 68px;
  padding: 14px 16px 12px;

  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;

  border-bottom: 1px solid rgba(255, 255, 255, 0.08);
}

.workspace-members-heading {
  min-width: 0;
}

.workspace-members-heading span {
  display: block;
  overflow: hidden;

  color: rgba(255, 255, 255, 0.42);
  font-size: 12px;
  font-weight: 500;
  line-height: 1.3;

  white-space: nowrap;
  text-overflow: ellipsis;
}

.workspace-members-heading h2 {
  margin: 4px 0 0;

  color: rgba(255, 255, 255, 0.9);
  font-size: 18px;
  font-weight: 650;
  line-height: 1.25;
  letter-spacing: -0.015em;
}

.workspace-members-actions {
  display: inline-flex;
  align-items: center;
  gap: 4px;
}

.icon-btn {
  width: 32px;
  height: 32px;

  border: 0;
  border-radius: 6px;

  display: inline-flex;
  align-items: center;
  justify-content: center;

  color: rgba(255, 255, 255, 0.45);
  background: transparent;

  cursor: pointer;
  transition:
    background 120ms ease,
    color 120ms ease,
    transform 120ms ease;
}

.icon-btn:hover:not(:disabled) {
  color: rgba(255, 255, 255, 0.86);
  background: rgba(255, 255, 255, 0.08);
}

.icon-btn:active:not(:disabled) {
  transform: scale(0.96);
}

.icon-btn:disabled {
  cursor: default;
  opacity: 0.38;
}

.icon-btn .material-symbols-outlined {
  font-size: 19px;
}

.workspace-members-summary {
  padding: 10px 16px;

  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;

  color: rgba(255, 255, 255, 0.45);
  font-size: 12px;
  font-weight: 500;

  border-bottom: 1px solid rgba(255, 255, 255, 0.07);
  background: #202020;
}

.online-count {
  display: inline-flex;
  align-items: center;
  gap: 6px;

  color: rgba(255, 255, 255, 0.58);
}

.online-count span {
  width: 7px;
  height: 7px;
  border-radius: 999px;
  background: #37a169;
}

.workspace-members-list {
  flex: 1;
  overflow-y: auto;
  padding: 10px 8px 14px;
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
  background: rgba(255, 255, 255, 0.18);
  background-clip: content-box;
}

.members-group + .members-group {
  margin-top: 14px;
}

.members-group h3 {
  margin: 0 0 4px;
  padding: 7px 8px 5px;

  color: rgba(255, 255, 255, 0.34);
  font-size: 11px;
  font-weight: 650;
  line-height: 1;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.member-row {
  min-width: 0;
  padding: 7px 8px;

  display: grid;
  grid-template-columns: 34px minmax(0, 1fr) auto;
  align-items: center;
  gap: 10px;

  border-radius: 7px;
  cursor: default;

  transition:
    background 120ms ease,
    box-shadow 120ms ease;
}

.member-row:hover {
  background: rgba(255, 255, 255, 0.06);
}

.avatar-wrapper {
  position: relative;
  width: 32px;
  height: 32px;
}

.avatar {
  width: 32px;
  height: 32px;

  border-radius: 7px;
  display: inline-flex;
  align-items: center;
  justify-content: center;

  object-fit: cover;
  color: rgba(255, 255, 255, 0.88);
  background: rgba(255, 255, 255, 0.1);

  font-size: 12px;
  font-weight: 650;
}

.avatar-fallback {
  text-transform: uppercase;
}

.presence-dot {
  position: absolute;
  right: -2px;
  bottom: -2px;

  width: 10px;
  height: 10px;

  border: 2px solid #191919;
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.22);
}

.presence-dot.online {
  background: #37a169;
}

.presence-dot.offline {
  background: rgba(255, 255, 255, 0.24);
}

.member-info {
  min-width: 0;
}

.member-name-row {
  min-width: 0;

  display: flex;
  align-items: center;
  gap: 6px;
}

.member-name-row strong {
  overflow: hidden;

  color: rgba(255, 255, 255, 0.86);
  font-size: 13.5px;
  font-weight: 560;
  line-height: 1.25;

  white-space: nowrap;
  text-overflow: ellipsis;
}

.member-role {
  flex-shrink: 0;

  padding: 4px 7px;
  border-radius: 999px;

  color: rgba(255, 255, 255, 0.48);
  background: rgba(255, 255, 255, 0.07);

  font-size: 11px;
  font-weight: 560;
  line-height: 1;
}

.empty-state {
  margin: 16px;
  padding: 28px 18px;

  border-radius: 10px;
  border: 1px solid rgba(255, 255, 255, 0.08);

  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;

  color: rgba(255, 255, 255, 0.48);
  background: rgba(255, 255, 255, 0.035);
}

.empty-state > .material-symbols-outlined {
  margin-bottom: 10px;

  color: rgba(255, 255, 255, 0.36);
  font-size: 28px;
}

.empty-state strong {
  color: rgba(255, 255, 255, 0.82);
  font-size: 14px;
  font-weight: 650;
}

.empty-state p {
  max-width: 260px;
  margin: 6px 0 0;

  font-size: 13px;
  line-height: 1.45;
}

.empty-state button {
  margin-top: 14px;
  padding: 7px 11px;

  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 7px;

  color: rgba(255, 255, 255, 0.82);
  background: rgba(255, 255, 255, 0.07);

  font-size: 13px;
  font-weight: 550;

  cursor: pointer;
}

.empty-state button:hover {
  background: rgba(255, 255, 255, 0.1);
}

.member-skeleton {
  padding: 7px 8px;

  display: grid;
  grid-template-columns: 34px 1fr;
  gap: 10px;
  align-items: center;
}

.avatar-skeleton,
.info-skeleton .line1,
.info-skeleton .line2 {
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
  width: 32px;
  height: 32px;
  border-radius: 7px;
}

.info-skeleton .line1 {
  width: 68%;
  height: 10px;
}

.info-skeleton .line2 {
  width: 46%;
  height: 9px;
  margin-top: 8px;
}

.workspace-members-sidebar-enter-active,
.workspace-members-sidebar-leave-active {
  transition:
    transform 180ms ease,
    opacity 140ms ease;
}

.workspace-members-sidebar-enter-from,
.workspace-members-sidebar-leave-to {
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
  .workspace-members-sidebar {
    top: 0;
    z-index: 950;
    width: 100vw;
    height: 100vh;
  }
}

@media (prefers-reduced-motion: reduce) {
  .workspace-members-sidebar-enter-active,
  .workspace-members-sidebar-leave-active,
  .avatar-skeleton,
  .info-skeleton .line1,
  .info-skeleton .line2 {
    transition: none;
    animation: none;
  }
}
</style>