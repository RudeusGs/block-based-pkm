import { reactive, ref } from 'vue'
import { meController } from '@/api/services/me.api'
import { fileController } from '@/api/services/file.api'
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
export type AiPreferencePresetKey = 'balanced' | 'focused' | 'strict' | 'wide'

export interface AiPreferencePreset {
  key: AiPreferencePresetKey
  label: string
  description: string
  icon: string
  values: UpdateUserTaskPreferenceRequest
}

export const preferredDayOptions = [
  { value: 1, label: 'Mon', title: 'Monday' },
  { value: 2, label: 'Tue', title: 'Tuesday' },
  { value: 3, label: 'Wed', title: 'Wednesday' },
  { value: 4, label: 'Thu', title: 'Thursday' },
  { value: 5, label: 'Fri', title: 'Friday' },
  { value: 6, label: 'Sat', title: 'Saturday' },
  { value: 0, label: 'Sun', title: 'Sunday' },
]

const WORKDAY_DAYS = [1, 2, 3, 4, 5]
const ALL_DAYS = [0, 1, 2, 3, 4, 5, 6]

const DEFAULT_TASK_PREFERENCE: UpdateUserTaskPreferenceRequest = {
  workDayStartHour: 8,
  workDayEndHour: 18,
  preferredDaysOfWeek: [...WORKDAY_DAYS],
  maxRecommendationsPerSession: 4,
  minPriorityForRecommendation: 'medium',
  recommendationSensitivity: 58,
  recommendationIntervalMinutes: 60,
  enableAutoRecommendation: true,
}

export const aiPreferencePresets: AiPreferencePreset[] = [
  {
    key: 'balanced',
    label: 'Cân bằng',
    description: 'Ít spam, ưu tiên deadline + task medium/high. Hợp dùng mặc định.',
    icon: 'bi-sliders2',
    values: {
      ...DEFAULT_TASK_PREFERENCE,
      preferredDaysOfWeek: [...WORKDAY_DAYS],
    },
  },
  {
    key: 'focused',
    label: 'Tập trung',
    description: 'Gợi ý ít hơn, chọn task có tín hiệu mạnh hơn để làm ngay.',
    icon: 'bi-bullseye',
    values: {
      workDayStartHour: 8,
      workDayEndHour: 18,
      preferredDaysOfWeek: [...WORKDAY_DAYS],
      maxRecommendationsPerSession: 3,
      minPriorityForRecommendation: 'medium',
      recommendationSensitivity: 70,
      recommendationIntervalMinutes: 120,
      enableAutoRecommendation: true,
    },
  },
  {
    key: 'strict',
    label: 'Nghiêm ngặt',
    description: 'Chỉ đẩy việc high-priority, deadline rõ, tránh gợi ý linh tinh.',
    icon: 'bi-shield-check',
    values: {
      workDayStartHour: 8,
      workDayEndHour: 18,
      preferredDaysOfWeek: [...WORKDAY_DAYS],
      maxRecommendationsPerSession: 2,
      minPriorityForRecommendation: 'high',
      recommendationSensitivity: 82,
      recommendationIntervalMinutes: 180,
      enableAutoRecommendation: true,
    },
  },
  {
    key: 'wide',
    label: 'Nhiều gợi ý',
    description: 'Mở rộng phạm vi phân tích, hợp lúc muốn rà toàn bộ backlog.',
    icon: 'bi-lightning-charge',
    values: {
      workDayStartHour: 7,
      workDayEndHour: 22,
      preferredDaysOfWeek: [...ALL_DAYS],
      maxRecommendationsPerSession: 6,
      minPriorityForRecommendation: 'low',
      recommendationSensitivity: 42,
      recommendationIntervalMinutes: 45,
      enableAutoRecommendation: true,
    },
  },
]

function cloneTaskPreference(
  source: UpdateUserTaskPreferenceRequest
): UpdateUserTaskPreferenceRequest {
  return {
    workDayStartHour: source.workDayStartHour,
    workDayEndHour: source.workDayEndHour,
    preferredDaysOfWeek: [...source.preferredDaysOfWeek],
    maxRecommendationsPerSession: source.maxRecommendationsPerSession,
    minPriorityForRecommendation: source.minPriorityForRecommendation,
    recommendationSensitivity: source.recommendationSensitivity,
    recommendationIntervalMinutes: source.recommendationIntervalMinutes,
    enableAutoRecommendation: source.enableAutoRecommendation,
  }
}

