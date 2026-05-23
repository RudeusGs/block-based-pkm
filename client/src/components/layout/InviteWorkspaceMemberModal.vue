<template>
  <Teleport to="body">
    <Transition name="invite-modal-fade">
      <div
        v-if="open"
        class="invite-modal-layer"
        role="presentation"
      >
        <div
          class="invite-modal-backdrop"
          @click="emit('close')"
        ></div>

        <section
          class="invite-modal"
          role="dialog"
          aria-modal="true"
          aria-label="Mời thành viên"
          @click.stop
        >
          <header class="invite-modal-header">
            <div>
              <span>Workspace</span>
              <h2>Mời thành viên</h2>
            </div>

            <button
              type="button"
              class="invite-icon-btn"
              aria-label="Đóng"
              @click="emit('close')"
            >
              ×
            </button>
          </header>

          <main class="invite-modal-body">
            <p class="invite-description">
              Nhập Gmail của thành viên bạn muốn mời vào workspace
              <strong>{{ workspaceName }}</strong>. Người được mời sẽ nhận email xác nhận.
            </p>

            <label class="invite-field">
              <span>Email</span>

              <input
                v-model="emailModel"
                type="email"
                placeholder="name@gmail.com"
                autocomplete="email"
                @keydown.enter="emit('submit')"
              />
            </label>

            <div class="invite-field">
              <span>Quyền truy cập</span>

              <div class="invite-role-grid">
                <button
                  v-for="option in roleOptions"
                  :key="option.value"
                  type="button"
                  class="invite-role-card"
                  :class="{ active: roleModel === option.value }"
                  @click="roleModel = option.value"
                >
                  <strong>{{ option.label }}</strong>
                  <small>{{ option.description }}</small>
                </button>
              </div>
            </div>

            <p
              v-if="error"
              class="invite-error"
            >
              {{ error }}
            </p>
          </main>

          <footer class="invite-modal-footer">
            <button
              type="button"
              class="invite-ghost-btn"
              @click="emit('close')"
            >
              Hủy
            </button>

            <button
              type="button"
              class="invite-primary-btn"
              :disabled="!canSubmit || isSubmitting"
              @click="emit('submit')"
            >
              {{ isSubmitting ? 'Đang gửi...' : 'Gửi lời mời' }}
            </button>
          </footer>
        </section>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { WorkspaceRoleValue } from '@/api/models/workspace.model'

const props = defineProps<{
  open: boolean
  workspaceName: string
  email: string
  role: WorkspaceRoleValue
  error: string | null
  canSubmit: boolean
  isSubmitting: boolean
}>()

const emit = defineEmits<{
  close: []
  submit: []
  'update:email': [value: string]
  'update:role': [value: WorkspaceRoleValue]
}>()

const emailModel = computed({
  get: () => props.email,
  set: (value: string) => emit('update:email', value),
})

const roleModel = computed({
  get: () => props.role,
  set: (value: WorkspaceRoleValue) => emit('update:role', value),
})

const roleOptions: Array<{
  value: WorkspaceRoleValue
  label: string
  description: string
}> = [
  {
    value: 'manager',
    label: 'Manager',
    description: 'Quản lý nội dung và thành viên.',
  },
  {
    value: 'member',
    label: 'Member',
    description: 'Tạo, chỉnh sửa và làm việc chung.',
  },
  {
    value: 'viewer',
    label: 'Viewer',
    description: 'Chỉ xem nội dung workspace.',
  },
]
</script>

<style scoped>
.invite-modal-layer {
  position: fixed;
  inset: 0;
  z-index: 1000;
  display: grid;
  place-items: center;
  padding: 20px;
}

.invite-modal-backdrop {
  position: absolute;
  inset: 0;
  background: rgba(0, 0, 0, 0.34);
}

.invite-modal {
  position: relative;
  z-index: 1;
  width: min(460px, calc(100vw - 32px));
  overflow: hidden;
  border: 1px solid #2f2f2f;
  border-radius: 12px;
  color: #e7e7e7;
  background: #191919;
  box-shadow:
    0 24px 80px rgba(0, 0, 0, 0.46),
    0 8px 28px rgba(0, 0, 0, 0.28);
}

