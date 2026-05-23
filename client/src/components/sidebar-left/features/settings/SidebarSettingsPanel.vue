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
              <span>Tài khoản, AI và bảo mật</span>
            </div>

            <nav class="settings-modal-tabs" aria-label="Settings tabs">
              <button
                type="button"
                class="settings-modal-tab"
                :class="{ active: activeTab === 'profile' }"
                @click="activeTab = 'profile'"
              >
                <i class="bi bi-person-circle"></i>
                <span>Profile</span>
                <small>Tên hiển thị & avatar</small>
              </button>

              <button
                type="button"
                class="settings-modal-tab"
                :class="{ active: activeTab === 'ai' }"
                @click="activeTab = 'ai'"
              >
                <i class="bi bi-stars"></i>
                <span>AI Prioritizer</span>
                <small>Luật gợi ý task</small>
              </button>

              <button
                type="button"
                class="settings-modal-tab"
                :class="{ active: activeTab === 'security' }"
                @click="activeTab = 'security'"
              >
                <i class="bi bi-shield-lock"></i>
                <span>Security</span>
                <small>Mật khẩu</small>
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
                class="settings-modal-section ai-settings-section"
              >
                <div
                  v-if="!workspaceId"
                  class="settings-modal-empty ai-empty"
                >
                  <div class="settings-empty-icon">
                    <i class="bi bi-stars"></i>
                  </div>
                  <div>
                    <strong>Chọn workspace trước đã bro</strong>
                    <span>AI settings được lưu riêng cho từng workspace để gợi ý không bị lẫn ngữ cảnh.</span>
                  </div>
                </div>

                <div
                  v-else-if="isLoadingTaskPreference"
                  class="settings-modal-empty"
                >
                  <span class="settings-modal-spinner"></span>
                  <span>Đang tải cấu hình AI...</span>
                </div>

                <template v-else>
                  <div class="settings-ai-hero">
                    <div class="settings-ai-hero-main">
                      <span class="settings-ai-hero-icon">
                        <i class="bi bi-stars"></i>
                      </span>

                      <div>
                        <div class="settings-ai-title-row">
                          <strong>AI Prioritizer Engine</strong>
                          <span
                            class="settings-ai-status"
                            :class="aiStatusClass"
                          >
                            {{ aiStatusLabel }}
                          </span>
                        </div>

                        <p>
                          {{ aiStatusDescription }}
                        </p>
                      </div>
                    </div>

                    <div class="settings-ai-summary-grid">
                      <span>
                        <strong>{{ taskPreferenceForm.maxRecommendationsPerSession }}</strong>
                        gợi ý/lần
                      </span>
                      <span>
                        <strong>{{ priorityDisplayLabel }}</strong>
                        trở lên
                      </span>
                      <span>
                        <strong>{{ taskPreferenceForm.recommendationSensitivity }}</strong>
                        độ lọc
                      </span>
                      <span>
                        <strong>{{ taskPreferenceForm.recommendationIntervalMinutes }}m</strong>
                        nghỉ auto
                      </span>
                    </div>
                  </div>

                  <div class="settings-form-group settings-ai-presets-group">
                    <div class="settings-group-heading">
                      <div>
                        <h3>Quick setup</h3>
                        <p>Chọn preset để AI bớt random, sau đó bấm Save để lưu vào backend.</p>
                      </div>

                      <button
                        type="button"
                        class="settings-soft-action"
                        :disabled="isSavingTaskPreference"
                        @click="emit('resetTaskPreference')"
                      >
                        Reset default
                      </button>
                    </div>

                    <div class="settings-ai-preset-grid">
                      <button
                        v-for="preset in aiPresetOptions"
                        :key="preset.key"
                        type="button"
                        class="settings-ai-preset-card"
                        :class="{ active: isPresetActive(preset.key) }"
                        :disabled="isSavingTaskPreference"
                        @click="emit('applyAiPreset', preset.key)"
                      >
                        <i :class="['bi', preset.icon]"></i>
                        <span>
                          <strong>{{ preset.label }}</strong>
                          <small>{{ preset.description }}</small>
                        </span>
                      </button>
                    </div>
                  </div>

                  <div class="settings-form-group">
                    <div class="settings-group-heading">
                      <div>
                        <h3>Auto schedule</h3>
                        <p>Chỉ áp dụng cho auto recommendation. Nút “Phân tích task” vẫn chạy thủ công khi bạn bấm.</p>
                      </div>
                    </div>

                    <label class="settings-switch-row elevated">
                      <span>
                        <strong>Auto recommendation</strong>
                        <small>Bật để hệ thống tự đề xuất theo giờ/ngày và interval bên dưới.</small>
                      </span>

                      <input
                        v-model="taskPreferenceForm.enableAutoRecommendation"
                        type="checkbox"
                        :disabled="isSavingTaskPreference"
                      />
                    </label>

                    <div class="settings-field-grid">
                      <label class="settings-field compact">
                        <span>Start hour</span>

                        <input
                          v-model.number="taskPreferenceForm.workDayStartHour"
                          type="number"
                          min="0"
                          max="23"
                          :disabled="isSavingTaskPreference"
                        />
                      </label>

                      <label class="settings-field compact">
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

                    <div class="settings-field settings-field-top">
                      <span>Preferred days</span>

                      <div>
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

                        <small class="settings-field-note">
                          Auto chỉ chạy vào những ngày này. Manual analyze không bị giới hạn.
                        </small>
                      </div>
                    </div>

                    <label class="settings-field">
                      <span>Auto interval</span>

                      <div>
                        <input
                          v-model.number="taskPreferenceForm.recommendationIntervalMinutes"
                          type="number"
                          min="1"
                          max="1440"
                          :disabled="isSavingTaskPreference"
                        />
                        <small class="settings-field-note">
                          Khoảng nghỉ để tránh AI spam gợi ý liên tục.
                        </small>
                      </div>
                    </label>
                  </div>

                  <div class="settings-form-group">
                    <div class="settings-group-heading">
                      <div>
                        <h3>Recommendation quality</h3>
                        <p>Những rule này đang được backend dùng khi generate task recommendations.</p>
                      </div>
                    </div>

                    <div class="settings-field-grid">
                      <label class="settings-field compact">
                        <span>Max per run</span>

                        <input
                          v-model.number="taskPreferenceForm.maxRecommendationsPerSession"
                          type="number"
                          min="1"
                          max="20"
                          :disabled="isSavingTaskPreference"
                        />
                      </label>

                      <label class="settings-field compact">
                        <span>Min priority</span>

                        <select
                          v-model="taskPreferenceForm.minPriorityForRecommendation"
                          :disabled="isSavingTaskPreference"
                        >
                          <option value="low">Low - rộng hơn</option>
                          <option value="medium">Medium - cân bằng</option>
                          <option value="high">High - nghiêm ngặt</option>
                        </select>
                      </label>
                    </div>

                    <label class="settings-field settings-field-top">
                      <span>Quality threshold</span>

                      <div>
                        <div class="settings-range-head">
                          <strong>{{ sensitivityLabel }}</strong>
                          <small>{{ taskPreferenceForm.recommendationSensitivity }}/100</small>
                        </div>

                        <input
                          v-model.number="taskPreferenceForm.recommendationSensitivity"
                          type="range"
                          min="0"
                          max="100"
                          :disabled="isSavingTaskPreference"
                        />

                        <small class="settings-field-note">
                          Thấp = nhiều gợi ý hơn. Cao = ít hơn nhưng chắc kèo hơn.
                        </small>
                      </div>
                    </label>
                  </div>

                  <div class="settings-ai-explain-card">
                    <i class="bi bi-info-circle"></i>
                    <span>
                      AI sẽ lọc task theo priority, deadline, assignee, trạng thái đang làm, duplicate title và độ rõ của tiêu đề.
                    </span>
                  </div>

                  <p
                    v-if="aiValidationMessage"
                    class="settings-inline-error"
                  >
                    {{ aiValidationMessage }}
                  </p>

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

                  <div class="settings-modal-actions sticky-actions">
                    <button
                      type="button"
                      class="settings-primary-action"
                      :disabled="isSavingTaskPreference || Boolean(aiValidationMessage)"
                      @click="emit('saveTaskPreference')"
                    >
                      <span
                        v-if="isSavingTaskPreference"
                        class="settings-modal-spinner dark"
                      ></span>

                      <i
                        v-else
                        class="bi bi-check2"
                      ></i>

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
import {
  aiPreferencePresets,
  type AiPreferencePresetKey,
  type SidebarSettingsTab,
} from './useSidebarSettings'
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
  applyAiPreset: [presetKey: AiPreferencePresetKey]
  resetTaskPreference: []
}>()

