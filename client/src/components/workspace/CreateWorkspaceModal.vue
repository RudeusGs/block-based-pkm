<template>
  <Teleport to="body">
    <div
      v-if="modelValue"
      class="workspace-modal-overlay"
      @click="handleOverlayClick"
    >
      <div class="workspace-modal-shell" @click.stop>
        <div class="workspace-modal-topbar d-flex align-items-center justify-content-between">
          <div class="d-flex align-items-center gap-2 modal-crumb">
            <i class="bi bi-grid-1x2"></i>
            <span>New workspace</span>
          </div>

          <button class="ghost-icon-btn" type="button" @click="closeModal">
            <i class="bi bi-x-lg"></i>
          </button>
        </div>

        <form @submit.prevent="handleSubmit" class="workspace-modal-body">
          <div class="hero-row d-flex align-items-start gap-3">
            <button class="workspace-icon-btn" type="button">
              <i class="bi bi-grid-1x2-fill"></i>
            </button>

            <div class="flex-grow-1 min-w-0">
              <input
                ref="nameInputRef"
                v-model="form.name"
                type="text"
                class="workspace-title-input"
                placeholder="Untitled workspace"
                maxlength="50"
              />

              <div class="title-meta d-flex align-items-center justify-content-between mt-2">
                <span class="inline-hint" :class="{ danger: nameError }">
                  {{ nameError || 'Tên workspace sẽ hiển thị ở sidebar và danh sách workspace.' }}
                </span>
                <span class="char-counter" :class="{ danger: form.name.length >= 50 }">
                  {{ form.name.length }}/50
                </span>
              </div>
            </div>
          </div>

          <div class="workspace-properties mt-4">
            <div class="property-row">
              <div class="property-label">
                <i class="bi bi-text-paragraph"></i>
                <span>Description</span>
              </div>

              <div class="property-value">
                <textarea
                  v-model="form.description"
                  class="workspace-description-input"
                  rows="6"
                  maxlength="500"
                  placeholder="Write a short description about this workspace..."
                ></textarea>

                <div class="title-meta d-flex align-items-center justify-content-between mt-2">
                  <span class="inline-hint" :class="{ danger: descriptionError }">
                    {{ descriptionError || 'Mô tả giúp team hiểu workspace này dùng để làm gì.' }}
                  </span>
                  <span class="char-counter" :class="{ danger: form.description.length >= 500 }">
                    {{ form.description.length }}/500
                  </span>
                </div>
              </div>
            </div>
          </div>

          <div class="workspace-preview mt-4">
            <div class="preview-label">Preview in sidebar</div>

            <div class="preview-card d-flex align-items-center gap-3">
              <div class="preview-icon">
                <i class="bi bi-grid-1x2-fill"></i>
              </div>

              <div class="min-w-0">
                <div class="preview-title text-truncate">
                  {{ previewName }}
                </div>
                <div class="preview-description text-truncate">
                  {{ previewDescription }}
                </div>
              </div>
            </div>
          </div>

          <div class="workspace-modal-footer d-flex align-items-center justify-content-between mt-4">
            <div class="keyboard-hint">
              <span>Enter to create</span>
              <span class="dot-separator"></span>
              <span>Esc to close</span>
            </div>

            <div class="d-flex align-items-center gap-2">
              <button class="btn btn-ghost-action" type="button" @click="closeModal">
                Cancel
              </button>

              <button class="btn btn-create-workspace" type="submit" :disabled="!isFormValid">
                Create workspace
              </button>
            </div>
          </div>
        </form>
      </div>
    </div>
  </Teleport>
</template>

<script setup>
import { computed, nextTick, onMounted, onBeforeUnmount, ref, watch } from 'vue'

const props = defineProps({
  modelValue: {
    type: Boolean,
    default: false
  }
})

const emit = defineEmits(['update:modelValue', 'submit'])

const nameInputRef = ref(null)

const form = ref({
  name: '',
  description: ''
})

watch(
  () => props.modelValue,
  async (isOpen) => {
    if (isOpen) {
      await nextTick()
      nameInputRef.value?.focus()
    }
  }
)

const nameError = computed(() => {
  const value = form.value.name.trim()

  if (!value) return 'Tên Workspace không được để trống.'
  if (value.length > 50) return 'Tên Workspace không được quá 50 ký tự.'
  return ''
})

const descriptionError = computed(() => {
  const value = form.value.description.trim()

  if (value.length > 500) return 'Description không được quá 500 ký tự.'
  return ''
})

const isFormValid = computed(() => !nameError.value && !descriptionError.value)

const previewName = computed(() => form.value.name.trim() || 'Untitled workspace')

const previewDescription = computed(() => {
  return form.value.description.trim() || 'No description'
})

function closeModal() {
  emit('update:modelValue', false)
}

function handleOverlayClick() {
  closeModal()
}

function resetForm() {
  form.value = {
    name: '',
    description: ''
  }
}

function handleSubmit() {
  if (!isFormValid.value) return

  emit('submit', {
    name: form.value.name.trim(),
    description: form.value.description.trim() || null
  })

  resetForm()
  closeModal()
}

function handleKeydown(event) {
  if (!props.modelValue) return

  if (event.key === 'Escape') {
    closeModal()
  }
}

onMounted(() => {
  window.addEventListener('keydown', handleKeydown)
})

onBeforeUnmount(() => {
  window.removeEventListener('keydown', handleKeydown)
})
</script>

<style scoped src="@/assets/css/components/CreateWorkspaceModal.css"></style>