function normalizePriority(value: string): PriorityRequest {
  const normalized = value.trim().toLowerCase()

  if (normalized === 'low' || normalized === 'medium' || normalized === 'high') {
    return normalized
  }

  return DEFAULT_TASK_PREFERENCE.minPriorityForRecommendation
}

function uniqueSortedDays(days: number[]) {
  return Array.from(
    new Set(
      days
        .map((day) => Number(day))
        .filter((day) => Number.isInteger(day) && day >= 0 && day <= 6)
    )
  ).sort((a, b) => a - b)
}

function applyTaskPreference(
  target: UpdateUserTaskPreferenceRequest,
  source: UserTaskPreferenceResponse | UpdateUserTaskPreferenceRequest
) {
  target.workDayStartHour = source.workDayStartHour
  target.workDayEndHour = source.workDayEndHour
  target.preferredDaysOfWeek = uniqueSortedDays([...source.preferredDaysOfWeek])
  target.maxRecommendationsPerSession = source.maxRecommendationsPerSession
  target.minPriorityForRecommendation = normalizePriority(
    source.minPriorityForRecommendation
  )
  target.recommendationSensitivity = source.recommendationSensitivity
  target.recommendationIntervalMinutes = source.recommendationIntervalMinutes
  target.enableAutoRecommendation = source.enableAutoRecommendation
}

function buildTaskPreferencePayload(
  form: UpdateUserTaskPreferenceRequest
): UpdateUserTaskPreferenceRequest {
  return {
    workDayStartHour: Number(form.workDayStartHour),
    workDayEndHour: Number(form.workDayEndHour),
    preferredDaysOfWeek: uniqueSortedDays([...form.preferredDaysOfWeek]),
    maxRecommendationsPerSession: Number(form.maxRecommendationsPerSession),
    minPriorityForRecommendation: normalizePriority(
      form.minPriorityForRecommendation
    ),
    recommendationSensitivity: Number(form.recommendationSensitivity),
    recommendationIntervalMinutes: Number(form.recommendationIntervalMinutes),
    enableAutoRecommendation: Boolean(form.enableAutoRecommendation),
  }
}

function validateTaskPreferencePayload(
  payload: UpdateUserTaskPreferenceRequest
): string | null {
  if (
    !Number.isInteger(payload.workDayStartHour) ||
    payload.workDayStartHour < 0 ||
    payload.workDayStartHour > 23
  ) {
    return 'Giờ bắt đầu phải nằm trong khoảng 0-23.'
  }

  if (
    !Number.isInteger(payload.workDayEndHour) ||
    payload.workDayEndHour < 0 ||
    payload.workDayEndHour > 23
  ) {
    return 'Giờ kết thúc phải nằm trong khoảng 0-23.'
  }

  if (payload.workDayStartHour >= payload.workDayEndHour) {
    return 'Giờ bắt đầu phải nhỏ hơn giờ kết thúc.'
  }

  if (!payload.preferredDaysOfWeek.length) {
    return 'Cần chọn ít nhất 1 ngày AI được phép tự gợi ý.'
  }

  if (
    !Number.isInteger(payload.maxRecommendationsPerSession) ||
    payload.maxRecommendationsPerSession < 1 ||
    payload.maxRecommendationsPerSession > 20
  ) {
    return 'Số gợi ý mỗi lần phải nằm trong khoảng 1-20.'
  }

  if (
    !Number.isInteger(payload.recommendationSensitivity) ||
    payload.recommendationSensitivity < 0 ||
    payload.recommendationSensitivity > 100
  ) {
    return 'Độ lọc AI phải nằm trong khoảng 0-100.'
  }

  if (
    !Number.isInteger(payload.recommendationIntervalMinutes) ||
    payload.recommendationIntervalMinutes < 1 ||
    payload.recommendationIntervalMinutes > 1440
  ) {
    return 'Khoảng nghỉ auto phải nằm trong khoảng 1-1440 phút.'
  }

  return null
}