.invite-modal-header {
  min-height: 62px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 14px 16px;
  border-bottom: 1px solid #2b2b2b;
}

.invite-modal-header span {
  display: block;
  color: #858585;
  font-size: 11px;
  font-weight: 600;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.invite-modal-header h2 {
  margin: 2px 0 0;
  color: #f1f1f1;
  font-size: 16px;
  font-weight: 650;
  line-height: 1.25;
}

.invite-icon-btn {
  width: 30px;
  height: 30px;
  border: 0;
  border-radius: 6px;
  color: #9b9b9b;
  background: transparent;
  font-size: 22px;
  line-height: 1;
  cursor: pointer;
}

.invite-icon-btn:hover {
  color: #f1f1f1;
  background: #252525;
}

.invite-modal-body {
  padding: 16px;
}

.invite-description {
  margin: 0 0 16px;
  color: #9b9b9b;
  font-size: 13px;
  line-height: 1.5;
}

.invite-description strong {
  color: #d8d8d8;
  font-weight: 600;
}

.invite-field {
  display: flex;
  flex-direction: column;
  gap: 7px;
  margin-top: 14px;
}

.invite-field > span {
  color: #cfcfcf;
  font-size: 12.5px;
  font-weight: 600;
}

.invite-field input {
  width: 100%;
  min-height: 36px;
  border: 1px solid #343434;
  border-radius: 7px;
  padding: 0 10px;
  color: #f1f1f1;
  background: #202020;
  font-size: 13px;
  outline: none;
}

.invite-field input::placeholder {
  color: #707070;
}

.invite-field input:focus {
  border-color: #5a5a5a;
  background: #222222;
}

.invite-role-grid {
  display: grid;
  gap: 8px;
}

.invite-role-card {
  border: 1px solid #303030;
  border-radius: 8px;
  display: flex;
  flex-direction: column;
  gap: 3px;
  padding: 10px;
  color: #bdbdbd;
  background: #202020;
  text-align: left;
  cursor: pointer;
}

.invite-role-card:hover {
  border-color: #454545;
  background: #242424;
}

.invite-role-card.active {
  border-color: #6b6b6b;
  background: #292929;
}

.invite-role-card strong {
  color: #f1f1f1;
  font-size: 13px;
  font-weight: 650;
}

.invite-role-card small {
  color: #8f8f8f;
  font-size: 12px;
  line-height: 1.35;
}

.invite-error {
  margin: 14px 0 0;
  border: 1px solid rgba(248, 113, 113, 0.32);
  border-radius: 7px;
  padding: 9px 10px;
  color: #fca5a5;
  background: rgba(127, 29, 29, 0.16);
  font-size: 12.5px;
  line-height: 1.4;
}

.invite-modal-footer {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  padding: 12px 16px;
  border-top: 1px solid #2b2b2b;
}

.invite-ghost-btn,
.invite-primary-btn {
  min-height: 32px;
  border-radius: 6px;
  padding: 0 11px;
  font-size: 12.5px;
  font-weight: 560;
  cursor: pointer;
}

.invite-ghost-btn {
  border: 1px solid transparent;
  color: #a3a3a3;
  background: transparent;
}

.invite-ghost-btn:hover {
  color: #f1f1f1;
  background: #252525;
  border-color: #303030;
}

.invite-primary-btn {
  border: 1px solid #f1f1f1;
  color: #111111;
  background: #f1f1f1;
}

.invite-primary-btn:hover:not(:disabled) {
  background: #ffffff;
}

.invite-primary-btn:disabled {
  cursor: not-allowed;
  opacity: 0.5;
}

.invite-modal-fade-enter-active,
.invite-modal-fade-leave-active {
  transition: opacity 150ms ease;
}

.invite-modal-fade-enter-from,
.invite-modal-fade-leave-to {
  opacity: 0;
}

@media (max-width: 576px) {
  .invite-modal-layer {
    align-items: end;
    padding: 12px;
  }

  .invite-modal {
    width: 100%;
  }

  .invite-modal-footer {
    flex-direction: column-reverse;
  }

  .invite-ghost-btn,
  .invite-primary-btn {
    width: 100%;
  }
}
</style>
