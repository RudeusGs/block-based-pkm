<template>
  <section class="page-notes-section">
    <div class="notes-shell">
      <div class="notes-toolbar-label">
        <span class="notes-toolbar-dot"></span>
        <span>Page notes</span>
      </div>

      <div class="editorjs-frame" @click="focusEditor">
        <div :id="holderId" class="editorjs-host"></div>
      </div>
    </div>
  </section>
</template>

<script setup>
import { onBeforeUnmount, onMounted, watch } from 'vue'
import EditorJS from '@editorjs/editorjs'
import Header from '@editorjs/header'
import List from '@editorjs/list'
import Checklist from '@editorjs/checklist'
import Quote from '@editorjs/quote'

const props = defineProps({
  modelValue: {
    type: Object,
    default: () => ({
      time: Date.now(),
      blocks: [
        {
          type: 'paragraph',
          data: {
            text: ''
          }
        }
      ]
    })
  }
})

const emit = defineEmits(['update:modelValue'])

const holderId = `page-note-editor-${Math.random().toString(36).slice(2, 10)}`

let editor = null
let saveTimer = null
let lastSyncedValue = ''

function getDefaultData() {
  return {
    time: Date.now(),
    blocks: [
      {
        type: 'paragraph',
        data: {
          text: ''
        }
      }
    ]
  }
}

function normalizeValue(value) {
  if (!value || typeof value !== 'object' || !Array.isArray(value.blocks)) {
    return getDefaultData()
  }

  return value
}

async function saveEditorData() {
  if (!editor) return

  try {
    const output = await editor.save()
    lastSyncedValue = JSON.stringify(output)
    emit('update:modelValue', output)
  } catch (error) {
    console.error('Failed to save Editor.js data:', error)
  }
}

function queueSave() {
  clearTimeout(saveTimer)
  saveTimer = setTimeout(() => {
    saveEditorData()
  }, 250)
}

function focusEditor() {
  if (!editor) return
  editor.caret?.focus?.(true)
}

onMounted(async () => {
  const initialData = normalizeValue(props.modelValue)
  lastSyncedValue = JSON.stringify(initialData)

  editor = new EditorJS({
    holder: holderId,
    data: initialData,
    autofocus: false,
    minHeight: 180,
    placeholder: "Type '/' for commands",
    defaultBlock: 'paragraph',
    inlineToolbar: true,
    tools: {
      header: {
        class: Header,
        inlineToolbar: true,
        config: {
          levels: [1, 2, 3, 4],
          defaultLevel: 2
        }
      },
      list: {
        class: List,
        inlineToolbar: true,
        config: {
          defaultStyle: 'unordered'
        }
      },
      checklist: {
        class: Checklist,
        inlineToolbar: true
      },
      quote: {
        class: Quote,
        inlineToolbar: true,
        config: {
          quotePlaceholder: 'Write a quote...',
          captionPlaceholder: 'Quote source'
        }
      }
    },
    async onChange() {
      queueSave()
    }
  })

  try {
    await editor.isReady
  } catch (error) {
    console.error('Editor.js init failed:', error)
  }
})

watch(
  () => props.modelValue,
  async (value) => {
    const normalized = normalizeValue(value)
    const nextValue = JSON.stringify(normalized)

    if (!editor) return
    if (nextValue === lastSyncedValue) return

    try {
      await editor.isReady
      await editor.render(normalized)
      lastSyncedValue = nextValue
    } catch (error) {
      console.error('Failed to render external Editor.js data:', error)
    }
  },
  { deep: true }
)

onBeforeUnmount(async () => {
  clearTimeout(saveTimer)

  if (editor) {
    try {
      await saveEditorData()
      editor.destroy()
    } catch (error) {
      console.error('Failed to destroy Editor.js instance:', error)
    } finally {
      editor = null
    }
  }
})
</script>

<style scoped>
.page-notes-section {
  margin-top: 1.5rem;
  padding-left: clamp(16px, 3.6vw, 48px);
}

.notes-shell {
  max-width: 860px;
  margin: 0;
}

