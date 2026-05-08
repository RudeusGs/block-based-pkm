import { reactive } from 'vue'

export type ToastType = 'success' | 'error' | 'warning' | 'info'

export interface ToastItem {
  id: number
  type: ToastType
  title: string
  message?: string
  duration: number
}

const toasts = reactive<ToastItem[]>([])

let toastId = 0

const removeToast = (id: number) => {
  const index = toasts.findIndex((toast) => toast.id === id)

  if (index !== -1) {
    toasts.splice(index, 1)
  }
}

const showToast = (payload: {
  type: ToastType
  title: string
  message?: string
  duration?: number
}) => {
  const id = ++toastId

  const toast: ToastItem = {
    id,
    type: payload.type,
    title: payload.title,
    message: payload.message,
    duration: payload.duration ?? 4000,
  }

  toasts.push(toast)

  if (toast.duration > 0) {
    window.setTimeout(() => {
      removeToast(id)
    }, toast.duration)
  }

  return id
}

export function useToast() {
  return {
    toasts,
    removeToast,

    success: (title: string, message?: string, duration?: number) =>
      showToast({
        type: 'success',
        title,
        message,
        duration,
      }),

    error: (title: string, message?: string, duration?: number) =>
      showToast({
        type: 'error',
        title,
        message,
        duration,
      }),

    warning: (title: string, message?: string, duration?: number) =>
      showToast({
        type: 'warning',
        title,
        message,
        duration,
      }),

    info: (title: string, message?: string, duration?: number) =>
      showToast({
        type: 'info',
        title,
        message,
        duration,
      }),
  }
}