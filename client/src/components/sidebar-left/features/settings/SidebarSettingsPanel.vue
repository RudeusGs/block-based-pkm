<template>
  <Teleport to="body">
    <Transition name="settings-modal">
      <div
        class="settings-modal-layer"
        role="presentation"
      >
        <div
          class="settings-modal-backdrop"
          @click="emit('close')"
        ></div>

        <section
          class="settings-modal-shell"
          role="dialog"
          aria-modal="true"
          aria-labelledby="settings-modal-title"
          @click.stop
        >
          <aside class="settings-modal-sidebar">
            <div class="settings-modal-brand">
              <div class="settings-modal-brand-icon">
                <i class="bi bi-sliders2"></i>
              </div>

              <div class="settings-modal-brand-copy">
                <span>Workspace</span>
                <strong>Settings</strong>
              </div>
            </div>

            <nav class="settings-modal-tabs" aria-label="Settings tabs">
              <button
                type="button"
                class="settings-modal-tab"
                :class="{ active: activeTab === 'profile' }"
                @click="activeTab = 'profile'"
              >
                <span class="settings-modal-tab-icon">
                  <i class="bi bi-person-circle"></i>
                </span>

                <span class="settings-modal-tab-copy">
                  <strong>Profile</strong>
                  <small>Name, avatar and account identity</small>
                </span>
              </button>

              <button
                type="button"
                class="settings-modal-tab"
                :class="{ active: activeTab === 'ai' }"
                @click="activeTab = 'ai'"
              >
                <span class="settings-modal-tab-icon">
                  <i class="bi bi-magic"></i>
                </span>

                <span class="settings-modal-tab-copy">
                  <strong>AI suggestions</strong>
                  <small>Task recommendation preferences</small>
                </span>
              </button>

              <button
                type="button"
                class="settings-modal-tab"
                :class="{ active: activeTab === 'security' }"
                @click="activeTab = 'security'"
              >
                <span class="settings-modal-tab-icon">
                  <i class="bi bi-shield-lock"></i>
                </span>

                <span class="settings-modal-tab-copy">
                  <strong>Security</strong>
                  <small>Password and sign-in protection</small>
                </span>
              </button>
            </nav>

            <div class="settings-modal-sidebar-foot">
              <span class="settings-modal-foot-dot"></span>
              <span>Press Esc to close</span>
            </div>
          </aside>

          <main class="settings-modal-main">
            <header class="settings-modal-header">
              <div>
                <p class="settings-modal-eyebrow">
                  {{ activeTabEyebrow }}
                </p>

                <h2 id="settings-modal-title">
                  {{ activeTabTitle }}
                </h2>

                <p class="settings-modal-description">
                  {{ activeTabDescription }}
                </p>
              </div>

              <button
                type="button"
                class="settings-modal-close"
                title="Đóng Settings"
                aria-label="Đóng Settings"
                @click="emit('close')"
              >
                <i class="bi bi-x-lg"></i>
              </button>
            </header>

            <div class="settings-modal-content">
              <section
                v-if="activeTab === 'profile'"
                class="settings-modal-section"
              >
                <div
                  v-if="isLoadingProfileSettings"
                  class="settings-modal-empty"
                >
                  <span class="settings-modal-spinner"></span>
                  <span>Đang tải hồ sơ…</span>
                </div>

                <template v-else>
                  <div class="settings-modal-card profile-card">
                    <div class="settings-profile-preview">
                      <span class="settings-profile-avatar">
                        <img
                          v-if="canPreviewAvatar"
                          :key="profileAvatarPreviewSrc"
                          :src="profileAvatarPreviewSrc"
                          :alt="profileForm.fullName || 'Avatar'"
                          referrerpolicy="no-referrer"
                          @error="avatarPreviewFailed = true"
                          @load="avatarPreviewFailed = false"
                        />

                        <span v-else>
                          {{ profileInitial }}
                        </span>
                      </span>

                      <div class="settings-profile-preview-copy">
                        <strong>
                          {{ profileForm.fullName.trim() || 'Unnamed user' }}
                        </strong>

                        <span>
                          {{ avatarPreviewHint }}
                        </span>
                      </div>
                    </div>

                    <a
                      v-if="profileAvatarPreviewSrc"
                      class="settings-avatar-open-link"
                      :href="profileAvatarPreviewSrc"
                      target="_blank"
                      rel="noreferrer"
                    >
                      Mở ảnh trong tab mới
                      <i class="bi bi-box-arrow-up-right"></i>
                    </a>
                  </div>

                  <div class="settings-modal-card">
                    <label class="settings-field">
                      <span>Full name</span>

                      <input
                        v-model="profileForm.fullName"
                        type="text"
                        maxlength="100"
                        placeholder="Tên hiển thị"
                        :disabled="isSavingProfileSettings"
                      />
                    </label>

                    <label class="settings-field">
                      <span>Avatar URL</span>

                      <input
                        v-model="profileForm.avatarUrl"
                        type="text"
                        placeholder="https://... hoặc Google Drive/Dropbox/GitHub image link"
                        :disabled="isSavingProfileSettings"
                      />
                    </label>

                    <p class="settings-field-help">
                      Hỗ trợ link ảnh trực tiếp, Google Drive public file, Dropbox share link và GitHub blob image.
                    </p>

                    <p
                      v-if="profileSettingsError"
                      class="settings-inline-error"
                    >
                      {{ profileSettingsError }}
                    </p>

                    <p
                      v-if="profileSettingsSuccess"
                      class="settings-inline-success"
                    >
                      {{ profileSettingsSuccess }}
                    </p>

                    <div class="settings-modal-actions">
                      <button
                        type="button"
                        class="settings-primary-action"
                        :disabled="isSavingProfileSettings || !profileForm.fullName.trim()"
                        @click="emit('saveProfile')"
                      >
                        <span
                          v-if="isSavingProfileSettings"
                          class="settings-modal-spinner dark"
                        ></span>

                        <span>
                          {{ isSavingProfileSettings ? 'Saving...' : 'Save profile' }}
                        </span>
                      </button>
                    </div>
                  </div>
                </template>
              </section>

              <section
                v-else-if="activeTab === 'ai'"
                class="settings-modal-section"
              >
                <div
                  v-if="!workspaceId"
                  class="settings-modal-empty"
                >
                  <i class="bi bi-folder2-open"></i>
                  <span>Chọn workspace để cấu hình AI.</span>
                </div>

                <div
                  v-else-if="isLoadingTaskPreference"
                  class="settings-modal-empty"
                >
                  <span class="settings-modal-spinner"></span>
                  <span>Đang tải cấu hình AI…</span>
                </div>

                <template v-else>
                  <div class="settings-modal-card">
                    <div class="settings-card-head">
                      <div>
                        <strong>Working window</strong>
                        <span>AI ưu tiên gợi ý task trong khung giờ này.</span>
                      </div>
                    </div>

                    <div class="settings-field-grid">
                      <label class="settings-field">
                        <span>Start hour</span>

                        <input
                          v-model.number="taskPreferenceForm.workDayStartHour"
                          type="number"
                          min="0"
                          max="23"
                          :disabled="isSavingTaskPreference"
                        />
                      </label>

                      <label class="settings-field">
                        <span>End hour</span>

                        <input
                          v-model.number="taskPreferenceForm.workDayEndHour"
                          type="number"
                          min="1"
                          max="23"
                          :disabled="isSavingTaskPreference"
                        />
                      </label>
                    </div>

                    <label class="settings-field">
                      <span>Preferred days</span>

                      <div class="settings-day-grid">
                        <button
                          v-for="day in preferredDayOptions"
                          :key="day.value"
                          type="button"
                          :title="day.title"
                          :class="{ active: taskPreferenceForm.preferredDaysOfWeek.includes(day.value) }"
                          :disabled="isSavingTaskPreference"
                          @click="emit('togglePreferredDay', day.value)"
                        >
                          {{ day.label }}
                        </button>
                      </div>
                    </label>
                  </div>

                  <div class="settings-modal-card">
                    <div class="settings-card-head">
                      <div>
                        <strong>Recommendation behavior</strong>
                        <span>Điều chỉnh số lượng, độ nhạy và độ ưu tiên task.</span>
                      </div>
                    </div>

                    <div class="settings-field-grid">
                      <label class="settings-field">
                        <span>Max/session</span>

                        <input
                          v-model.number="taskPreferenceForm.maxRecommendationsPerSession"
                          type="number"
                          min="1"
                          max="20"
                          :disabled="isSavingTaskPreference"
                        />
                      </label>

                      <label class="settings-field">
                        <span>Min priority</span>

                        <select
                          v-model="taskPreferenceForm.minPriorityForRecommendation"
                          :disabled="isSavingTaskPreference"
                        >
                          <option value="low">Low</option>
                          <option value="medium">Medium</option>
                          <option value="high">High</option>
                        </select>
                      </label>
                    </div>

                    <label class="settings-field">
                      <span>
                        Sensitivity {{ taskPreferenceForm.recommendationSensitivity }}
                      </span>

                      <input
                        v-model.number="taskPreferenceForm.recommendationSensitivity"
                        type="range"
                        min="0"
                        max="100"
                        :disabled="isSavingTaskPreference"
                      />
                    </label>

                    <label class="settings-field">
                      <span>Interval minutes</span>

                      <input
                        v-model.number="taskPreferenceForm.recommendationIntervalMinutes"
                        type="number"
                        min="5"
                        max="1440"
                        :disabled="isSavingTaskPreference"
                      />
                    </label>

                    <label class="settings-switch-row">
                      <span>
                        <strong>Auto recommendation</strong>
                        <small>Cho phép hệ thống tự gợi ý task.</small>
                      </span>

                      <input
                        v-model="taskPreferenceForm.enableAutoRecommendation"
                        type="checkbox"
                        :disabled="isSavingTaskPreference"
                      />
                    </label>

                    <p
                      v-if="taskPreferenceError"
                      class="settings-inline-error"
                    >
                      {{ taskPreferenceError }}
                    </p>

                    <p
                      v-if="taskPreferenceSuccess"
                      class="settings-inline-success"
                    >
                      {{ taskPreferenceSuccess }}
                    </p>

                    <div class="settings-modal-actions">
                      <button
                        type="button"
                        class="settings-primary-action"
                        :disabled="isSavingTaskPreference"
                        @click="emit('saveTaskPreference')"
                      >
                        <span
                          v-if="isSavingTaskPreference"
                          class="settings-modal-spinner dark"
                        ></span>

                        <span>
                          {{ isSavingTaskPreference ? 'Saving...' : 'Save AI settings' }}
                        </span>
                      </button>
                    </div>
                  </div>
                </template>
              </section>

              <section
                v-else
                class="settings-modal-section"
              >
                <div class="settings-modal-card">
                  <div class="settings-card-head">
                    <div>
                      <strong>Password</strong>
                      <span>Cập nhật mật khẩu tài khoản hiện tại.</span>
                    </div>
                  </div>

                  <label class="settings-field">
                    <span>Current password</span>

                    <input
                      v-model="passwordForm.currentPassword"
                      type="password"
                      autocomplete="current-password"
                      :disabled="isChangingPassword"
                    />
                  </label>

                  <label class="settings-field">
                    <span>New password</span>

                    <input
                      v-model="passwordForm.newPassword"
                      type="password"
                      autocomplete="new-password"
                      :disabled="isChangingPassword"
                    />
                  </label>

                  <p
                    v-if="passwordSettingsError"
                    class="settings-inline-error"
                  >
                    {{ passwordSettingsError }}
                  </p>

                  <p
                    v-if="passwordSettingsSuccess"
                    class="settings-inline-success"
                  >
                    {{ passwordSettingsSuccess }}
                  </p>

                  <div class="settings-modal-actions">
                    <button
                      type="button"
                      class="settings-primary-action"
                      :disabled="isChangingPassword || !passwordForm.currentPassword || !passwordForm.newPassword"
                      @click="emit('changePassword')"
                    >
                      <span
                        v-if="isChangingPassword"
                        class="settings-modal-spinner dark"
                      ></span>

                      <span>
                        {{ isChangingPassword ? 'Updating...' : 'Change password' }}
                      </span>
                    </button>
                  </div>
                </div>
              </section>
            </div>
          </main>
        </section>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import type { Guid } from '@/api/models/common.model'
