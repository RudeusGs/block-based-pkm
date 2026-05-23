<template>
  <main class="invite-success-page">
    <router-link to="/" class="success-brand" aria-label="Go to home">
      <span class="material-symbols-outlined">widgets</span>
      <strong>Block Paged</strong>
    </router-link>

    <section class="success-shell" aria-live="polite">
      <div class="success-icon-wrap" :class="statusClass">
        <span class="material-symbols-outlined">
          {{ statusIcon }}
        </span>
      </div>

      <p class="success-kicker">{{ kicker }}</p>
      <h1>{{ title }}</h1>
      <p class="success-message">{{ message }}</p>

      <div v-if="member" class="success-details">
        <div>
          <span>Workspace</span>
          <strong>{{ member.workspaceId }}</strong>
        </div>
        <div>
          <span>Vai trò</span>
          <strong>{{ member.role }}</strong>
        </div>
      </div>

      <div class="success-actions">
        <button
          v-if="status === 'loading'"
          type="button"
          class="success-primary"
          disabled
        >
          <span class="spinner-border spinner-border-sm" aria-hidden="true"></span>
          Đang xác nhận
        </button>

        <button
          v-else-if="status === 'success'"
          type="button"
          class="success-primary"
          @click="goToApp"
        >
          <span class="material-symbols-outlined">arrow_forward</span>
          Vào workspace
        </button>

        <router-link
          v-else-if="errorAction === 'login'"
          class="success-primary"
          :to="loginTarget"
        >
          <span class="material-symbols-outlined">login</span>
          Đăng nhập để xác nhận
        </router-link>

        <router-link
          v-else-if="errorAction === 'register'"
          class="success-primary"
          :to="registerTarget"
        >
          <span class="material-symbols-outlined">person_add</span>
          Tạo tài khoản
        </router-link>

        <button
          v-else-if="errorAction === 'switch-account'"
          type="button"
          class="success-primary"
          @click="switchAccount"
        >
          <span class="material-symbols-outlined">account_circle</span>
          Đổi tài khoản
        </button>

        <button
          v-else
          type="button"
          class="success-primary"
          @click="retryAccept"
        >
          <span class="material-symbols-outlined">refresh</span>
          Thử lại
        </button>

        <router-link
          v-if="status === 'error' && errorAction !== 'login'"
          class="success-secondary"
          :to="loginTarget"
        >
          Đăng nhập
        </router-link>

        <router-link
          v-if="status === 'error' && errorAction !== 'register'"
          class="success-secondary"
          :to="registerTarget"
        >
          Tạo tài khoản
        </router-link>
      </div>
    </section>
  </main>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { workspaceController } from '@/api'
import type { WorkspaceMemberResponse } from '@/api/models/workspace.model'
import {
  getApiErrorMessage,
  getApiResultErrorMessage,
} from '@/api/utils/api-error.util'
import {
  clearAuthToken,
  isAuthenticated,
} from '@/modules/auth/utils/auth-token.util'

type InviteStatus = 'loading' | 'success' | 'error'
type InviteErrorAction = 'retry' | 'login' | 'register' | 'switch-account'

const route = useRoute()
const router = useRouter()

const status = ref<InviteStatus>('loading')
const message = ref('Đang kiểm tra lời mời workspace của bạn.')
const member = ref<WorkspaceMemberResponse | null>(null)
const errorAction = ref<InviteErrorAction>('retry')

const token = computed(() => {
  const rawToken = route.query.token

  if (Array.isArray(rawToken)) {
    return rawToken[0] ?? ''
  }

  return typeof rawToken === 'string' ? rawToken : ''
})

const currentInvitePath = computed(() => {
  return route.fullPath || '/success'
})

const loginTarget = computed(() => ({
  name: 'login',
  query: {
    redirect: status.value === 'success' ? '/app' : currentInvitePath.value,
  },
}))

const registerTarget = computed(() => ({
  name: 'register',
  query: {
    redirect: currentInvitePath.value,
  },
}))

const statusClass = computed(() => `is-${status.value}`)

const statusIcon = computed(() => {
  if (status.value === 'success') return 'check_circle'
  if (status.value === 'error') return 'error'

  return 'hourglass_top'
})

const kicker = computed(() => {
  if (status.value === 'success') return 'Lời mời đã được xác nhận'
  if (status.value === 'error' && errorAction.value === 'login') {
    return 'Cần đăng nhập'
  }

  if (status.value === 'error' && errorAction.value === 'switch-account') {
    return 'Sai tài khoản'
  }

  if (status.value === 'error' && errorAction.value === 'register') {
    return 'Cần tài khoản'
  }

  if (status.value === 'error') return 'Chưa thể xác nhận lời mời'

  return 'Đang xử lý lời mời'
})

const title = computed(() => {
  if (status.value === 'success') return 'Bạn đã tham gia workspace'
  if (status.value === 'error' && errorAction.value === 'login') {
    return 'Đăng nhập để xác nhận lời mời'
  }

  if (status.value === 'error' && errorAction.value === 'switch-account') {
    return 'Dùng đúng tài khoản được mời'
  }

  if (status.value === 'error' && errorAction.value === 'register') {
    return 'Tạo tài khoản để nhận lời mời'
  }

  if (status.value === 'error') return 'Link lời mời cần được kiểm tra lại'

  return 'Đang đưa bạn vào workspace'
})

function getErrorCode(error: unknown): string | null {
  const apiError = error as {
    data?: {
      error?: {
        code?: string
      } | null
    }
  }

  return apiError.data?.error?.code ?? null
}

