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
  padding: 92px 20px 42px;
  background:
    radial-gradient(circle at top, rgba(255, 255, 255, 0.9), transparent 34%),
    #f7f6f3;
  color: #2f3437;
  font-family:
    'Inter',
    ui-sans-serif,
    system-ui,
    -apple-system,
    BlinkMacSystemFont,
    'Segoe UI',
    sans-serif;
  position: relative;
}

.invite-success-page::before {
  content: '';
  position: fixed;
  inset: 0;
  pointer-events: none;
  background-image:
    linear-gradient(rgba(55, 53, 47, 0.025) 1px, transparent 1px),
    linear-gradient(90deg, rgba(55, 53, 47, 0.025) 1px, transparent 1px);
  background-size: 42px 42px;
  mask-image: linear-gradient(to bottom, rgba(0, 0, 0, 0.52), transparent 72%);
}

.success-brand {
  position: absolute;
  top: 26px;
  left: 28px;
  z-index: 2;
  display: inline-flex;
  align-items: center;
  gap: 10px;
  min-height: 38px;
  padding: 5px 9px 5px 6px;
  border-radius: 10px;
  color: #37352f;
  text-decoration: none;
  transition:
    background-color 0.16s ease,
    color 0.16s ease;
}

.success-brand:hover {
  background: rgba(55, 53, 47, 0.08);
  color: #111111;
}

.success-brand .material-symbols-outlined {
  width: 30px;
  height: 30px;
  display: inline-grid;
  place-items: center;
  border-radius: 8px;
  background: #37352f;
  color: #ffffff;
  font-size: 19px;
  font-variation-settings: 'FILL' 0;
}

.success-brand strong {
  font-size: 0.92rem;
  font-weight: 650;
  letter-spacing: -0.01em;
}

.success-shell {
  position: relative;
  z-index: 1;
  width: min(100%, 560px);
  padding: 34px;
  border: 1px solid rgba(55, 53, 47, 0.1);
  border-radius: 18px;
  background: rgba(255, 255, 255, 0.82);
  box-shadow:
    0 1px 2px rgba(15, 15, 15, 0.04),
    0 14px 40px rgba(15, 15, 15, 0.08);
  text-align: center;
  backdrop-filter: blur(18px);
}

.success-shell::after {
  content: '';
  position: absolute;
  inset: 0;
  border-radius: inherit;
  pointer-events: none;
  box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.72);
}

.success-icon-wrap {
  width: 58px;
  height: 58px;
  display: inline-grid;
  place-items: center;
  border-radius: 16px;
  margin-bottom: 20px;
  border: 1px solid rgba(55, 53, 47, 0.1);
  background: #f1f1ef;
  color: #37352f;
}

.success-icon-wrap .material-symbols-outlined {
  font-size: 32px;
  font-variation-settings: 'FILL' 0;
}

.success-icon-wrap.is-loading {
  background: #f1f1ef;
  color: #787774;
}

.success-icon-wrap.is-success {
  background: #eef3ed;
  color: #4d6b53;
}

.success-icon-wrap.is-error {
  background: #f6eeee;
  color: #8f4a4a;
}

.success-icon-wrap.is-loading .material-symbols-outlined {
  animation: success-hourglass 1.1s ease-in-out infinite;
}

@keyframes success-hourglass {
  0%,
  100% {
    transform: rotate(0deg);
    opacity: 0.72;
  }

  50% {
    transform: rotate(180deg);
    opacity: 1;
  }
}

.success-kicker {
  margin: 0 0 9px;
  color: #787774;
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.075em;
  text-transform: uppercase;
}

.success-shell h1 {
  max-width: 480px;
  margin: 0 auto;
  color: #2f3437;
  font-size: clamp(1.75rem, 4.2vw, 2.8rem);
  line-height: 1.08;
  font-weight: 750;
  letter-spacing: -0.045em;
}

.success-message {
  max-width: 455px;
  margin: 16px auto 0;
  color: #5f5e5b;
  font-size: 0.96rem;
  line-height: 1.65;
}

.success-details {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 10px;
  margin-top: 24px;
  text-align: left;
}

.success-details div {
  min-width: 0;
  padding: 13px 14px;
  border-radius: 12px;
  background: #f7f7f5;
  border: 1px solid rgba(55, 53, 47, 0.09);
  box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.72);
}

.success-details span {
  display: block;
  margin-bottom: 6px;
  color: #8a8986;
  font-size: 0.69rem;
  font-weight: 700;
  letter-spacing: 0.055em;
  text-transform: uppercase;
}

.success-details strong {
  display: block;
  overflow-wrap: anywhere;
  color: #37352f;
  font-size: 0.88rem;
  font-weight: 650;
  line-height: 1.45;
}

.success-actions {
  display: flex;
  justify-content: center;
  align-items: center;
  flex-wrap: wrap;
  gap: 10px;
  margin-top: 28px;
}

.success-primary,
.success-secondary {
  min-height: 40px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 7px;
  border-radius: 9px;
  padding: 0 14px;
  font-size: 0.9rem;
  font-weight: 650;
  line-height: 1;
  text-decoration: none;
  border: 1px solid transparent;
  cursor: pointer;
  transition:
    background-color 0.16s ease,
    border-color 0.16s ease,
    box-shadow 0.16s ease,
    transform 0.16s ease,
    color 0.16s ease,
    opacity 0.16s ease;
}

