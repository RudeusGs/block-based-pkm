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
            <div class="settings-modal-title-block">
              <strong>Settings</strong>
              <span>Workspace preferences</span>
            </div>

            <nav class="settings-modal-tabs" aria-label="Settings tabs">
              <button
                type="button"
                class="settings-modal-tab"
                :class="{ active: activeTab === 'profile' }"
                @click="activeTab = 'profile'"
              >
                <span>Profile</span>
                <small>Name and avatar</small>
              </button>

              <button
                type="button"
                class="settings-modal-tab"
                :class="{ active: activeTab === 'ai' }"
                @click="activeTab = 'ai'"
              >
                <span>AI suggestions</span>
                <small>Recommendation rules</small>
              </button>

              <button
                type="button"
                class="settings-modal-tab"
                :class="{ active: activeTab === 'security' }"
                @click="activeTab = 'security'"
              >
                <span>Security</span>
                <small>Password</small>
              </button>
            </nav>
          </aside>

          <main class="settings-modal-main">
            <header class="settings-modal-header">
              <div>
                <h2 id="settings-modal-title">
                  {{ activeTabTitle }}
                </h2>

                <p>
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
                  <span>Đang tải hồ sơ...</span>
                </div>

                <template v-else>
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

                  <div class="settings-form-group">
                    <label class="settings-field">
                      <span>Full name</span>

                      <input
                        v-model="profileForm.fullName"
                        type="text"
                        maxlength="100"
                        placeholder="Tên hiển thị"
                        :disabled="isSavingProfileSettings || isUploadingAvatarImage"
                      />
                    </label>

                    <div class="settings-field">
                      <span>Avatar image</span>

                      <div class="settings-upload-control">
                        <input
                          ref="avatarFileInputRef"
                          class="settings-file-input"
                          type="file"
                          accept="image/*"
                          :disabled="isSavingProfileSettings || isUploadingAvatarImage"
                          @change="handleAvatarFileChange"
                        />

                        <button
                          type="button"
                          class="settings-upload-btn"
                          :disabled="isSavingProfileSettings || isUploadingAvatarImage"
                          @click="avatarFileInputRef?.click()"
                        >
                          <span
                            v-if="isUploadingAvatarImage"
                            class="settings-modal-spinner"
                          ></span>

                          <i
                            v-else
                            class="bi bi-cloud-arrow-up"
                          ></i>

                          <span>
                            {{ isUploadingAvatarImage ? 'Uploading...' : 'Upload image' }}
                          </span>
                        </button>

                        <small>
                          JPG, PNG, WebP hoặc GIF.
                        </small>
                      </div>
                    </div>
                  </div>

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
                      :disabled="isSavingProfileSettings || isUploadingAvatarImage || !profileForm.fullName.trim()"
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
                  <span>Chọn workspace để cấu hình AI.</span>
                </div>

                <div
                  v-else-if="isLoadingTaskPreference"
                  class="settings-modal-empty"
                >
                  <span class="settings-modal-spinner"></span>
                  <span>Đang tải cấu hình AI...</span>
                </div>

                <template v-else>
                  <div class="settings-form-group">
                    <h3>Working window</h3>

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
                          min="0"
                          max="23"
                          :disabled="isSavingTaskPreference"
                        />
                      </label>
                    </div>

                    <div class="settings-field">
                      <span>Preferred days</span>

                      <div class="settings-day-row">
                        <button
                          v-for="day in preferredDayOptions"
                          :key="day.value"
                          type="button"
                          :class="{
                            active: taskPreferenceForm.preferredDaysOfWeek.includes(day.value),
                          }"
                          :title="day.title"
                          :disabled="isSavingTaskPreference"
                          @click="emit('togglePreferredDay', day.value)"
                        >
                          {{ day.label }}
                        </button>
                      </div>
                    </div>
                  </div>

                  <div class="settings-form-group">
                    <h3>Recommendation rules</h3>

                    <div class="settings-field-grid">
                      <label class="settings-field">
                        <span>Max per session</span>

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
                        Sensitivity: {{ taskPreferenceForm.recommendationSensitivity }}
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
                  </div>

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
                </template>
              </section>

              <section
                v-else
                class="settings-modal-section"
              >
                <div class="settings-form-group">
                  <h3>Password</h3>

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
                </div>

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
  isUploadingAvatarImage: boolean
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
  uploadAvatarImage: [file: File]
  changePassword: []
  togglePreferredDay: [day: number]
  saveTaskPreference: []
}>()