import type { UpdateUserTaskPreferenceRequest } from '@/api/models/recommendation.model'
import type { SidebarSettingsTab } from './useSidebarSettings'
import { normalizeImageUrl } from '@/utils/image-url.util'

const props = defineProps<{
  workspaceId: Guid | null

  profileForm: {
    fullName: string
    avatarUrl: string
  }

  passwordForm: {
    currentPassword: string
    newPassword: string
  }

  taskPreferenceForm: UpdateUserTaskPreferenceRequest

  preferredDayOptions: Array<{
    value: number
    label: string
    title: string
  }>

  isLoadingProfileSettings: boolean
  isSavingProfileSettings: boolean
  profileSettingsError: string | null
  profileSettingsSuccess: string | null

  isChangingPassword: boolean
  passwordSettingsError: string | null
  passwordSettingsSuccess: string | null

  isLoadingTaskPreference: boolean
  isSavingTaskPreference: boolean
  taskPreferenceError: string | null
  taskPreferenceSuccess: string | null
}>()

const emit = defineEmits<{
  close: []
  saveProfile: []
  changePassword: []
  togglePreferredDay: [day: number]
  saveTaskPreference: []
}>()

const activeTab = ref<SidebarSettingsTab>('profile')
const avatarPreviewFailed = ref(false)