const activeTab = ref<SidebarSettingsTab>('profile')
const avatarPreviewFailed = ref(false)
const avatarFileInputRef = ref<HTMLInputElement | null>(null)
const aiPresetOptions = aiPreferencePresets

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
  if (activeTab.value === 'ai') return 'AI Prioritizer'

  return 'Security'
})

const activeTabDescription = computed(() => {
  if (activeTab.value === 'profile') {
    return 'Quản lý tên hiển thị và ảnh đại diện của bạn.'
  }

  if (activeTab.value === 'ai') {
    return 'Cấu hình cách AI chọn task đáng làm, giảm gợi ý trùng và hạn chế spam.'
  }

  return 'Cập nhật mật khẩu tài khoản.'
})

const priorityDisplayLabel = computed(() => {
  const priority = props.taskPreferenceForm.minPriorityForRecommendation

  if (priority === 'high') return 'High'
  if (priority === 'low') return 'Low'

  return 'Medium'
})

const sensitivityLabel = computed(() => {
  const value = props.taskPreferenceForm.recommendationSensitivity

  if (value >= 75) return 'Nghiêm ngặt'
  if (value >= 55) return 'Cân bằng'
  if (value >= 35) return 'Rộng vừa'

  return 'Mở rộng'
})

const aiStatusLabel = computed(() => {
  if (!props.workspaceId) return 'Chưa chọn workspace'

  return props.taskPreferenceForm.enableAutoRecommendation
    ? 'Auto đang bật'
    : 'Auto đang tắt'
})

