import { ref } from 'vue'

export type ToastType = 'success' | 'info' | 'warning' | 'error'

export interface Toast {
  id: number
  type: ToastType
  title: string
  message?: string
}

const toasts = ref<Toast[]>([])
let counter = 0

function add(type: ToastType, title: string, message?: string, duration = 4000) {
  const id = ++counter
  toasts.value.push({ id, type, title, message })
  setTimeout(() => dismiss(id), duration)
}

function dismiss(id: number) {
  toasts.value = toasts.value.filter(t => t.id !== id)
}

export function useToast() {
  return {
    toasts,
    dismiss,
    success: (title: string, message?: string) => add('success', title, message),
    info:    (title: string, message?: string) => add('info',    title, message),
    warning: (title: string, message?: string) => add('warning', title, message),
    error:   (title: string, message?: string) => add('error',   title, message),
  }
}