const profileAvatarPreviewSrc = computed(() => {
  return normalizeImageUrl(props.profileForm.avatarUrl) ?? undefined
})

const canPreviewAvatar = computed(() => {
  return Boolean(profileAvatarPreviewSrc.value) && !avatarPreviewFailed.value
})

const profileInitial = computed(() => {
  const name = props.profileForm.fullName.trim()

  if (!name) return 'U'

  return name.charAt(0).toUpperCase()
})

const avatarPreviewHint = computed(() => {
  if (!profileAvatarPreviewSrc.value) {
      return 'Paste an image link to preview your avatar.'
  }

  if (avatarPreviewFailed.value) {
    return 'Không tải được ảnh. Hãy dùng link ảnh public/direct.'
  }

  return 'Image preview from your avatar URL.'
})

const activeTabEyebrow = computed(() => {
  if (activeTab.value === 'profile') return 'Account'
  if (activeTab.value === 'ai') return 'Recommendations'

  return 'Security'
})

const activeTabTitle = computed(() => {
  if (activeTab.value === 'profile') return 'Profile settings'
  if (activeTab.value === 'ai') return 'AI suggestion settings'

  return 'Security settings'
})

const activeTabDescription = computed(() => {
  if (activeTab.value === 'profile') {
    return 'Manage your display name and avatar for the current workspace experience.'
  }

  if (activeTab.value === 'ai') {
    return 'Tune how task recommendations are generated for the selected workspace.'
  }

  return 'Update password and keep your account protected.'
})