const activeTab = ref<SidebarSettingsTab>('profile')
const avatarPreviewFailed = ref(false)
const avatarFileInputRef = ref<HTMLInputElement | null>(null)

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
  if (!props.profileForm.avatarUrl.trim()) {
    return 'Chưa có avatar.'
  }

  if (!profileAvatarPreviewSrc.value) {
    return 'Không nhận diện được ảnh.'
  }

  if (avatarPreviewFailed.value) {
    return 'Không load được ảnh.'
  }

  return 'Avatar preview.'
})

const activeTabTitle = computed(() => {
  if (activeTab.value === 'profile') return 'Profile'
  if (activeTab.value === 'ai') return 'AI suggestions'

  return 'Security'
})

const activeTabDescription = computed(() => {
  if (activeTab.value === 'profile') {
    return 'Manage your display name and avatar.'
  }

  if (activeTab.value === 'ai') {
    return 'Tune how task recommendations are generated for this workspace.'
  }

  return 'Update your account password.'
})

watch(
  () => props.profileForm.avatarUrl,
  () => {
    avatarPreviewFailed.value = false
  }
)

function handleAvatarFileChange(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]

  if (file) {
    emit('uploadAvatarImage', file)
  }

  input.value = ''
}

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
  color: #f1f1f1;
}

.settings-modal-backdrop {
  position: absolute;
  inset: 0;
  background: rgba(0, 0, 0, 0.48);
}

.settings-modal-shell {
  position: relative;
  z-index: 1;
  width: min(900px, calc(100vw - 40px));
  height: min(680px, calc(100vh - 40px));
  display: grid;
  grid-template-columns: 220px minmax(0, 1fr);
  overflow: hidden;
  border: 1px solid #2b2b2b;
  border-radius: 12px;
  background: #191919;
  box-shadow: 0 24px 80px rgba(0, 0, 0, 0.5);
}

.settings-modal-sidebar {
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding: 14px 10px;
  border-right: 1px solid #2b2b2b;
  background: #151515;
}

.settings-modal-title-block {
  padding: 3px 6px 8px;
}

.settings-modal-title-block strong {
  display: block;
  color: #f1f1f1;
  font-size: 14px;
  font-weight: 700;
}

.settings-modal-title-block span {
  display: block;
  margin-top: 2px;
  color: #8a8a8a;
  font-size: 12px;
}

.settings-modal-tabs {
  display: flex;
  flex-direction: column;
  gap: 1px;
}

.settings-modal-tab {
  width: 100%;
  border: 0;
  border-radius: 6px;
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 1px;
  padding: 7px 8px;
  color: #a3a3a3;
  background: transparent;
  text-align: left;
}

.settings-modal-tab:hover,
.settings-modal-tab.active {
  color: #f1f1f1;
  background: #242424;
}

.settings-modal-tab span {
  font-size: 13px;
  font-weight: 650;
}

.settings-modal-tab small {
  color: #737373;
  font-size: 11px;
}

.settings-modal-main {
  min-width: 0;
  display: flex;
  flex-direction: column;
  background: #191919;
}

.settings-modal-header {
  min-height: 86px;
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 18px;
  padding: 22px 28px 16px;
  border-bottom: 1px solid #2b2b2b;
}

.settings-modal-header h2 {
  margin: 0;
  color: #f1f1f1;
  font-size: 22px;
  font-weight: 720;
  letter-spacing: -0.03em;
}

.settings-modal-header p {
  margin: 5px 0 0;
  color: #8a8a8a;
  font-size: 13px;
  line-height: 1.45;
}

.settings-modal-close {
  width: 28px;
  height: 28px;
  border: 0;
  border-radius: 6px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  color: #8a8a8a;
  background: transparent;
}