.success-primary .material-symbols-outlined,
.success-secondary .material-symbols-outlined {
  font-size: 19px;
  font-variation-settings: 'FILL' 0;
}

.success-primary {
  background: #37352f;
  color: #ffffff;
  border-color: #37352f;
  box-shadow:
    0 1px 1px rgba(15, 15, 15, 0.08),
    0 6px 16px rgba(15, 15, 15, 0.12);
}

.success-primary:hover:not(:disabled) {
  background: #2f2e2a;
  border-color: #2f2e2a;
  color: #ffffff;
  transform: translateY(-1px);
}

.success-primary:active:not(:disabled) {
  transform: translateY(0);
  box-shadow: 0 1px 1px rgba(15, 15, 15, 0.08);
}

.success-primary:disabled {
  cursor: not-allowed;
  opacity: 0.72;
  transform: none;
  box-shadow: none;
}

.success-secondary {
  background: transparent;
  color: #5f5e5b;
  border-color: rgba(55, 53, 47, 0.13);
}

.success-secondary:hover {
  background: rgba(55, 53, 47, 0.06);
  color: #37352f;
  border-color: rgba(55, 53, 47, 0.18);
}

.success-primary:focus-visible,
.success-secondary:focus-visible,
.success-brand:focus-visible {
  outline: none;
  box-shadow:
    0 0 0 2px #ffffff,
    0 0 0 4px rgba(55, 53, 47, 0.32);
}

.spinner-border {
  width: 0.9rem;
  height: 0.9rem;
  border-width: 0.14em;
}

@media (max-width: 576px) {
  .invite-success-page {
    align-items: start;
    padding: 86px 14px 28px;
  }

  .success-brand {
    top: 18px;
    left: 14px;
  }

  .success-shell {
    padding: 28px 18px;
    border-radius: 16px;
  }

  .success-icon-wrap {
    width: 54px;
    height: 54px;
    border-radius: 14px;
  }

  .success-shell h1 {
    font-size: 1.72rem;
    letter-spacing: -0.035em;
  }

  .success-message {
    font-size: 0.92rem;
  }

  .success-details {
    grid-template-columns: 1fr;
  }

  .success-actions {
    align-items: stretch;
    flex-direction: column;
  }

  .success-primary,
  .success-secondary {
    width: 100%;
  }
}

@media (prefers-color-scheme: dark) {
  .invite-success-page {
    background:
      radial-gradient(circle at top, rgba(43, 43, 40, 0.88), transparent 34%),
      #191918;
    color: #e6e6e3;
  }

  .invite-success-page::before {
    background-image:
      linear-gradient(rgba(255, 255, 255, 0.035) 1px, transparent 1px),
      linear-gradient(90deg, rgba(255, 255, 255, 0.035) 1px, transparent 1px);
  }

  .success-brand {
    color: #e6e6e3;
  }

  .success-brand:hover {
    background: rgba(255, 255, 255, 0.08);
    color: #ffffff;
  }

  .success-brand .material-symbols-outlined {
    background: #e6e6e3;
    color: #191918;
  }

  .success-shell {
    background: rgba(32, 32, 30, 0.88);
    border-color: rgba(255, 255, 255, 0.1);
    box-shadow:
      0 1px 2px rgba(0, 0, 0, 0.22),
      0 18px 50px rgba(0, 0, 0, 0.35);
  }

  .success-shell::after {
    box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.06);
  }

  .success-icon-wrap {
    background: #2b2b28;
    color: #e6e6e3;
    border-color: rgba(255, 255, 255, 0.1);
  }

  .success-icon-wrap.is-loading {
    background: #2b2b28;
    color: #b9b8b3;
  }

  .success-icon-wrap.is-success {
    background: rgba(77, 107, 83, 0.22);
    color: #a7c3ac;
  }

  .success-icon-wrap.is-error {
    background: rgba(143, 74, 74, 0.2);
    color: #d2a0a0;
  }

  .success-kicker {
    color: #a8a8a2;
  }

  .success-shell h1 {
    color: #f1f1ef;
  }

  .success-message {
    color: #b9b8b3;
  }

  .success-details div {
    background: rgba(255, 255, 255, 0.04);
    border-color: rgba(255, 255, 255, 0.09);
    box-shadow: none;
  }

  .success-details span {
    color: #a8a8a2;
  }

  .success-details strong {
    color: #f1f1ef;
  }

  .success-primary {
    background: #f1f1ef;
    color: #191918;
    border-color: #f1f1ef;
    box-shadow: none;
  }

  .success-primary:hover:not(:disabled) {
    background: #ffffff;
    border-color: #ffffff;
    color: #191918;
  }

  .success-secondary {
    color: #d6d6d1;
    border-color: rgba(255, 255, 255, 0.12);
  }

  .success-secondary:hover {
    background: rgba(255, 255, 255, 0.08);
    color: #ffffff;
    border-color: rgba(255, 255, 255, 0.18);
  }

  .success-primary:focus-visible,
  .success-secondary:focus-visible,
  .success-brand:focus-visible {
    box-shadow:
      0 0 0 2px #191918,
      0 0 0 4px rgba(241, 241, 239, 0.4);
  }
}
</style>