.notes-toolbar-label {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 12px;
  padding-left: 2px;
  font-size: 11px;
  font-weight: 800;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: rgba(143, 142, 141, 0.92);
}

.notes-toolbar-dot {
  width: 7px;
  height: 7px;
  border-radius: 999px;
  background: #adc6ff;
  box-shadow: 0 0 0 6px rgba(173, 198, 255, 0.06);
}

.editorjs-frame {
  border-radius: 20px;
  padding: 10px 8px 14px 60px !important; 
  background: transparent;
  border: none;
  box-shadow: none;
}

:deep(.ce-block__content),
:deep(.ce-toolbar__content) {
  max-width: 100% !important;
  margin-left: 0 !important;
}

:deep(.ce-toolbar__plus) {
  left: -45px !important;
}

:deep(.ce-toolbar__settings-btn) {
  left: -20px !important; 
  right: auto !important;
}

.editorjs-host {
  min-height: 180px;
}

/* Base */
:deep(.codex-editor) {
  color: #e7e5e5;
}

:deep(.codex-editor__redactor) {
  padding-bottom: 28px !important;
}

:deep(.ce-block__content),
:deep(.ce-toolbar__content) {
  max-width: 100%;
}

:deep(.ce-toolbar__content) {
  max-width: calc(100% - 18px);
}

:deep(.ce-block) {
  padding: 0.08rem 0;
}

:deep(.ce-paragraph),
:deep(.ce-header),
:deep(.cdx-quote__text),
:deep(.cdx-quote__caption),
:deep(.cdx-list__item),
:deep(.cdx-checklist__item-text) {
  color: #e7e5e5;
}

/* Typography mượt và thoáng hơn */
:deep(.ce-paragraph) {
  line-height: 1.95;
  font-size: 15px;
  padding: 0.22rem 0;
}

:deep(.ce-header) {
  line-height: 1.18;
  font-weight: 750;
  letter-spacing: -0.035em;
  padding: 0.42rem 0 0.22rem;
}

:deep(h1.ce-header) {
  font-size: 2rem;
}

:deep(h2.ce-header) {
  font-size: 1.52rem;
}

:deep(h3.ce-header) {
  font-size: 1.18rem;
}

:deep(.ce-paragraph[data-placeholder]:empty::before),
:deep(.ce-header[data-placeholder]:empty::before) {
  color: rgba(172, 171, 170, 0.38);
}

/* Block look */
:deep(.ce-block__content) {
  border-radius: 12px;
  padding: 2px 10px;
  margin-left: -10px;
  margin-right: -10px;
  transition:
    background 0.16s ease,
    transform 0.16s ease;
}

/* Giữ hover block */
:deep(.ce-block__content:hover) {
  background: rgba(255, 255, 255, 0.028);
}

/* Focus block vẫn nhẹ, nhưng không làm nổi container */
:deep(.ce-block--focused .ce-block__content) {
  background: rgba(255, 255, 255, 0.016);
}

/* Selected block dịu hơn bản cũ */
:deep(.ce-block--selected .ce-block__content) {
  background: rgba(173, 198, 255, 0.06);
  border-radius: 12px;
}

/* Toolbar buttons */
:deep(.ce-toolbar__plus),
:deep(.ce-toolbar__settings-btn) {
  width: 28px;
  height: 28px;
  border-radius: 9px;
  color: #8f8e8d !important;
  background-color: transparent !important;
  border: none !important;
  opacity: 0.78;
  transition:
    background 0.18s ease,
    color 0.18s ease,
    opacity 0.18s ease;
}

:deep(.ce-toolbar__plus:hover),
:deep(.ce-toolbar__settings-btn:hover) {
  background-color: rgba(255, 255, 255, 0.065) !important;
  color: #f3f1f1 !important;
  opacity: 1;
}

:deep(.ce-toolbar__plus svg),
:deep(.ce-toolbar__settings-btn svg) {
  transform: scale(0.92);
  fill: currentColor;
}

:deep(.ce-toolbar__plus) {
  left: -34px;
}

:deep(.ce-toolbar__settings-btn) {
  right: -2px;
}