.settings-modal-close:hover {
  color: #f1f1f1;
  background: #242424;
}

.settings-modal-close i {
  font-size: 14px;
}

.settings-modal-content {
  min-height: 0;
  flex: 1;
  overflow-y: auto;
  padding: 24px 28px 30px;
}

.settings-modal-content::-webkit-scrollbar {
  width: 10px;
}

.settings-modal-content::-webkit-scrollbar-track {
  background: transparent;
}

.settings-modal-content::-webkit-scrollbar-thumb {
  border: 3px solid transparent;
  border-radius: 999px;
  background: #3a3a3a;
  background-clip: content-box;
}

.settings-modal-section {
  max-width: 620px;
  display: flex;
  flex-direction: column;
  gap: 18px;
}

.settings-profile-preview {
  display: flex;
  align-items: center;
  gap: 12px;
  padding-bottom: 16px;
  border-bottom: 1px solid #2b2b2b;
}

.settings-profile-avatar {
  width: 52px;
  height: 52px;
  overflow: hidden;
  border-radius: 8px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  color: #f1f1f1;
  background: #2a2a2a;
  font-size: 20px;
  font-weight: 750;
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
  gap: 2px;
}

.settings-profile-preview-copy strong {
  overflow: hidden;
  color: #f1f1f1;
  font-size: 15px;
  font-weight: 700;
  white-space: nowrap;
  text-overflow: ellipsis;
}

.settings-profile-preview-copy span,
.settings-field-help {
  color: #8a8a8a;
  font-size: 12px;
  line-height: 1.45;
}


.settings-upload-control {
  min-width: 0;
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
}

.settings-file-input {
  display: none;
}

.settings-upload-btn {
  min-height: 32px;
  border: 1px solid #303030;
  border-radius: 6px;
  display: inline-flex;
  align-items: center;
  gap: 7px;
  padding: 5px 10px;
  color: #e7e7e7;
  background: #202020;
  font-size: 12px;
  font-weight: 650;
  transition:
    background 0.12s ease,
    border-color 0.12s ease,
    color 0.12s ease,
    opacity 0.12s ease;
}

.settings-upload-btn:hover:not(:disabled) {
  color: #ffffff;
  background: #272727;
  border-color: #3a3a3a;
}

.settings-upload-btn:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

.settings-upload-btn i {
  font-size: 14px;
}

.settings-upload-control small {
  color: #737373;
  font-size: 12px;
  line-height: 1.45;
}

.settings-form-group {
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding-bottom: 18px;
  border-bottom: 1px solid #2b2b2b;
}

.settings-form-group:last-child {
  border-bottom: 0;
}

.settings-form-group h3 {
  margin: 0 0 2px;
  color: #f1f1f1;
  font-size: 13px;
  font-weight: 700;
}

.settings-field {
  min-width: 0;
  display: grid;
  grid-template-columns: 160px minmax(0, 1fr);
  align-items: center;
  gap: 16px;
}

.settings-field > span {
  color: #a3a3a3;
  font-size: 12px;
  font-weight: 600;
}

.settings-field input,
.settings-field select {
  width: 100%;
  min-height: 32px;
  border: 1px solid #303030;
  border-radius: 6px;
  outline: 0;
  padding: 5px 8px;
  color: #e7e7e7;
  background: #202020;
  font-size: 13px;
}

.settings-field input:hover:not(:disabled),
.settings-field select:hover:not(:disabled),
.settings-field input:focus:not(:disabled),
.settings-field select:focus:not(:disabled) {
  border-color: #4a4a4a;
  background: #242424;
}

.settings-field input[type='range'] {
  padding: 0;
  border: 0;
  background: transparent;
}

.settings-field-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.settings-field-grid .settings-field {
  grid-template-columns: 1fr;
  gap: 6px;
}

.settings-day-row {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 4px;
}

.settings-day-row button {
  min-width: 42px;
  height: 28px;
  border: 0;
  border-radius: 6px;
  color: #a3a3a3;
  background: transparent;
  font-size: 12px;
}

