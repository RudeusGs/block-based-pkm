<template>
  <section class="lunar-panel settings">
    <div class="lunar-panel-head">
      <span>Settings</span>

      <button
        type="button"
        title="Đóng"
        @click.stop="emit('close')"
      >
        <i class="bi bi-x-lg"></i>
      </button>
    </div>

    <div class="lunar-settings-tabs">
      <button
        type="button"
        :class="{ active: activeTab === 'profile' }"
        @click.stop="activeTab = 'profile'"
      >
        Profile
      </button>

      <button
        type="button"
        :class="{ active: activeTab === 'ai' }"
        @click.stop="activeTab = 'ai'"
      >
        AI
      </button>

      <button
        type="button"
        :class="{ active: activeTab === 'security' }"
        @click.stop="activeTab = 'security'"
      >
        Security
      </button>
    </div>

    <div
      v-if="activeTab === 'profile'"
      class="lunar-settings-body"
    >
      <div
        v-if="isLoadingProfileSettings"
        class="lunar-empty compact"
      >
        Đang tải hồ sơ…
      </div>

      <template v-else>
        <label class="lunar-field">
          <span>Full name</span>

          <input
            v-model="profileForm.fullName"
            type="text"
            maxlength="100"
            placeholder="Tên hiển thị"
            :disabled="isSavingProfileSettings"
          />
        </label>

        <label class="lunar-field">
          <span>Avatar URL</span>

          <input
            v-model="profileForm.avatarUrl"
            type="text"
            placeholder="https://..."
            :disabled="isSavingProfileSettings"
          />
        </label>

        <p
          v-if="profileSettingsError"
          class="lunar-inline-error"
        >
          {{ profileSettingsError }}
        </p>

        <p
          v-if="profileSettingsSuccess"
          class="lunar-inline-success"
        >
          {{ profileSettingsSuccess }}
        </p>

        <button
          type="button"
          class="lunar-primary-action"
          :disabled="isSavingProfileSettings || !profileForm.fullName.trim()"
          @click.stop="emit('saveProfile')"
        >
          <span
            v-if="isSavingProfileSettings"
            class="lunar-ai-spinner small"
          ></span>

          <span>
            {{ isSavingProfileSettings ? 'Saving...' : 'Save profile' }}
          </span>
        </button>
      </template>
    </div>

    <div
      v-else-if="activeTab === 'ai'"
      class="lunar-settings-body"
    >
      <div
        v-if="!workspaceId"
        class="lunar-empty compact"
      >
        Chọn workspace để cấu hình AI.
      </div>

      <div
        v-else-if="isLoadingTaskPreference"
        class="lunar-empty compact"
      >
        Đang tải cấu hình AI…
      </div>

      <template v-else>
        <div class="lunar-field-grid">
          <label class="lunar-field">
            <span>Start hour</span>

            <input
              v-model.number="taskPreferenceForm.workDayStartHour"
              type="number"
              min="0"
              max="23"
              :disabled="isSavingTaskPreference"
            />
          </label>

          <label class="lunar-field">
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

        <label class="lunar-field">
          <span>Preferred days</span>

          <div class="lunar-day-grid">
            <button
              v-for="day in preferredDayOptions"
              :key="day.value"
              type="button"
              :title="day.title"
              :class="{ active: taskPreferenceForm.preferredDaysOfWeek.includes(day.value) }"
              @click.stop="emit('togglePreferredDay', day.value)"
            >
              {{ day.label }}
            </button>
          </div>
        </label>

        <div class="lunar-field-grid">
          <label class="lunar-field">
            <span>Max/session</span>

            <input
              v-model.number="taskPreferenceForm.maxRecommendationsPerSession"
              type="number"
              min="1"
              max="20"
              :disabled="isSavingTaskPreference"
            />
          </label>

          <label class="lunar-field">
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

        <label class="lunar-field">
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

        <label class="lunar-field">
          <span>Interval minutes</span>

          <input
            v-model.number="taskPreferenceForm.recommendationIntervalMinutes"
            type="number"
            min="5"
            max="1440"
            :disabled="isSavingTaskPreference"
          />
        </label>

        <label class="lunar-switch-row">
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
          class="lunar-inline-error"
        >
          {{ taskPreferenceError }}
        </p>

        <p
          v-if="taskPreferenceSuccess"
          class="lunar-inline-success"
        >
          {{ taskPreferenceSuccess }}
        </p>

        <button
          type="button"
          class="lunar-primary-action"
          :disabled="isSavingTaskPreference"
          @click.stop="emit('saveTaskPreference')"
        >
          <span
            v-if="isSavingTaskPreference"
            class="lunar-ai-spinner small"
          ></span>

          <span>
            {{ isSavingTaskPreference ? 'Saving...' : 'Save AI settings' }}
          </span>
        </button>
      </template>
    </div>

    <div
      v-else
      class="lunar-settings-body"
    >
      <label class="lunar-field">
        <span>Current password</span>

        <input
          v-model="passwordForm.currentPassword"
          type="password"
          autocomplete="current-password"
          :disabled="isChangingPassword"
        />
      </label>

      <label class="lunar-field">
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
        class="lunar-inline-error"
      >
        {{ passwordSettingsError }}
      </p>

      <p
        v-if="passwordSettingsSuccess"
        class="lunar-inline-success"
      >
        {{ passwordSettingsSuccess }}
      </p>

      <button
        type="button"
        class="lunar-primary-action"
        :disabled="isChangingPassword || !passwordForm.currentPassword || !passwordForm.newPassword"
        @click.stop="emit('changePassword')"
      >
        <span
          v-if="isChangingPassword"
          class="lunar-ai-spinner small"
        ></span>

        <span>
          {{ isChangingPassword ? 'Updating...' : 'Change password' }}
        </span>
      </button>
    </div>
  </section>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import type { Guid } from '@/api/models/common.model'
import type { UpdateUserTaskPreferenceRequest } from '@/api/models/recommendation.model'
import type { SidebarSettingsTab } from './useSidebarSettings'

defineProps<{
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
</script>