watch(
  () => props.profileForm.avatarUrl,
  () => {
    avatarPreviewFailed.value = false
  }
)

function handleKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape') {
    emit('close')
  }
}

onMounted(() => {
  document.body.classList.add('settings-modal-scroll-lock')
  window.addEventListener('keydown', handleKeydown)
})

onBeforeUnmount(() => {
  document.body.classList.remove('settings-modal-scroll-lock')
  window.removeEventListener('keydown', handleKeydown)
})
</script>

<style scoped>
:global(.settings-modal-scroll-lock) {
  overflow: hidden;
}

.settings-modal-layer {
  position: fixed;
  inset: 0;
  z-index: 2300;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 28px;
  color: #f4f4f5;
}

.settings-modal-backdrop {
  position: absolute;
  inset: 0;
  background:
    radial-gradient(circle at top left, rgba(255, 255, 255, 0.08), transparent 34%),
    rgba(0, 0, 0, 0.58);
  backdrop-filter: blur(10px);
}

.settings-modal-shell {
  position: relative;
  z-index: 1;
  width: min(1040px, calc(100vw - 42px));
  height: min(720px, calc(100vh - 42px));
  display: grid;
  grid-template-columns: 280px minmax(0, 1fr);
  overflow: hidden;
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 18px;
  background: #101010;
  box-shadow:
    0 36px 120px rgba(0, 0, 0, 0.58),
    0 18px 52px rgba(0, 0, 0, 0.42),
    inset 0 1px 0 rgba(255, 255, 255, 0.06);
}