.settings-day-row button:hover:not(:disabled),
.settings-day-row button.active {
  color: #f1f1f1;
  background: #242424;
}

.settings-switch-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 2px 0;
}

.settings-switch-row span {
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.settings-switch-row strong {
  color: #f1f1f1;
  font-size: 13px;
  font-weight: 650;
}

.settings-switch-row small {
  color: #8a8a8a;
  font-size: 12px;
}

.settings-switch-row input {
  width: 16px;
  height: 16px;
  flex-shrink: 0;
}

.settings-modal-actions {
  display: flex;
  justify-content: flex-end;
}

.settings-primary-action {
  min-height: 32px;
  border: 0;
  border-radius: 6px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 6px 12px;
  color: #111;
  background: #f1f1f1;
  font-size: 13px;
  font-weight: 650;
}

.settings-primary-action:hover:not(:disabled) {
  background: #fff;
}

.settings-primary-action:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.settings-inline-error,
.settings-inline-success {
  margin: 0;
  font-size: 12px;
  line-height: 1.45;
}

.settings-inline-error {
  color: #f2a6a6;
}

.settings-inline-success {
  color: #75b798;
}

.settings-modal-empty {
  min-height: 120px;
  display: flex;
  align-items: center;
  gap: 8px;
  color: #8a8a8a;
  font-size: 13px;
}

.settings-modal-spinner {
  width: 14px;
  height: 14px;
  border-radius: 999px;
  border: 2px solid #3a3a3a;
  border-top-color: #f1f1f1;
  animation: settings-spin 0.75s linear infinite;
}

.settings-modal-spinner.dark {
  width: 12px;
  height: 12px;
  border-color: rgba(17, 17, 17, 0.25);
  border-top-color: #111;
}

.settings-modal-enter-active,
.settings-modal-leave-active {
  transition: opacity 0.16s ease;
}

.settings-modal-enter-from,
.settings-modal-leave-to {
  opacity: 0;
}

.settings-modal-enter-active .settings-modal-shell,
.settings-modal-leave-active .settings-modal-shell {
  transition: transform 0.18s ease;
}

.settings-modal-enter-from .settings-modal-shell,
.settings-modal-leave-to .settings-modal-shell {
  transform: translateY(8px) scale(0.99);
}

.settings-modal-tab:focus-visible,
.settings-modal-close:focus-visible,
.settings-primary-action:focus-visible,
.settings-day-row button:focus-visible,
.settings-field input:focus-visible,
.settings-field select:focus-visible {
  outline: 2px solid #525252;
  outline-offset: 2px;
}

@keyframes settings-spin {
  to {
    transform: rotate(360deg);
  }
}

@media (max-width: 760px) {
  .settings-modal-layer {
    padding: 0;
  }

  .settings-modal-shell {
    width: 100vw;
    height: 100vh;
    border-radius: 0;
    grid-template-columns: 1fr;
  }

  .settings-modal-sidebar {
    border-right: 0;
    border-bottom: 1px solid #2b2b2b;
  }

  .settings-modal-tabs {
    flex-direction: row;
    overflow-x: auto;
  }

  .settings-modal-tab {
    min-width: 140px;
  }

  .settings-field,
  .settings-field-grid {
    grid-template-columns: 1fr;
  }

  .settings-modal-header,
  .settings-modal-content {
    padding-left: 20px;
    padding-right: 20px;
  }
}

@media (prefers-reduced-motion: reduce) {
  .settings-modal-enter-active,
  .settings-modal-leave-active,
  .settings-modal-enter-active .settings-modal-shell,
  .settings-modal-leave-active .settings-modal-shell,
  .settings-modal-spinner {
    transition: none;
    animation: none;
  }
}


/* Image upload display fix: preview avatar without stretching and keep the full image visible. */
.settings-profile-avatar {
  padding: 2px;
  border: 1px solid #343434;
  border-radius: 999px;
  background: #171717;
}

.settings-profile-avatar img {
  display: block;
  width: 100%;
  height: 100%;
  object-fit: contain;
  object-position: center center;
  border-radius: inherit;
  background: #171717;
}

</style>


