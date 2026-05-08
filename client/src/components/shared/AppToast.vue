<template>
  <Teleport to="body">
    <div class="toast-stack">
      <TransitionGroup name="toast-slide">
        <div
          v-for="toast in toasts"
          :key="toast.id"
          class="app-toast"
          :class="`app-toast-${toast.type}`"
        >
          <div class="toast-icon-wrap">
            <span class="material-symbols-outlined toast-icon">
              {{ getIcon(toast.type) }}
            </span>
          </div>

          <div class="toast-content">
            <div class="toast-title">
              {{ toast.title }}
            </div>

            <div v-if="toast.message" class="toast-message">
              {{ toast.message }}
            </div>
          </div>

          <button
            type="button"
            class="toast-close"
            aria-label="Close notification"
            @click="removeToast(toast.id)"
          >
            <span class="material-symbols-outlined">close</span>
          </button>
        </div>
      </TransitionGroup>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { useToast, type ToastType } from '@/components/composables/useToast'

const { toasts, removeToast } = useToast()

const getIcon = (type: ToastType) => {
  switch (type) {
    case 'success':
      return 'check_circle'
    case 'error':
      return 'error'
    case 'warning':
      return 'warning'
    case 'info':
      return 'info'
    default:
      return 'notifications'
  }
}
</script>

<style scoped>
.toast-stack {
  position: fixed;
  right: 28px;
  bottom: 28px;
  z-index: 2147483647;
  width: min(420px, calc(100vw - 32px));
  pointer-events: none;
}

.app-toast {
  pointer-events: auto;
  display: flex;
  align-items: flex-start;
  gap: 14px;
  width: 100%;
  padding: 16px 16px 16px 14px;
  margin-top: 12px;
  position: relative;
  overflow: hidden;

  border-radius: 22px;
  border: 1px solid rgba(255, 255, 255, 0.1);

  background:
    linear-gradient(
      135deg,
      rgba(25, 25, 28, 0.96),
      rgba(12, 12, 14, 0.96)
    );

  box-shadow:
    0 24px 80px rgba(0, 0, 0, 0.34),
    0 10px 28px rgba(0, 0, 0, 0.24),
    inset 0 1px 0 rgba(255, 255, 255, 0.08);

  color: #f8fafc;
  backdrop-filter: blur(18px);
}

.app-toast::before {
  content: '';
  position: absolute;
  inset: 12px auto 12px 0;
  width: 4px;
  border-radius: 0 999px 999px 0;
  background: #a3a3a3;
}

.app-toast::after {
  content: '';
  position: absolute;
  inset: 0;
  pointer-events: none;
  background:
    radial-gradient(
      circle at top left,
      rgba(255, 255, 255, 0.1),
      transparent 36%
    );
  opacity: 0.75;
}

.toast-icon-wrap {
  width: 38px;
  height: 38px;
  border-radius: 16px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;

  background: rgba(255, 255, 255, 0.08);
  box-shadow:
    inset 0 1px 0 rgba(255, 255, 255, 0.08),
    0 10px 24px rgba(0, 0, 0, 0.18);
}

.toast-icon {
  font-size: 22px;
  font-variation-settings: 'FILL' 1;
}

.toast-content {
  flex: 1;
  min-width: 0;
  padding-top: 1px;
  position: relative;
  z-index: 1;
}

.toast-title {
  font-size: 14px;
  line-height: 1.35;
  font-weight: 800;
  letter-spacing: -0.01em;
  color: #ffffff;
}

.toast-message {
  margin-top: 4px;
  font-size: 13px;
  line-height: 1.5;
  color: rgba(226, 232, 240, 0.72);
}

.toast-close {
  position: relative;
  z-index: 1;
  border: 0;
  background: rgba(255, 255, 255, 0.04);
  color: rgba(226, 232, 240, 0.56);
  width: 30px;
  height: 30px;
  border-radius: 999px;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0;
  transition:
    background 0.18s ease,
    color 0.18s ease,
    transform 0.18s ease;
  flex-shrink: 0;
}

.toast-close:hover {
  background: rgba(255, 255, 255, 0.1);
  color: #ffffff;
  transform: rotate(90deg);
}

.toast-close .material-symbols-outlined {
  font-size: 18px;
}

.app-toast-success::before {
  background: linear-gradient(180deg, #22c55e, #14b8a6);
}

.app-toast-success .toast-icon-wrap {
  background: rgba(34, 197, 94, 0.14);
  color: #4ade80;
}

.app-toast-error::before {
  background: linear-gradient(180deg, #fb7185, #ef4444);
}

.app-toast-error .toast-icon-wrap {
  background: rgba(239, 68, 68, 0.15);
  color: #fb7185;
}

.app-toast-warning::before {
  background: linear-gradient(180deg, #fbbf24, #f97316);
}

.app-toast-warning .toast-icon-wrap {
  background: rgba(245, 158, 11, 0.16);
  color: #fbbf24;
}

.app-toast-info::before {
  background: linear-gradient(180deg, #60a5fa, #8b5cf6);
}

.app-toast-info .toast-icon-wrap {
  background: rgba(96, 165, 250, 0.15);
  color: #93c5fd;
}

.toast-slide-enter-active {
  transition:
    transform 0.44s cubic-bezier(0.16, 1, 0.3, 1),
    opacity 0.28s ease,
    filter 0.28s ease;
}

.toast-slide-leave-active {
  position: absolute;
  right: 0;
  transition:
    transform 0.24s ease,
    opacity 0.2s ease,
    filter 0.2s ease;
}

.toast-slide-enter-from {
  opacity: 0;
  filter: blur(8px);
  transform: translateX(120%) translateY(16px) scale(0.94);
}

.toast-slide-leave-to {
  opacity: 0;
  filter: blur(6px);
  transform: translateX(120%) translateY(8px) scale(0.96);
}

.toast-slide-move {
  transition: transform 0.28s cubic-bezier(0.16, 1, 0.3, 1);
}

@media (max-width: 576px) {
  .toast-stack {
    right: 16px;
    bottom: 16px;
    width: calc(100vw - 32px);
  }

  .app-toast {
    border-radius: 18px;
    padding: 14px;
  }

  .toast-icon-wrap {
    width: 36px;
    height: 36px;
    border-radius: 14px;
  }
}
</style>