export function useSidebarSettings() {
  const isLoadingProfileSettings = ref(false)
  const isSavingProfileSettings = ref(false)
  const isUploadingAvatarImage = ref(false)
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

  const taskPreferenceForm = reactive<UpdateUserTaskPreferenceRequest>(
    cloneTaskPreference(DEFAULT_TASK_PREFERENCE)
  )

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

  async function uploadAvatarImage(file: File) {
    if (isUploadingAvatarImage.value) return false

    isUploadingAvatarImage.value = true
    profileSettingsError.value = null
    profileSettingsSuccess.value = null

    try {
      const result = await fileController.uploadMyAvatarImage(file)

      if (!result.isSuccess || !result.data) {
        profileSettingsError.value = getApiResultErrorMessage(
          result,
          'Không thể upload avatar.'
        )
        return false
      }

      profileForm.fullName = result.data.fullName
      profileForm.avatarUrl = normalizeImageUrl(result.data.avatarUrl) ?? ''
      profileSettingsSuccess.value = 'Đã upload avatar.'

      return true
    } catch (error) {
      profileSettingsError.value = getApiErrorMessage(
        error,
        'Không thể upload avatar.'
      )
      return false
    } finally {
      isUploadingAvatarImage.value = false
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
    taskPreferenceSuccess.value = null

    if (!workspaceId) {
      taskPreferenceError.value = null
      applyTaskPreference(taskPreferenceForm, DEFAULT_TASK_PREFERENCE)
      return
    }

    if (isLoadingTaskPreference.value) return

    isLoadingTaskPreference.value = true
    taskPreferenceError.value = null

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

    const payload = buildTaskPreferencePayload(taskPreferenceForm)
    const validationMessage = validateTaskPreferencePayload(payload)

    if (validationMessage) {
      taskPreferenceError.value = validationMessage
      taskPreferenceSuccess.value = null
      return false
    }

    isSavingTaskPreference.value = true
    taskPreferenceError.value = null
    taskPreferenceSuccess.value = null

    try {
      const result = await recommendationController.updatePreference(
        workspaceId,
        payload
      )

      if (!result.isSuccess || !result.data) {
        taskPreferenceError.value = getApiResultErrorMessage(
          result,
          'Không thể lưu cấu hình AI.'
        )
        return false
      }

      applyTaskPreference(taskPreferenceForm, result.data)
      taskPreferenceSuccess.value =
        'Đã lưu AI settings. Gợi ý mới sẽ dùng cấu hình này ngay.'

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

    taskPreferenceError.value = null
    taskPreferenceSuccess.value = null

    if (index === -1) {
      taskPreferenceForm.preferredDaysOfWeek.push(day)
      taskPreferenceForm.preferredDaysOfWeek = uniqueSortedDays(
        taskPreferenceForm.preferredDaysOfWeek
      )
      return
    }

    if (taskPreferenceForm.preferredDaysOfWeek.length <= 1) {
      taskPreferenceError.value = 'Cần giữ ít nhất 1 ngày cho AI auto.'
      return
    }

    taskPreferenceForm.preferredDaysOfWeek.splice(index, 1)
  }

  function applyTaskPreferencePreset(presetKey: AiPreferencePresetKey) {
    const preset = aiPreferencePresets.find((item) => item.key === presetKey)

    if (!preset) return

    applyTaskPreference(taskPreferenceForm, preset.values)
    taskPreferenceError.value = null
    taskPreferenceSuccess.value = `Đã áp preset ${preset.label}. Bấm Save để lưu vào workspace.`
  }

  function resetTaskPreferenceToDefault() {
    applyTaskPreference(taskPreferenceForm, DEFAULT_TASK_PREFERENCE)
    taskPreferenceError.value = null
    taskPreferenceSuccess.value = 'Đã đưa AI settings về mặc định. Bấm Save để lưu.'
  }

  return {
    profileForm,
    passwordForm,
    taskPreferenceForm,
    preferredDayOptions,
    aiPreferencePresets,

    isLoadingProfileSettings,
    isSavingProfileSettings,
    isUploadingAvatarImage,
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
    uploadAvatarImage,
    changePassword,
    fetchTaskPreference,
    saveTaskPreference,
    togglePreferredDay,
    applyTaskPreferencePreset,
    resetTaskPreferenceToDefault,
  }
}
