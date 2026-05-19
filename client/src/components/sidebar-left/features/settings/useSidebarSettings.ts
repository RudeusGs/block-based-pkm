import { reactive, ref } from 'vue'
import { meController } from '@/api/services/me.api'
import { recommendationController } from '@/api/services/recommendation.api'
import {
  getApiErrorMessage,
  getApiResultErrorMessage,
} from '@/api/utils/api-error.util'
import type { Guid } from '@/api/models/common.model'
import type {
  PriorityRequest,
  UpdateUserTaskPreferenceRequest,
  UserTaskPreferenceResponse,
} from '@/api/models/recommendation.model'
import { normalizeImageUrl } from '@/utils/image-url.util'

export type SidebarSettingsTab = 'profile' | 'ai' | 'security'

export const preferredDayOptions = [
  { value: 1, label: 'Mon', title: 'Monday' },
  { value: 2, label: 'Tue', title: 'Tuesday' },
  { value: 3, label: 'Wed', title: 'Wednesday' },
  { value: 4, label: 'Thu', title: 'Thursday' },
  { value: 5, label: 'Fri', title: 'Friday' },
  { value: 6, label: 'Sat', title: 'Saturday' },
  { value: 0, label: 'Sun', title: 'Sunday' },
]

const DEFAULT_TASK_PREFERENCE: UpdateUserTaskPreferenceRequest = {
  workDayStartHour: 8,
  workDayEndHour: 18,
  preferredDaysOfWeek: [1, 2, 3, 4, 5],
  maxRecommendationsPerSession: 3,
  minPriorityForRecommendation: 'medium',
  recommendationSensitivity: 50,
  recommendationIntervalMinutes: 30,
  enableAutoRecommendation: true,
}

function applyTaskPreference(
  target: UpdateUserTaskPreferenceRequest,
  source: UserTaskPreferenceResponse | UpdateUserTaskPreferenceRequest
) {
  target.workDayStartHour = source.workDayStartHour
  target.workDayEndHour = source.workDayEndHour
  target.preferredDaysOfWeek = [...source.preferredDaysOfWeek]
  target.maxRecommendationsPerSession = source.maxRecommendationsPerSession
  target.minPriorityForRecommendation =
    source.minPriorityForRecommendation as PriorityRequest
  target.recommendationSensitivity = source.recommendationSensitivity
  target.recommendationIntervalMinutes = source.recommendationIntervalMinutes
  target.enableAutoRecommendation = source.enableAutoRecommendation
}

