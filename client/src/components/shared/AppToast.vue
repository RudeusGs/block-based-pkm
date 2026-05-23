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
          <div class="toast-marker" aria-hidden="true"></div>

          <div class="toast-content">
            <div class="toast-title">
              {{ toast.title }}
            </div>

            <div
              v-if="toast.message"
              class="toast-message"
            >
              {{ toast.message }}
            </div>
          </div>

          <button
            type="button"
            class="toast-close"
            aria-label="Close notification"
            @click="removeToast(toast.id)"
          >
            ×
          </button>
        </div>
      </TransitionGroup>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { useToast } from '@/components/composables/useToast'

const { toasts, removeToast } = useToast()
</script>

<style scoped>
.toast-stack {
  position: fixed;
  right: 24px;
  bottom: 24px;
  z-index: 2147483647;
  width: min(380px, calc(100vw - 32px));
  pointer-events: none;
}

.app-toast {
  pointer-events: auto;
  position: relative;
  display: flex;
  align-items: flex-start;
  gap: 10px;
  width: 100%;
  margin-top: 10px;
  padding: 12px 12px 12px 13px;
  overflow: hidden;

  border: 1px solid #2f2f2f;
  border-radius: 8px;

  background: #191919;
  color: #e6e6e6;

  box-shadow:
    0 12px 32px rgba(0, 0, 0, 0.28),
    0 2px 8px rgba(0, 0, 0, 0.2);
}

.toast-marker {
  width: 6px;
  height: 6px;
  margin-top: 7px;
  border-radius: 999px;
  flex-shrink: 0;
  background: #8b8b8b;
}

.toast-content {
  min-width: 0;
  flex: 1;
}

.toast-title {
  color: #f1f1f1;
  font-size: 13.5px;
  font-weight: 600;
  line-height: 1.35;
  letter-spacing: -0.01em;
}

.toast-message {
  margin-top: 3px;
  color: #9b9b9b;
  font-size: 12.5px;
  line-height: 1.45;
}

.toast-close {
  width: 24px;
  height: 24px;
  border: 0;
  border-radius: 5px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;

  color: #8b8b8b;
  background: transparent;

  font-size: 18px;
  line-height: 1;
  cursor: pointer;
}

.toast-close:hover {
  color: #e6e6e6;
  background: #252525;
}

/* Notion-style soft status colors */
.app-toast-success {
  border-color: rgba(59, 130, 96, 0.38);
  background: #19211c;
}

.app-toast-success .toast-marker {
  background: #4ade80;
}

.app-toast-error {
  border-color: rgba(190, 80, 80, 0.42);
  background: #241a1a;
}

.app-toast-error .toast-marker {
  background: #f87171;
}

.app-toast-warning {
  border-color: rgba(180, 130, 48, 0.42);
  background: #231f17;
}

.app-toast-warning .toast-marker {
  background: #fbbf24;
}

.app-toast-info {
  border-color: rgba(84, 124, 190, 0.42);
  background: #181e27;
}

.app-toast-info .toast-marker {
  background: #60a5fa;
}

.toast-slide-enter-active {
  transition:
    transform 180ms ease,
    opacity 160ms ease;
}

.toast-slide-leave-active {
  position: absolute;
  right: 0;
  transition:
    transform 150ms ease,
    opacity 140ms ease;
}

.toast-slide-enter-from {
  opacity: 0;
  transform: translateX(18px);
}

.toast-slide-leave-to {
  opacity: 0;
  transform: translateX(18px);
}

.toast-slide-move {
  transition: transform 180ms ease;
}

@media (max-width: 576px) {
  .toast-stack {
    right: 16px;
    bottom: 16px;
    width: calc(100vw - 32px);
  }

  .app-toast {
    padding: 11px 11px 11px 12px;
    border-radius: 8px;
  }
}

@media (prefers-reduced-motion: reduce) {
  .toast-slide-enter-active,
  .toast-slide-leave-active,
  .toast-slide-move {
    transition: none;
  }
}
</style>