function getActionFromErrorCode(
  errorCode: string | null,
  statusCode?: number
): InviteErrorAction {
  if (errorCode === 'Workspace.MissingUserContext' || statusCode === 401) {
    return 'login'
  }

  if (errorCode === 'Workspace.TargetUserNotFoundByEmail') {
    return 'register'
  }

  if (errorCode === 'Workspace.InvitationEmailMismatch') {
    return 'switch-account'
  }

  return 'retry'
}

function setError(
  errorMessage: string,
  action: InviteErrorAction = 'retry'
) {
  status.value = 'error'
  message.value = errorMessage
  member.value = null
  errorAction.value = action
}

async function acceptInvitation() {
  if (!token.value.trim()) {
    setError('Link lời mời không có token hợp lệ.')
    return
  }

  status.value = 'loading'
  message.value = 'Đang kiểm tra lời mời workspace của bạn.'
  member.value = null
  errorAction.value = 'retry'

  try {
    const response = await workspaceController.acceptInvitation(token.value)

    if (!response.isSuccess || !response.data) {
      const errorCode = response.error?.code ?? null

      setError(
        getApiResultErrorMessage(
          response,
          'Lời mời không hợp lệ hoặc đã hết hạn.'
        ),
        getActionFromErrorCode(errorCode, response.statusCode)
      )
      return
    }

    member.value = response.data
    status.value = 'success'
    message.value =
      'Workspace đã được thêm vào tài khoản của bạn. Bạn có thể mở ứng dụng để tiếp tục làm việc.'
  } catch (error) {
    const apiError = error as { status?: number }
    const errorCode = getErrorCode(error)

    setError(
      getApiErrorMessage(error, 'Lời mời không hợp lệ hoặc đã hết hạn.'),
      getActionFromErrorCode(errorCode, apiError.status)
    )
  }
}

function retryAccept() {
  void acceptInvitation()
}

async function goToApp() {
  if (isAuthenticated()) {
    await router.replace('/app')
    return
  }

  await router.push(loginTarget.value)
}

async function switchAccount() {
  clearAuthToken()
  await router.push(loginTarget.value)
}

onMounted(() => {
  void acceptInvitation()
})
</script>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&display=swap');
@import url('https://fonts.googleapis.com/css2?family=Material+Symbols+Outlined:wght,FILL@100..700,0..1&display=swap');

.invite-success-page {
  min-height: 100vh;
  display: grid;
  place-items: center;
  padding: 96px 20px 40px;
  background:
    linear-gradient(135deg, rgba(235, 246, 255, 0.86), rgba(255, 255, 255, 0.95)),
    #f7f9fb;
  color: #111827;
  font-family: 'Inter', sans-serif;
  position: relative;
}

.success-brand {
  position: absolute;
  top: 28px;
  left: 28px;
  display: inline-flex;
  align-items: center;
  gap: 10px;
  color: #111827;
  text-decoration: none;
}

.success-brand .material-symbols-outlined {
  width: 40px;
  height: 40px;
  display: inline-grid;
  place-items: center;
  border-radius: 12px;
  background: #111827;
  color: #ffffff;
}

.success-shell {
  width: min(100%, 560px);
  padding: 40px;
  border: 1px solid rgba(17, 24, 39, 0.08);
  border-radius: 24px;
  background: rgba(255, 255, 255, 0.9);
  box-shadow: 0 28px 90px rgba(15, 23, 42, 0.12);
  text-align: center;
}

.success-icon-wrap {
  width: 72px;
  height: 72px;
  display: inline-grid;
  place-items: center;
  border-radius: 24px;
  margin-bottom: 22px;
}

.success-icon-wrap .material-symbols-outlined {
  font-size: 42px;
  font-variation-settings: 'FILL' 1;
}

.success-icon-wrap.is-loading {
  background: #eff6ff;
  color: #2563eb;
}

.success-icon-wrap.is-success {
  background: #ecfdf5;
  color: #047857;
}

.success-icon-wrap.is-error {
  background: #fff1f2;
  color: #be123c;
}

.success-kicker {
  margin: 0 0 8px;
  color: #64748b;
  font-size: 0.76rem;
  font-weight: 800;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.success-shell h1 {
  margin: 0;
  font-size: clamp(2rem, 5vw, 3.4rem);
  line-height: 1;
  font-weight: 800;
  letter-spacing: 0;
}

.success-message {
  max-width: 460px;
  margin: 18px auto 0;
  color: #475569;
  line-height: 1.7;
}

.success-details {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
  margin-top: 26px;
  text-align: left;
}

.success-details div {
  min-width: 0;
  padding: 14px;
  border-radius: 14px;
  background: #f8fafc;
  border: 1px solid #e5e7eb;
}

.success-details span {
  display: block;
  margin-bottom: 6px;
  color: #64748b;
  font-size: 0.72rem;
  font-weight: 700;
  text-transform: uppercase;
}

.success-details strong {
  display: block;
  overflow-wrap: anywhere;
  color: #111827;
  font-size: 0.9rem;
}

.success-actions {
  display: flex;
  justify-content: center;
  align-items: center;
  flex-wrap: wrap;
  gap: 12px;
  margin-top: 30px;
}

.success-primary,
.success-secondary {
  min-height: 46px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  border-radius: 14px;
  padding: 0 18px;
  font-weight: 800;
  text-decoration: none;
  border: 0;
}

.success-primary {
  background: #111827;
  color: #ffffff;
}

.success-primary:disabled {
  opacity: 0.72;
}

.success-secondary {
  background: #eef2f7;
  color: #111827;
}

@media (max-width: 576px) {
  .invite-success-page {
    padding-top: 88px;
  }

  .success-brand {
    top: 20px;
    left: 20px;
  }

  .success-shell {
    padding: 30px 20px;
    border-radius: 20px;
  }

  .success-details {
    grid-template-columns: 1fr;
  }

  .success-primary,
  .success-secondary {
    width: 100%;
  }
}
</style>