const aiStatusClass = computed(() => {
  return {
    enabled: Boolean(props.workspaceId && props.taskPreferenceForm.enableAutoRecommendation),
    disabled: Boolean(props.workspaceId && !props.taskPreferenceForm.enableAutoRecommendation),
  }
})

const aiStatusDescription = computed(() => {
  if (!props.workspaceId) {
    return 'AI settings được lưu theo từng workspace, nên hãy chọn workspace trước khi chỉnh.'
  }

  if (!props.taskPreferenceForm.enableAutoRecommendation) {
    return 'Auto recommendation đang tắt. Nút “Phân tích task” vẫn dùng các rule chất lượng bên dưới khi chạy thủ công.'
  }

  return 'Auto recommendation sẽ chạy theo khung giờ/ngày bạn chọn. Manual analyze vẫn chạy ngay khi bạn bấm.'
})

const aiValidationMessage = computed(() => {
  const form = props.taskPreferenceForm

  if (
    !Number.isInteger(Number(form.workDayStartHour)) ||
    form.workDayStartHour < 0 ||
    form.workDayStartHour > 23
  ) {
    return 'Start hour phải nằm trong khoảng 0-23.'
  }

  if (
    !Number.isInteger(Number(form.workDayEndHour)) ||
    form.workDayEndHour < 0 ||
    form.workDayEndHour > 23
  ) {
    return 'End hour phải nằm trong khoảng 0-23.'
  }

  if (form.workDayStartHour >= form.workDayEndHour) {
    return 'Start hour phải nhỏ hơn End hour.'
  }

  if (!form.preferredDaysOfWeek.length) {
    return 'Cần chọn ít nhất 1 ngày cho auto recommendation.'
  }

  if (
    !Number.isInteger(Number(form.maxRecommendationsPerSession)) ||
    form.maxRecommendationsPerSession < 1 ||
    form.maxRecommendationsPerSession > 20
  ) {
    return 'Max per run phải nằm trong khoảng 1-20.'
  }

  if (
    !Number.isInteger(Number(form.recommendationSensitivity)) ||
    form.recommendationSensitivity < 0 ||
    form.recommendationSensitivity > 100
  ) {
    return 'Quality threshold phải nằm trong khoảng 0-100.'
  }

  if (
    !Number.isInteger(Number(form.recommendationIntervalMinutes)) ||
    form.recommendationIntervalMinutes < 1 ||
    form.recommendationIntervalMinutes > 1440
  ) {
    return 'Auto interval phải nằm trong khoảng 1-1440 phút.'
  }

  return null
})

