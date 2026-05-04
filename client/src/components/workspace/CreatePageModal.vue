<template>
  <div class="page-composer">
    <div class="d-flex align-items-center justify-content-between mb-3">
      <div class="composer-meta d-flex align-items-center gap-2">
        <span class="composer-dot"></span>
        <span>New page draft</span>
      </div>

      <button class="composer-close-btn" type="button" @click="handleCancel">
        <i class="bi bi-x-lg"></i>
      </button>
    </div>

    <div class="d-flex align-items-start gap-3">
      <button class="page-icon-trigger" type="button">
        <i class="bi bi-file-earmark-richtext"></i>
      </button>

      <div class="flex-grow-1 min-w-0">
        <input
          v-model="form.title"
          type="text"
          class="page-composer-title"
          placeholder="Untitled page"
        />

        <div class="d-flex flex-wrap gap-2 mt-3">
          <button class="preset-chip" type="button" @click="applyPagePreset('blank')">
            Blank
          </button>
          <button class="preset-chip" type="button" @click="applyPagePreset('roadmap')">
            Roadmap
          </button>
          <button class="preset-chip" type="button" @click="applyPagePreset('notes')">
            Notes
          </button>
          <button class="preset-chip" type="button" @click="applyPagePreset('spec')">
            Spec
          </button>
        </div>
      </div>
    </div>

    <div class="composer-divider my-4"></div>

    <div class="composer-content-label mb-2">Start with a short introduction</div>

    <textarea
      v-model="form.content"
      rows="7"
      class="page-composer-content"
      placeholder="Write a short summary, goals, notes, or context for this page..."
    ></textarea>

    <div class="d-flex flex-wrap align-items-center justify-content-between gap-3 mt-4">
      <div class="composer-hint">
        Page title is required. Content can be empty.
      </div>

      <div class="d-flex align-items-center gap-2">
        <button class="btn btn-page-ghost" type="button" @click="handleCancel">
          Cancel
        </button>
        <button class="btn btn-page-create" type="button" @click="handleSubmit">
          Create page
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'

const emit = defineEmits(['cancel', 'submit'])

const form = ref({
  title: '',
  content: ''
})

function applyPagePreset(type) {
  const presets = {
    blank: {
      title: '',
      content: ''
    },
    roadmap: {
      title: 'Roadmap',
      content: '## Goal\n\n## Milestones\n\n## Risks\n\n## Next steps'
    },
    notes: {
      title: 'Meeting Notes',
      content: '## Summary\n\n## Discussion points\n\n## Action items'
    },
    spec: {
      title: 'Feature Spec',
      content: '## Problem\n\n## Proposed solution\n\n## Scope\n\n## Open questions'
    }
  }

  const preset = presets[type]
  if (!preset) return

  if (!form.value.title.trim()) {
    form.value.title = preset.title
  }

  if (!form.value.content.trim()) {
    form.value.content = preset.content
  }
}

function resetForm() {
  form.value = {
    title: '',
    content: ''
  }
}

function handleCancel() {
  resetForm()
  emit('cancel')
}

function handleSubmit() {
  const title = form.value.title.trim()
  const content = form.value.content.trim()

  if (!title) return

  emit('submit', {
    title,
    content
  })

  resetForm()
}
</script>

<style scoped>
.page-composer {
  border-radius: 24px;
  padding: 24px;
  background:
    linear-gradient(180deg, rgba(255, 255, 255, 0.035), rgba(255, 255, 255, 0.02)),
    #131313;
  border: 1px solid rgba(72, 72, 72, 0.14);
  box-shadow:
    0 18px 40px rgba(0, 0, 0, 0.18),
    0 1px 0 rgba(255, 255, 255, 0.02) inset;
}

.composer-meta {
  font-size: 11px;
  font-weight: 800;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: #8f8e8d;
}

.composer-dot {
  width: 7px;
  height: 7px;
  border-radius: 50%;
  background: #adc6ff;
}

.composer-close-btn {
  width: 34px;
  height: 34px;
  border: 0;
  border-radius: 10px;
  background: transparent;
  color: #8f8e8d;
  transition: 0.2s ease;
}

.composer-close-btn:hover {
  background: rgba(255, 255, 255, 0.05);
  color: #e7e5e5;
}

.page-icon-trigger {
  width: 56px;
  height: 56px;
  min-width: 56px;
  border: 1px solid rgba(72, 72, 72, 0.18);
  border-radius: 18px;
  background: rgba(173, 198, 255, 0.08);
  color: #adc6ff;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-size: 22px;
  transition: 0.2s ease;
}

.page-icon-trigger:hover {
  background: rgba(173, 198, 255, 0.12);
}

.page-composer-title {
  width: 100%;
  border: 0;
  outline: 0;
  background: transparent;
  color: #f3f2f1;
  font-size: 2.25rem;
  line-height: 1.1;
  font-weight: 800;
  letter-spacing: -0.05em;
  padding: 2px 0;
}

.page-composer-title::placeholder {
  color: rgba(231, 229, 229, 0.28);
}

.preset-chip {
  height: 30px;
  border: 1px solid rgba(72, 72, 72, 0.16);
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.03);
  color: #acabaa;
  font-size: 11px;
  font-weight: 700;
  padding: 0 12px;
  transition: 0.2s ease;
}

.preset-chip:hover {
  background: rgba(255, 255, 255, 0.06);
  color: #e7e5e5;
}

.composer-divider {
  height: 1px;
  background: rgba(72, 72, 72, 0.14);
}

.composer-content-label {
  font-size: 11px;
  font-weight: 800;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: #8f8e8d;
}

.page-composer-content {
  width: 100%;
  min-height: 180px;
  border: 0;
  outline: 0;
  resize: vertical;
  background: transparent;
  color: #e7e5e5;
  font-size: 15px;
  line-height: 1.85;
  padding: 0;
}

.page-composer-content::placeholder {
  color: rgba(172, 171, 170, 0.4);
}

.composer-hint {
  font-size: 12px;
  color: #8f8e8d;
}

.btn-page-ghost {
  border: 0;
  background: transparent;
  color: #acabaa;
  font-weight: 600;
  padding: 0.65rem 0.9rem;
}

.btn-page-ghost:hover {
  color: #e7e5e5;
}

.btn-page-create {
  background: #adc6ff;
  color: #003d88;
  border: 0;
  border-radius: 12px;
  font-weight: 700;
  padding: 0.7rem 1rem;
}

.btn-page-create:hover {
  background: #98b8ff;
  color: #003d88;
}

@media (max-width: 768px) {
  .page-composer {
    padding: 18px;
  }

  .page-composer-title {
    font-size: 1.8rem;
  }

  .d-flex.flex-wrap.align-items-center.justify-content-between.gap-3.mt-4 {
    flex-direction: column;
    align-items: stretch !important;
  }

  .btn-page-create,
  .btn-page-ghost {
    width: 100%;
  }
}
</style>