.settings-modal-sidebar {
  min-width: 0;
  display: flex;
  flex-direction: column;
  padding: 14px;
  border-right: 1px solid #262626;
  background:
    radial-gradient(circle at top left, rgba(255, 255, 255, 0.06), transparent 42%),
    #151515;
}

.settings-modal-brand {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 8px 14px;
  border-bottom: 1px solid #262626;
}

.settings-modal-brand-icon {
  width: 34px;
  height: 34px;
  border: 1px solid #333333;
  border-radius: 10px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: #f5f5f5;
  background: #202020;
}

.settings-modal-brand-copy {
  min-width: 0;
  display: flex;
  flex-direction: column;
  line-height: 1.2;
}

.settings-modal-brand-copy span {
  color: #777777;
  font-size: 11px;
  font-weight: 800;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.settings-modal-brand-copy strong {
  color: #f5f5f5;
  font-size: 14px;
  font-weight: 850;
}

.settings-modal-tabs {
  display: flex;
  flex-direction: column;
  gap: 5px;
  padding: 14px 0;
}

.settings-modal-tab {
  width: 100%;
  min-height: 58px;
  border: 1px solid transparent;
  border-radius: 12px;
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 9px;
  color: #a3a3a3;
  background: transparent;
  text-align: left;
  transition:
    color 140ms ease,
    background-color 140ms ease,
    border-color 140ms ease;
}

.settings-modal-tab:hover,
.settings-modal-tab.active {
  color: #f5f5f5;
  border-color: #333333;
  background: #202020;
}

.settings-modal-tab.active {
  background:
    linear-gradient(135deg, #242424, #1a1a1a);
}

.settings-modal-tab-icon {
  width: 34px;
  height: 34px;
  border: 1px solid #2f2f2f;
  border-radius: 10px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  color: #d4d4d4;
  background: #101010;
}

.settings-modal-tab-copy {
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.settings-modal-tab-copy strong {
  color: inherit;
  font-size: 13px;
  font-weight: 850;
}

.settings-modal-tab-copy small {
  overflow: hidden;
  color: #737373;
  font-size: 11px;
  line-height: 1.25;
  white-space: nowrap;
  text-overflow: ellipsis;
}

.settings-modal-sidebar-foot {
  margin-top: auto;
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 8px 2px;
  color: #737373;
  font-size: 11px;
}

.settings-modal-foot-dot {
  width: 6px;
  height: 6px;
  border-radius: 999px;
  background: #a3a3a3;
}

.settings-modal-main {
  min-width: 0;
  min-height: 0;
  display: flex;
  flex-direction: column;
  background:
    radial-gradient(circle at top right, rgba(255, 255, 255, 0.045), transparent 34%),
    #101010;
}

.settings-modal-header {
  min-height: 120px;
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 18px;
  padding: 24px 28px 20px;
  border-bottom: 1px solid #262626;
}

.settings-modal-eyebrow {
  margin: 0 0 8px;
  color: #737373;
  font-size: 11px;
  font-weight: 850;
  letter-spacing: 0.1em;
  text-transform: uppercase;
}

.settings-modal-header h2 {
  margin: 0;
  color: #f5f5f5;
  font-size: clamp(1.55rem, 2.2vw, 2.15rem);
  font-weight: 850;
  letter-spacing: -0.045em;
  line-height: 1.05;
}

.settings-modal-description {
  max-width: 580px;
  margin: 8px 0 0;
  color: #8a8a8a;
  font-size: 13px;
  line-height: 1.55;
}

.settings-modal-close {
  width: 34px;
  height: 34px;
  border: 1px solid transparent;
  border-radius: 10px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: #a3a3a3;
  background: transparent;
  transition:
    color 140ms ease,
    background-color 140ms ease,
    border-color 140ms ease;
}

.settings-modal-close:hover {
  color: #f5f5f5;
  border-color: #333333;
  background: #202020;
}

.settings-modal-content {
  min-height: 0;
  flex: 1;
  overflow-y: auto;
  padding: 24px 28px 30px;
}

.settings-modal-section {
  max-width: 720px;
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.settings-modal-card {
  border: 1px solid #262626;
  border-radius: 16px;
  padding: 16px;
  background:
    linear-gradient(135deg, rgba(255, 255, 255, 0.035), transparent),
    #151515;
}

.profile-card {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 14px;
}

.settings-profile-preview {
  min-width: 0;
  display: flex;
  align-items: center;
  gap: 13px;
}

.settings-profile-avatar {
  width: 64px;
  height: 64px;
  overflow: hidden;
  border: 1px solid #333333;
  border-radius: 18px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  color: #f5f5f5;
  background:
    radial-gradient(circle at top left, rgba(255, 255, 255, 0.12), transparent 42%),
    #202020;
  font-size: 24px;
  font-weight: 900;
}

.settings-profile-avatar img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.settings-profile-preview-copy {
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.settings-profile-preview-copy strong {
  overflow: hidden;
  color: #f5f5f5;
  font-size: 16px;
  font-weight: 850;
  white-space: nowrap;
  text-overflow: ellipsis;
}

.settings-profile-preview-copy span {
  color: #737373;
  font-size: 12px;
  line-height: 1.45;
}

.settings-avatar-open-link {
  flex-shrink: 0;
  border: 1px solid #2f2f2f;
  border-radius: 10px;
  display: inline-flex;
  align-items: center;
  gap: 7px;
  padding: 8px 10px;
  color: #d4d4d4;
  background: #101010;
  font-size: 12px;
  font-weight: 800;
  text-decoration: none;
}

.settings-avatar-open-link:hover {
  color: #f5f5f5;
  border-color: #444444;
  background: #202020;
}

.settings-card-head {
  margin-bottom: 14px;
}

.settings-card-head div {
  display: flex;
  flex-direction: column;
  gap: 3px;
}

.settings-card-head strong {
  color: #f5f5f5;
  font-size: 14px;
  font-weight: 850;
}

.settings-card-head span {
  color: #737373;
  font-size: 12px;
  line-height: 1.45;
}

.settings-field {
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 7px;
  margin-bottom: 13px;
}

.settings-field span {
  color: #a3a3a3;
  font-size: 12px;
  font-weight: 800;
}

.settings-field input,
.settings-field textarea,
.settings-field select {
  width: 100%;
  min-height: 38px;
  border: 1px solid #2f2f2f;
  border-radius: 10px;
  outline: 0;
  padding: 8px 10px;
  color: #f5f5f5;
  background: #101010;
  font-size: 13px;
  transition:
    border-color 140ms ease,
    background-color 140ms ease,
    box-shadow 140ms ease;
}

.settings-field input:hover:not(:disabled),
.settings-field textarea:hover:not(:disabled),
.settings-field select:hover:not(:disabled),
.settings-field input:focus:not(:disabled),
.settings-field textarea:focus:not(:disabled),
.settings-field select:focus:not(:disabled) {
  border-color: #444444;
  background: #171717;
}

.settings-field input:focus:not(:disabled),
.settings-field textarea:focus:not(:disabled),
.settings-field select:focus:not(:disabled) {
  box-shadow: 0 0 0 3px rgba(255, 255, 255, 0.055);
}

.settings-field-help {
  margin: -4px 0 13px;
  color: #737373;
  font-size: 12px;
  line-height: 1.5;
}

.settings-field input:disabled,
.settings-field textarea:disabled,
.settings-field select:disabled,
.settings-day-grid button:disabled,
.settings-switch-row input:disabled,
.settings-primary-action:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

.settings-field input[type='range'] {
  min-height: auto;
  padding: 0;
  accent-color: #f5f5f5;
}

.settings-field-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.settings-day-grid {
  display: grid;
  grid-template-columns: repeat(7, minmax(0, 1fr));
  gap: 7px;
}

.settings-day-grid button {
  min-height: 34px;
  border: 1px solid #2f2f2f;
  border-radius: 10px;
  color: #a3a3a3;
  background: #101010;
  font-size: 12px;
  font-weight: 850;
  transition:
    color 140ms ease,
    background-color 140ms ease,
    border-color 140ms ease;
}

.settings-day-grid button:hover:not(:disabled),
.settings-day-grid button.active {
  color: #f5f5f5;
  border-color: #444444;
  background: #242424;
}

.settings-switch-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  margin: 4px 0 13px;
  padding: 12px;
  border: 1px solid #2f2f2f;
  border-radius: 13px;
  background: #101010;
}

.settings-switch-row span {
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 3px;
}

.settings-switch-row strong {
  color: #f5f5f5;
  font-size: 13px;
  font-weight: 850;
}

.settings-switch-row small {
  color: #737373;
  font-size: 12px;
}

.settings-switch-row input {
  width: 18px;
  height: 18px;
  flex-shrink: 0;
  accent-color: #f5f5f5;
}

.settings-modal-actions {
  display: flex;
  justify-content: flex-end;
  margin-top: 14px;
}

.settings-primary-action {
  min-height: 38px;
  border: 1px solid #f5f5f5;
  border-radius: 10px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 8px 13px;
  color: #101010;
  background: #f5f5f5;
  font-size: 13px;
  font-weight: 900;
  transition:
    background-color 140ms ease,
    border-color 140ms ease,
    transform 140ms ease;
}

.settings-primary-action:hover:not(:disabled) {
  background: #ffffff;
  border-color: #ffffff;
  transform: translateY(-1px);
}

.settings-inline-error,
.settings-inline-success {
  margin: 0;
  border-radius: 10px;
  padding: 9px 11px;
  font-size: 12px;
  line-height: 1.45;
}

.settings-inline-error {
  border: 1px solid rgba(248, 113, 113, 0.24);
  color: #fca5a5;
  background: rgba(127, 29, 29, 0.18);
}

.settings-inline-success {
  border: 1px solid rgba(74, 222, 128, 0.2);
  color: #86efac;
  background: rgba(20, 83, 45, 0.14);
}

.settings-modal-empty {
  min-height: 170px;
  border: 1px solid #262626;
  border-radius: 16px;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 10px;
  color: #a3a3a3;
  background: #151515;
  font-size: 13px;
}

.settings-modal-spinner {
  width: 15px;
  height: 15px;
  border-radius: 999px;
  border: 2px solid #333333;
  border-top-color: #f5f5f5;
  animation: settings-spin 0.75s linear infinite;
}

.settings-modal-spinner.dark {
  width: 13px;
  height: 13px;
  border-color: rgba(16, 16, 16, 0.25);
  border-top-color: #101010;
}

.settings-modal-enter-active,
.settings-modal-leave-active {
  transition: opacity 180ms ease;
}

.settings-modal-enter-active .settings-modal-shell,
.settings-modal-leave-active .settings-modal-shell {
  transition:
    transform 220ms cubic-bezier(0.16, 1, 0.3, 1),
    opacity 180ms ease;
}

.settings-modal-enter-from,
.settings-modal-leave-to {
  opacity: 0;
}

.settings-modal-enter-from .settings-modal-shell,
.settings-modal-leave-to .settings-modal-shell {
  opacity: 0;
  transform: translateY(10px) scale(0.975);
}

@keyframes settings-spin {
  to {
    transform: rotate(360deg);
  }
}

@media (max-width: 860px) {
  .settings-modal-layer {
    padding: 14px;
  }

  .settings-modal-shell {
    width: calc(100vw - 28px);
    height: calc(100vh - 28px);
    grid-template-columns: 1fr;
  }

  .settings-modal-sidebar {
    border-right: 0;
    border-bottom: 1px solid #262626;
  }

  .settings-modal-tabs {
    display: grid;
    grid-template-columns: repeat(3, minmax(0, 1fr));
    padding-bottom: 0;
  }

  .settings-modal-tab-copy small,
  .settings-modal-sidebar-foot {
    display: none;
  }

  .settings-modal-header {
    min-height: auto;
    padding: 20px;
  }

  .settings-modal-content {
    padding: 20px;
  }

  .settings-field-grid {
    grid-template-columns: 1fr;
  }

  .settings-day-grid {
    grid-template-columns: repeat(4, minmax(0, 1fr));
  }

  .profile-card {
    align-items: flex-start;
    flex-direction: column;
  }
}
</style>