watch(
  () => props.profileForm.avatarUrl,
  () => {
    avatarPreviewFailed.value = false
  }
)

function isPresetActive(presetKey: AiPreferencePresetKey) {
  const preset = aiPresetOptions.find((item) => item.key === presetKey)

  if (!preset) return false

  const form = props.taskPreferenceForm
  const values = preset.values
  const dayKey = [...form.preferredDaysOfWeek].sort().join(',')
  const presetDayKey = [...values.preferredDaysOfWeek].sort().join(',')

  return (
    form.workDayStartHour === values.workDayStartHour &&
    form.workDayEndHour === values.workDayEndHour &&
    form.maxRecommendationsPerSession === values.maxRecommendationsPerSession &&
    form.minPriorityForRecommendation === values.minPriorityForRecommendation &&
    form.recommendationSensitivity === values.recommendationSensitivity &&
    form.recommendationIntervalMinutes === values.recommendationIntervalMinutes &&
    form.enableAutoRecommendation === values.enableAutoRecommendation &&
    dayKey === presetDayKey
  )
}

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
  background: rgba(0, 0, 0, 0.5);
  backdrop-filter: blur(6px);
}

.settings-modal-shell {
  position: relative;
  z-index: 1;
  width: min(980px, calc(100vw - 40px));
  height: min(740px, calc(100vh - 40px));
  max-height: calc(100vh - 40px);
  min-height: 0;
  display: grid;
  grid-template-columns: 230px minmax(0, 1fr);
  overflow: hidden;
  border: 1px solid #2b2b2b;
  border-radius: 14px;
  background: #191919;
  box-shadow: 0 24px 80px rgba(0, 0, 0, 0.5);
}

.settings-modal-sidebar {
  min-width: 0;
  min-height: 0;
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
  font-weight: 720;
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
  gap: 2px;
}

.settings-modal-tab {
  width: 100%;
  border: 0;
  border-radius: 8px;
  display: grid;
  grid-template-columns: 22px minmax(0, 1fr);
  align-items: center;
  column-gap: 8px;
  row-gap: 1px;
  padding: 8px;
  color: #a3a3a3;
  background: transparent;
  text-align: left;
}

.settings-modal-tab:hover,
.settings-modal-tab.active {
  color: #f1f1f1;
  background: #242424;
}

.settings-modal-tab i {
  grid-row: span 2;
  color: #737373;
  font-size: 15px;
}

.settings-modal-tab.active i,
.settings-modal-tab:hover i {
  color: #f1f1f1;
}

.settings-modal-tab span {
  min-width: 0;
  overflow: hidden;
  font-size: 13px;
  font-weight: 680;
  white-space: nowrap;
  text-overflow: ellipsis;
}