/* Inline / popover / slash menu */
:deep(.ce-inline-toolbar),
:deep(.ce-conversion-toolbar),
:deep(.ce-popover) {
  background: rgba(25, 25, 25, 0.96);
  border: 1px solid rgba(255, 255, 255, 0.05);
  border-radius: 14px;
  box-shadow:
    0 18px 40px rgba(0, 0, 0, 0.42),
    0 1px 0 rgba(255, 255, 255, 0.04) inset;
  backdrop-filter: blur(14px);
}

:deep(.ce-popover) {
  padding: 8px;
}

:deep(.ce-popover__items) {
  padding: 0;
}

:deep(.ce-popover-item),
:deep(.ce-conversion-tool) {
  min-height: 40px;
  border-radius: 10px;
  color: #d8d7d7;
  transition:
    background 0.18s ease,
    color 0.18s ease;
}

:deep(.ce-popover-item:hover),
:deep(.ce-conversion-tool:hover) {
  background: rgba(255, 255, 255, 0.06);
  color: #ffffff;
}

:deep(.ce-popover-item--active),
:deep(.ce-conversion-tool--focused) {
  background: rgba(173, 198, 255, 0.13);
  color: #adc6ff;
}

:deep(.ce-popover-item__icon),
:deep(.ce-conversion-tool__icon) {
  width: 32px;
  height: 32px;
  border-radius: 9px;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid rgba(255, 255, 255, 0.05);
  margin-right: 10px;
}

:deep(.ce-popover-item:hover .ce-popover-item__icon),
:deep(.ce-conversion-tool:hover .ce-conversion-tool__icon) {
  background: rgba(255, 255, 255, 0.07);
}

:deep(.cdx-search-field) {
  margin-bottom: 8px;
  background: #141414;
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 10px;
  box-shadow: none;
}

:deep(.cdx-search-field__input) {
  color: #e7e5e5;
}

:deep(.cdx-search-field__input::placeholder) {
  color: rgba(172, 171, 170, 0.42);
}

:deep(.ce-inline-toolbar) {
  padding: 6px;
}

:deep(.ce-inline-tool) {
  border-radius: 10px;
  color: #d8d7d7;
  transition:
    background 0.18s ease,
    color 0.18s ease;
}

:deep(.ce-inline-tool:hover) {
  background: rgba(255, 255, 255, 0.06);
  color: #ffffff;
}

:deep(.ce-inline-tool--active) {
  background: rgba(173, 198, 255, 0.14);
  color: #adc6ff;
}

/* List */
:deep(.cdx-list) {
  padding-left: 4px;
}

:deep(.cdx-list__item) {
  padding: 4px 0;
}

/* Checklist */
:deep(.cdx-checklist__item) {
  align-items: flex-start;
  padding: 4px 0;
}

:deep(.cdx-checklist__item-checkbox) {
  width: 18px;
  height: 18px;
  margin-top: 6px;
  border-radius: 6px;
  border-color: rgba(173, 198, 255, 0.32);
  background: rgba(255, 255, 255, 0.025);
}

:deep(.cdx-checklist__item--checked .cdx-checklist__item-checkbox) {
  background: #adc6ff;
  border-color: #adc6ff;
}

:deep(.cdx-checklist__item-text) {
  padding-left: 12px;
}

/* Quote */
:deep(.cdx-quote) {
  border-left: 3px solid rgba(173, 198, 255, 0.26);
  padding-left: 18px;
  margin: 10px 0;
}

:deep(.cdx-quote__text) {
  font-style: italic;
}

@media (max-width: 768px) {
  .page-notes-section {
    margin-top: 1.2rem;
    padding-left: 10px;
  }

  .editorjs-frame {
    padding: 8px 2px 14px 10px;
  }

  :deep(.ce-toolbar__plus) {
    left: -28px;
  }

  :deep(.ce-paragraph) {
    font-size: 14px;
  }

  :deep(h1.ce-header) {
    font-size: 1.7rem;
  }

  :deep(h2.ce-header) {
    font-size: 1.32rem;
  }

  :deep(h3.ce-header) {
    font-size: 1.08rem;
  }
}
</style>