export function useSidebarSettings() {
  const isLoadingProfileSettings = ref(false)
  const isSavingProfileSettings = ref(false)
  const profileSettingsError = ref<string | null>(null)
  const profileSettingsSuccess = ref<string | null>(null)

  const isChangingPassword = ref(false)
  const passwordSettingsError = ref<string | null>(null)
  const passwordSettingsSuccess = ref<string | null>(null)

  const isLoadingTaskPreference = ref(false)
  const isSavingTaskPreference = ref(false)
  const taskPreferenceError = ref<string | null>(null)
  const taskPreferenceSuccess = ref<string | null>(null)

  const profileForm = reactive({
    fullName: '',
    avatarUrl: '',
  })

  const passwordForm = reactive({
    currentPassword: '',
    newPassword: '',
  })

  const taskPreferenceForm = reactive<UpdateUserTaskPreferenceRequest>({
    ...DEFAULT_TASK_PREFERENCE,
    preferredDaysOfWeek: [...DEFAULT_TASK_PREFERENCE.preferredDaysOfWeek],
  })

  async function fetchProfileSettings() {
    if (isLoadingProfileSettings.value) return

    isLoadingProfileSettings.value = true
    profileSettingsError.value = null
    profileSettingsSuccess.value = null

    try {
      const result = await meController.getMyProfile()

      if (!result.isSuccess || !result.data) {
        profileSettingsError.value = getApiResultErrorMessage(
          result,
          'Không thể tải hồ sơ.'
        )
        return
      }

      profileForm.fullName = result.data.fullName
      profileForm.avatarUrl = normalizeImageUrl(result.data.avatarUrl) ?? ''
    } catch (error) {
      profileSettingsError.value = getApiErrorMessage(
        error,
        'Không thể tải hồ sơ.'
      )
    } finally {
      isLoadingProfileSettings.value = false
    }
  }

  async function saveProfileSettings() {
    const fullName = profileForm.fullName.trim()
    const avatarUrl = normalizeImageUrl(profileForm.avatarUrl)

    if (!fullName || isSavingProfileSettings.value) return false

    isSavingProfileSettings.value = true
    profileSettingsError.value = null
    profileSettingsSuccess.value = null

    try {
      const result = await meController.updateMyProfile({
        fullName,
        avatarUrl,
      })

      if (!result.isSuccess || !result.data) {
        profileSettingsError.value = getApiResultErrorMessage(
          result,
          'Không thể cập nhật hồ sơ.'
        )
        return false
      }

      profileForm.fullName = result.data.fullName
      profileForm.avatarUrl = normalizeImageUrl(result.data.avatarUrl) ?? ''
      profileSettingsSuccess.value = 'Đã cập nhật hồ sơ.'

      return true
    } catch (error) {
      profileSettingsError.value = getApiErrorMessage(
        error,
        'Không thể cập nhật hồ sơ.'
      )
      return false
    } finally {
      isSavingProfileSettings.value = false
    }
  }

  async function changePassword() {
    if (
      isChangingPassword.value ||
      !passwordForm.currentPassword ||
      !passwordForm.newPassword
    ) {
      return false
    }

    isChangingPassword.value = true
    passwordSettingsError.value = null
    passwordSettingsSuccess.value = null

    try {
      const result = await meController.changeMyPassword({
        currentPassword: passwordForm.currentPassword,
        newPassword: passwordForm.newPassword,
      })

      if (!result.isSuccess) {
        passwordSettingsError.value = getApiResultErrorMessage(
          result,
          'Không thể đổi mật khẩu.'
        )
        return false
      }

      passwordForm.currentPassword = ''
      passwordForm.newPassword = ''
      passwordSettingsSuccess.value = 'Đã đổi mật khẩu.'

      return true
    } catch (error) {
      passwordSettingsError.value = getApiErrorMessage(
        error,
        'Không thể đổi mật khẩu.'
      )
      return false
    } finally {
      isChangingPassword.value = false
    }
  }

  async function fetchTaskPreference(workspaceId: Guid | null) {
    if (!workspaceId || isLoadingTaskPreference.value) return

    isLoadingTaskPreference.value = true
    taskPreferenceError.value = null
    taskPreferenceSuccess.value = null

    try {
      const result = await recommendationController.getPreference(workspaceId)

      if (!result.isSuccess || !result.data) {
        taskPreferenceError.value = getApiResultErrorMessage(
          result,
          'Không thể tải cấu hình AI.'
        )
        return
      }

      applyTaskPreference(taskPreferenceForm, result.data)
    } catch (error) {
      taskPreferenceError.value = getApiErrorMessage(
        error,
        'Không thể tải cấu hình AI.'
      )
    } finally {
      isLoadingTaskPreference.value = false
    }
  }

  async function saveTaskPreference(workspaceId: Guid | null) {
    if (!workspaceId || isSavingTaskPreference.value) return false

    isSavingTaskPreference.value = true
    taskPreferenceError.value = null
    taskPreferenceSuccess.value = null

    try {
      const result = await recommendationController.updatePreference(
        workspaceId,
        {
          workDayStartHour: taskPreferenceForm.workDayStartHour,
          workDayEndHour: taskPreferenceForm.workDayEndHour,
          preferredDaysOfWeek: [...taskPreferenceForm.preferredDaysOfWeek],
          maxRecommendationsPerSession:
            taskPreferenceForm.maxRecommendationsPerSession,
          minPriorityForRecommendation:
            taskPreferenceForm.minPriorityForRecommendation,
          recommendationSensitivity: taskPreferenceForm.recommendationSensitivity,
          recommendationIntervalMinutes:
            taskPreferenceForm.recommendationIntervalMinutes,
          enableAutoRecommendation: taskPreferenceForm.enableAutoRecommendation,
        }
      )

      if (!result.isSuccess || !result.data) {
        taskPreferenceError.value = getApiResultErrorMessage(
          result,
          'Không thể lưu cấu hình AI.'
        )
        return false
      }

      applyTaskPreference(taskPreferenceForm, result.data)
      taskPreferenceSuccess.value = 'Đã lưu cấu hình AI.'

      return true
    } catch (error) {
      taskPreferenceError.value = getApiErrorMessage(
        error,
        'Không thể lưu cấu hình AI.'
      )
      return false
    } finally {
      isSavingTaskPreference.value = false
    }
  }

  function togglePreferredDay(day: number) {
    const index = taskPreferenceForm.preferredDaysOfWeek.indexOf(day)

    if (index === -1) {
      taskPreferenceForm.preferredDaysOfWeek.push(day)
      taskPreferenceForm.preferredDaysOfWeek.sort()
      return
    }

    if (taskPreferenceForm.preferredDaysOfWeek.length <= 1) return

    taskPreferenceForm.preferredDaysOfWeek.splice(index, 1)
  }

  return {
    profileForm,
    passwordForm,
    taskPreferenceForm,
    preferredDayOptions,

    isLoadingProfileSettings,
    isSavingProfileSettings,
    profileSettingsError,
    profileSettingsSuccess,

    isChangingPassword,
    passwordSettingsError,
    passwordSettingsSuccess,

    isLoadingTaskPreference,
    isSavingTaskPreference,
    taskPreferenceError,
    taskPreferenceSuccess,

    fetchProfileSettings,
    saveProfileSettings,
    changePassword,
    fetchTaskPreference,
    saveTaskPreference,
    togglePreferredDay,
  }
}