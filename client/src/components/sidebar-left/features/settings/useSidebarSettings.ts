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
      profileForm.avatarUrl = result.data.avatarUrl ?? ''
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

    if (!fullName || isSavingProfileSettings.value) return false

    isSavingProfileSettings.value = true
    profileSettingsError.value = null
    profileSettingsSuccess.value = null

    try {
      const result = await meController.updateMyProfile({
        fullName,
        avatarUrl: profileForm.avatarUrl.trim() || null,
      })

      if (!result.isSuccess || !result.data) {
        profileSettingsError.value = getApiResultErrorMessage(
          result,
          'Không thể cập nhật hồ sơ.'
        )
        return false
      }

      profileSettingsSuccess.value = 'Đã lưu hồ sơ.'
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
      !passwordForm.currentPassword ||
      !passwordForm.newPassword ||
      isChangingPassword.value
    ) {
      return
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
        return
      }

      passwordSettingsSuccess.value = 'Đã đổi mật khẩu.'
      passwordForm.currentPassword = ''
      passwordForm.newPassword = ''
    } catch (error) {
      passwordSettingsError.value = getApiErrorMessage(
        error,
        'Không thể đổi mật khẩu.'
      )
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
          'Không thể tải cấu hình AI gợi ý.'
        )
        return
      }

      applyTaskPreference(result.data)
    } catch (error) {
      taskPreferenceError.value = getApiErrorMessage(
        error,
        'Không thể tải cấu hình AI gợi ý.'
      )
    } finally {
      isLoadingTaskPreference.value = false
    }
  }

  function applyTaskPreference(preference: UserTaskPreferenceResponse) {
    taskPreferenceForm.workDayStartHour = preference.workDayStartHour
    taskPreferenceForm.workDayEndHour = preference.workDayEndHour
    taskPreferenceForm.preferredDaysOfWeek = [...preference.preferredDaysOfWeek]
    taskPreferenceForm.maxRecommendationsPerSession =
      preference.maxRecommendationsPerSession
    taskPreferenceForm.minPriorityForRecommendation =
      preference.minPriorityForRecommendation.toLowerCase() as PriorityRequest
    taskPreferenceForm.recommendationSensitivity =
      preference.recommendationSensitivity
    taskPreferenceForm.recommendationIntervalMinutes =
      preference.recommendationIntervalMinutes
    taskPreferenceForm.enableAutoRecommendation =
      preference.enableAutoRecommendation
  }

  function togglePreferredDay(day: number) {
    const current = new Set(taskPreferenceForm.preferredDaysOfWeek)

    if (current.has(day)) {
      current.delete(day)
    } else {
      current.add(day)
    }

    taskPreferenceForm.preferredDaysOfWeek = Array.from(current).sort()
  }

  async function saveTaskPreference(workspaceId: Guid | null) {
    if (!workspaceId || isSavingTaskPreference.value) return false

    if (taskPreferenceForm.workDayStartHour >= taskPreferenceForm.workDayEndHour) {
      taskPreferenceError.value = 'Giờ bắt đầu phải nhỏ hơn giờ kết thúc.'
      return false
    }

    if (!taskPreferenceForm.preferredDaysOfWeek.length) {
      taskPreferenceError.value = 'Chọn ít nhất một ngày làm việc.'
      return false
    }

    isSavingTaskPreference.value = true
    taskPreferenceError.value = null
    taskPreferenceSuccess.value = null

    try {
      const result = await recommendationController.updatePreference(
        workspaceId,
        {
          ...taskPreferenceForm,
          preferredDaysOfWeek: [...taskPreferenceForm.preferredDaysOfWeek],
        }
      )

      if (!result.isSuccess || !result.data) {
        taskPreferenceError.value = getApiResultErrorMessage(
          result,
          'Không thể lưu cấu hình AI gợi ý.'
        )
        return false
      }

      applyTaskPreference(result.data)
      taskPreferenceSuccess.value = 'Đã lưu cấu hình AI gợi ý.'
      return true
    } catch (error) {
      taskPreferenceError.value = getApiErrorMessage(
        error,
        'Không thể lưu cấu hình AI gợi ý.'
      )
      return false
    } finally {
      isSavingTaskPreference.value = false
    }
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
    togglePreferredDay,
    saveTaskPreference,
  }
}