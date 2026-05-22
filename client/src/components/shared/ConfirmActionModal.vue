<template>
  <Teleport to="body">
    <Transition name="confirm-action-fade">
      <div
        v-if="open"
        class="confirm-action-layer"
        role="presentation"
        @click.self="handleClose"
      >
        <section
          class="confirm-action-card"
          role="dialog"
          aria-modal="true"
          :aria-label="title"
          @click.stop
        >
          <div
            class="confirm-action-icon"
            :class="variant"
          >
            <i :class="iconClass"></i>
          </div>

          <div class="confirm-action-copy">
            <h2>{{ title }}</h2>
            <p>{{ message }}</p>

            <p
              v-if="description"
              class="confirm-action-description"
            >
              {{ description }}
            </p>

            <div
              v-if="error"
              class="confirm-action-error"
            >
              <i class="bi bi-exclamation-triangle"></i>
              <span>{{ error }}</span>
            </div>
          </div>

          <footer class="confirm-action-footer">
            <button
              type="button"
              class="confirm-action-btn ghost"
              :disabled="isSubmitting"
              @click="handleClose"
            >
              {{ cancelLabel }}
            </button>

            <button
              type="button"
              class="confirm-action-btn"
              :class="variant"
              :disabled="isSubmitting"
              @click="emit('confirm')"
            >
              <span
                v-if="isSubmitting"
                class="confirm-action-spinner"
              ></span>

              <span>{{ isSubmitting ? submittingLabel : confirmLabel }}</span>
            </button>
          </footer>
        </section>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, watch } from 'vue'

const props = withDefaults(
  defineProps<{
    open: boolean
    title: string
    message: string
    description?: string | null
    confirmLabel?: string
    submittingLabel?: string
    cancelLabel?: string
    variant?: 'danger' | 'warning'
    isSubmitting?: boolean
    error?: string | null
  }>(),
  {
    description: null,
    confirmLabel: 'Xác nhận',
    submittingLabel: 'Đang xử lý...',
    cancelLabel: 'Hủy',
    variant: 'danger',
    isSubmitting: false,
    error: null,
  }
)

const emit = defineEmits<{
  close: []
  confirm: []
}>()

const iconClass = computed(() => {
  return props.variant === 'warning'
    ? 'bi bi-exclamation-circle'
    : 'bi bi-trash3'
})

function handleClose() {
  if (props.isSubmitting) return
  emit('close')
}

function handleKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape') {
    handleClose()
  }
}

watch(
  () => props.open,
  (open) => {
    document.body.classList.toggle('confirm-action-scroll-lock', open)
  },
  { immediate: true }
)

onMounted(() => {
  window.addEventListener('keydown', handleKeydown)
})

onBeforeUnmount(() => {
  document.body.classList.remove('confirm-action-scroll-lock')
  window.removeEventListener('keydown', handleKeydown)
})
</script>

<style scoped>
:global(.confirm-action-scroll-lock) {
  overflow: hidden;
}

.confirm-action-layer {
  position: fixed;
  inset: 0;
  z-index: 2600;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 24px;
  color: #f1f1f1;
  background: rgba(0, 0, 0, 0.54);
  backdrop-filter: blur(10px);
}

.confirm-action-card {
  width: min(430px, calc(100vw - 32px));
  border: 1px solid #303030;
  border-radius: 18px;
  display: grid;
  grid-template-columns: auto minmax(0, 1fr);
  gap: 14px;
  padding: 18px;
  background: #171717;
  box-shadow: 0 24px 86px rgba(0, 0, 0, 0.55);
}

.confirm-action-icon {
  width: 42px;
  height: 42px;
  border-radius: 14px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  font-size: 18px;
}

.confirm-action-icon.danger {
  color: #ffb4b4;
  background: rgba(255, 87, 87, 0.13);
}

.confirm-action-icon.warning {
  color: #f7d58b;
  background: rgba(247, 213, 139, 0.12);
}

.confirm-action-copy {
  min-width: 0;
}

.confirm-action-copy h2 {
  margin: 0;
  color: #f5f5f5;
  font-size: 17px;
  font-weight: 760;
  letter-spacing: -0.02em;
}

.confirm-action-copy p {
  margin: 7px 0 0;
  color: #a3a3a3;
  font-size: 13px;
  line-height: 1.55;
}

.confirm-action-description {
  color: #7f7f7f !important;
}

.confirm-action-error {
  margin-top: 12px;
  border: 1px solid rgba(255, 111, 111, 0.28);
  border-radius: 11px;
  display: flex;
  align-items: flex-start;
  gap: 8px;
  padding: 8px 10px;
  color: #ffb4b4;
  background: rgba(255, 83, 83, 0.08);
  font-size: 12.5px;
  line-height: 1.45;
}

.confirm-action-footer {
  grid-column: 1 / -1;
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  padding-top: 4px;
}

.confirm-action-btn {
  min-height: 34px;
  border: 0;
  border-radius: 9px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 0 12px;
  color: #111;
  background: #f1f1f1;
  font-size: 13px;
  font-weight: 720;
  transition:
    transform 140ms ease,
    opacity 140ms ease,
    background-color 140ms ease;
}

.confirm-action-btn:hover:not(:disabled) {
  transform: translateY(-1px);
}

.confirm-action-btn:disabled {
  opacity: 0.62;
  cursor: not-allowed;
}

.confirm-action-btn.ghost {
  color: #d4d4d4;
  background: #242424;
}

.confirm-action-btn.danger {
  color: #210707;
  background: #ffb4b4;
}

.confirm-action-btn.warning {
  color: #211706;
  background: #f7d58b;
}

.confirm-action-spinner {
  width: 13px;
  height: 13px;
  border: 2px solid currentColor;
  border-top-color: transparent;
  border-radius: 999px;
  animation: confirm-action-spin 760ms linear infinite;
}

.confirm-action-fade-enter-active,
.confirm-action-fade-leave-active {
  transition: opacity 160ms ease;
}

.confirm-action-fade-enter-active .confirm-action-card,
.confirm-action-fade-leave-active .confirm-action-card {
  transition: transform 160ms ease;
}

.confirm-action-fade-enter-from,
.confirm-action-fade-leave-to {
  opacity: 0;
}

.confirm-action-fade-enter-from .confirm-action-card,
.confirm-action-fade-leave-to .confirm-action-card {
  transform: translateY(8px) scale(0.98);
}

@keyframes confirm-action-spin {
  to {
    transform: rotate(360deg);
  }
}

@media (prefers-reduced-motion: reduce) {
  .confirm-action-fade-enter-active,
  .confirm-action-fade-leave-active,
  .confirm-action-fade-enter-active .confirm-action-card,
  .confirm-action-fade-leave-active .confirm-action-card,
  .confirm-action-btn {
    transition: none;
  }

  .confirm-action-spinner {
    animation: none;
  }
}
</style>