.settings-modal-tab small {
  min-width: 0;
  overflow: hidden;
  color: #737373;
  font-size: 11px;
  white-space: nowrap;
  text-overflow: ellipsis;
}

.settings-modal-main {
  min-width: 0;
  min-height: 0;
  overflow: hidden;
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
  font-weight: 760;
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
  height: 100%;
  max-height: 100%;
  flex: 1 1 auto;
  overflow-x: hidden;
  overflow-y: scroll;
  overscroll-behavior: contain;
  scrollbar-gutter: stable;
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
  max-width: 680px;
  display: flex;
  flex-direction: column;
  gap: 18px;
}

.ai-settings-section {
  max-width: 760px;
  padding-bottom: 34px;
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
  border-radius: 999px;
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

.settings-upload-btn,
.settings-soft-action {
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

.settings-upload-btn:hover:not(:disabled),
.settings-soft-action:hover:not(:disabled) {
  color: #ffffff;
  background: #272727;
  border-color: #3a3a3a;
}

.settings-upload-btn:disabled,
.settings-soft-action:disabled {
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

.settings-group-heading {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
}

.settings-group-heading h3,
.settings-form-group h3 {
  margin: 0 0 2px;
  color: #f1f1f1;
  font-size: 13px;
  font-weight: 720;
}

.settings-group-heading p {
  margin: 0;
  color: #8a8a8a;
  font-size: 12px;
  line-height: 1.45;
}

.settings-field {
  min-width: 0;
  display: grid;
  grid-template-columns: 160px minmax(0, 1fr);
  align-items: center;
  gap: 16px;
}

.settings-field.compact {
  grid-template-columns: 1fr;
  gap: 6px;
}

.settings-field-top {
  align-items: flex-start;
}

.settings-field > span {
  color: #a3a3a3;
  font-size: 12px;
  font-weight: 650;
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

.settings-field-grid .settings-field:not(.compact) {
  grid-template-columns: 1fr;
  gap: 6px;
}

.settings-field-note {
  display: block;
  margin-top: 6px;
  color: #737373;
  font-size: 11.5px;
  line-height: 1.45;
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
  border: 1px solid transparent;
  border-radius: 6px;
  color: #a3a3a3;
  background: transparent;
  font-size: 12px;
}

.settings-day-row button:hover:not(:disabled),
.settings-day-row button.active {
  color: #f1f1f1;
  border-color: #343434;
  background: #242424;
}

.settings-switch-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 2px 0;
}

.settings-switch-row.elevated {
  padding: 12px;
  border: 1px solid #303030;
  border-radius: 10px;
  background: #202020;
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
  font-weight: 680;
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

.settings-ai-hero,
.settings-ai-explain-card {
  border: 1px solid #303030;
  border-radius: 12px;
  background: #2a2a2a;
}

.settings-ai-hero {
  display: flex;
  flex-direction: column;
  gap: 14px;
  padding: 14px;
}

.settings-ai-hero-main {
  display: flex;
  align-items: flex-start;
  gap: 12px;
}

.settings-ai-hero-icon,
.settings-empty-icon {
  width: 36px;
  height: 36px;
  border-radius: 10px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  color: #f1f1f1;
  background: #2a2a2a;
}

.settings-ai-title-row {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px;
}

.settings-ai-title-row strong {
  color: #f1f1f1;
  font-size: 15px;
  font-weight: 760;
}

.settings-ai-hero-main p,
.settings-ai-explain-card span,
.ai-empty span {
  margin: 4px 0 0;
  color: #a3a3a3;
  font-size: 12.5px;
  line-height: 1.5;
}

.settings-ai-status {
  min-height: 22px;
  border: 1px solid #3a3a3a;
  border-radius: 999px;
  display: inline-flex;
  align-items: center;
  padding: 2px 8px;
  color: #a3a3a3;
  background: #191919;
  font-size: 11px;
  font-weight: 750;
}

.settings-ai-status.enabled {
  color: #8fd19e;
  border-color: rgba(143, 209, 158, 0.28);
  background: rgba(143, 209, 158, 0.08);
}

.settings-ai-status.disabled {
  color: #f1c27d;
  border-color: rgba(241, 194, 125, 0.28);
  background: rgba(241, 194, 125, 0.08);
}

.settings-ai-summary-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 8px;
}

.settings-ai-summary-grid span {
  min-width: 0;
  border: 1px solid #303030;
  border-radius: 10px;
  display: flex;
  flex-direction: column;
  gap: 2px;
  padding: 10px;
  color: #8a8a8a;
  background: rgba(0, 0, 0, 0.12);
  font-size: 11.5px;
}

.settings-ai-summary-grid strong {
  color: #f1f1f1;
  font-size: 13px;
}

.settings-ai-preset-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 8px;
}

.settings-ai-preset-card {
  min-width: 0;
  border: 1px solid #303030;
  border-radius: 10px;
  display: flex;
  align-items: flex-start;
  gap: 10px;
  padding: 11px;
  color: #a3a3a3;
  background: #202020;
  text-align: left;
  transition:
    border-color 0.14s ease,
    background 0.14s ease,
    color 0.14s ease,
    transform 0.14s ease;
}

.settings-ai-preset-card:hover:not(:disabled),
.settings-ai-preset-card.active {
  color: #f1f1f1;
  border-color: #4a4a4a;
  background: #262626;
}

.settings-ai-preset-card:hover:not(:disabled) {
  transform: translateY(-1px);
}

.settings-ai-preset-card i {
  width: 22px;
  color: #f1f1f1;
  font-size: 16px;
}

.settings-ai-preset-card span {
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 3px;
}

.settings-ai-preset-card strong {
  color: inherit;
  font-size: 12.5px;
  font-weight: 760;
}

.settings-ai-preset-card small {
  color: #8a8a8a;
  font-size: 11.5px;
  line-height: 1.35;
}

.settings-range-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 6px;
}

.settings-range-head strong {
  color: #f1f1f1;
  font-size: 12px;
}

.settings-range-head small {
  color: #8a8a8a;
  font-size: 12px;
}

.settings-ai-explain-card {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  padding: 12px;
}

.settings-ai-explain-card i {
  color: #a3a3a3;
  font-size: 14px;
  margin-top: 2px;
}

.settings-modal-actions {
  display: flex;
  justify-content: flex-end;
}

.sticky-actions {
  position: sticky;
  bottom: -30px;
  padding-top: 12px;
  padding-bottom: 4px;
  background: linear-gradient(to top, #191919 72%, rgba(25, 25, 25, 0));
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
  font-weight: 680;
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

.settings-modal-empty.ai-empty {
  align-items: flex-start;
}

.ai-empty strong {
  display: block;
  color: #f1f1f1;
  font-size: 14px;
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
.settings-soft-action:focus-visible,
.settings-ai-preset-card:focus-visible,
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

@media (max-width: 860px) {
  .settings-ai-summary-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .settings-ai-preset-grid {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 760px) {
  .settings-modal-layer {
    padding: 0;
  }

  .settings-modal-shell {
    width: 100vw;
    height: 100vh;
    max-height: 100vh;
    border-radius: 0;
    grid-template-columns: 1fr;
    grid-template-rows: auto minmax(0, 1fr);
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
    min-width: 150px;
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

  .settings-modal-main {
    min-height: 0;
  }

  .settings-modal-content {
    max-height: none;
  }
}

@media (prefers-reduced-motion: reduce) {
  .settings-modal-enter-active,
  .settings-modal-leave-active,
  .settings-modal-enter-active .settings-modal-shell,
  .settings-modal-leave-active .settings-modal-shell,
  .settings-modal-spinner,
  .settings-ai-preset-card {
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
