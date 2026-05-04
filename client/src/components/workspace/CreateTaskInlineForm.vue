<template>
  <div class="task-composer">
    <div class="d-flex align-items-center justify-content-between mb-3">
      <div class="composer-meta d-flex align-items-center gap-2">
        <span class="composer-dot"></span>
        <span>Quick task capture</span>
      </div>

      <button class="composer-close-btn" type="button" @click="$emit('cancel')">
        <i class="bi bi-x-lg"></i>
      </button>
    </div>

    <div class="d-flex align-items-start gap-3">
      <button class="task-icon-trigger" type="button">
        <i class="bi bi-check2-square"></i>
      </button>

      <div class="flex-grow-1 min-w-0">
        <input
          v-model="form.title"
          type="text"
          class="task-title-input"
          placeholder="What needs to be done?"
        />

        <textarea
          v-model="form.description"
          rows="3"
          class="task-note-input mt-3"
          placeholder="Add context, acceptance notes, or a short description..."
        ></textarea>
      </div>
    </div>

    <div class="composer-divider my-4"></div>

    <div class="row g-3">
      <div class="col-12 col-md-6 col-xl-3">
        <div class="property-block">
          <div class="property-label">Assignee</div>
          <input
            v-model="form.assigneeName"
            type="text"
            class="property-input"
            placeholder="Marcus Aurelius"
          />
        </div>
      </div>

      <div class="col-6 col-md-6 col-xl-3">
        <div class="property-block">
          <div class="property-label">Status</div>

          <div class="property-select-wrap">
            <select v-model="form.status" class="property-select">
              <option>ToDo</option>
              <option>Doing</option>
              <option>Done</option>
            </select>
            <span class="property-select-icon">
              <i class="bi bi-chevron-down"></i>
            </span>
          </div>
        </div>
      </div>

      <div class="col-6 col-md-6 col-xl-3">
        <div class="property-block">
          <div class="property-label">Priority</div>

          <div class="property-select-wrap">
            <select v-model="form.priority" class="property-select">
              <option>Low</option>
              <option>Medium</option>
              <option>High</option>
            </select>
            <span class="property-select-icon">
              <i class="bi bi-chevron-down"></i>
            </span>
          </div>
        </div>
      </div>

      <div class="col-12 col-md-6 col-xl-3">
        <div class="property-block">
          <div class="property-label">Due date</div>
          <input
            v-model="form.dueDate"
            type="date"
            class="property-input"
          />
        </div>
      </div>
    </div>

    <div class="d-flex flex-wrap align-items-center justify-content-between gap-3 mt-4">
      <div class="composer-hint">
        A task needs a title and an assignee.
      </div>

      <div class="d-flex align-items-center gap-2">
        <button class="btn btn-task-ghost" type="button" @click="$emit('cancel')">
          Cancel
        </button>
        <button class="btn btn-task-create" type="button" @click="submitForm">
          Add task
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
  description: '',
  assigneeName: '',
  status: 'ToDo',
  priority: 'Medium',
  dueDate: ''
})

function submitForm() {
  if (!form.value.title.trim()) return
  if (!form.value.assigneeName.trim()) return

  emit('submit', {
    ...form.value,
    assigneeAvatar: `https://i.pravatar.cc/40?u=${encodeURIComponent(form.value.assigneeName.trim())}`
  })

  form.value = {
    title: '',
    description: '',
    assigneeName: '',
    status: 'ToDo',
    priority: 'Medium',
    dueDate: ''
  }
}
</script>

<style scoped>
.task-composer {
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

.task-icon-trigger {
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

.task-icon-trigger:hover {
  background: rgba(173, 198, 255, 0.12);
}

.task-title-input {
  width: 100%;
  border: 0;
  outline: 0;
  background: transparent;
  color: #f3f2f1;
  font-size: 2rem;
  line-height: 1.1;
  font-weight: 800;
  letter-spacing: -0.05em;
  padding: 2px 0;
}

.task-title-input::placeholder {
  color: rgba(231, 229, 229, 0.28);
}

.task-note-input {
  width: 100%;
  border: 0;
  outline: 0;
  resize: vertical;
  background: transparent;
  color: #e7e5e5;
  font-size: 15px;
  line-height: 1.8;
  min-height: 96px;
  padding: 0;
}

.task-note-input::placeholder {
  color: rgba(172, 171, 170, 0.4);
}

.composer-divider {
  height: 1px;
  background: rgba(72, 72, 72, 0.14);
}

.property-block {
  border-radius: 16px;
  padding: 14px;
  background: rgba(255, 255, 255, 0.025);
  border: 1px solid rgba(72, 72, 72, 0.12);
}

.property-label {
  font-size: 10px;
  font-weight: 800;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: #8f8e8d;
  margin-bottom: 8px;
}

.property-input {
  width: 100%;
  border: 0;
  outline: 0;
  background: transparent;
  color: #f3f2f1;
  font-size: 14px;
  min-height: 24px;
  padding: 0;
}

.property-input::placeholder {
  color: rgba(172, 171, 170, 0.4);
}

.property-select-wrap {
  position: relative;
}

.property-select {
  width: 100%;
  min-height: 44px;
  appearance: none;
  -webkit-appearance: none;
  -moz-appearance: none;
  border: 1px solid rgba(173, 198, 255, 0.1);
  outline: 0;
  border-radius: 14px;
  padding: 0 42px 0 14px;
  background:
    linear-gradient(180deg, rgba(255, 255, 255, 0.045), rgba(255, 255, 255, 0.02)),
    #181818;
  color: #f3f2f1;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  box-shadow:
    0 1px 0 rgba(255, 255, 255, 0.03) inset,
    0 10px 18px rgba(0, 0, 0, 0.14);
  transition:
    border-color 0.2s ease,
    background 0.2s ease,
    box-shadow 0.2s ease,
    transform 0.2s ease;
}

.property-select:hover {
  border-color: rgba(173, 198, 255, 0.22);
  background:
    linear-gradient(180deg, rgba(255, 255, 255, 0.06), rgba(255, 255, 255, 0.025)),
    #1b1b1b;
}

.property-select:focus {
  border-color: rgba(173, 198, 255, 0.45);
  box-shadow:
    0 0 0 3px rgba(173, 198, 255, 0.12),
    0 10px 22px rgba(0, 0, 0, 0.18);
}

.property-select-icon {
  position: absolute;
  top: 50%;
  right: 14px;
  transform: translateY(-50%);
  color: #9fb6eb;
  font-size: 12px;
  pointer-events: none;
  transition: color 0.2s ease;
}

.property-select-wrap:hover .property-select-icon {
  color: #c7d7ff;
}

.property-select option {
  background: #161616;
  color: #f3f2f1;
}

.composer-hint {
  font-size: 12px;
  color: #8f8e8d;
}

.btn-task-ghost {
  border: 0;
  background: transparent;
  color: #acabaa;
  font-weight: 600;
  padding: 0.65rem 0.9rem;
}

.btn-task-ghost:hover {
  color: #e7e5e5;
}

.btn-task-create {
  background: #adc6ff;
  color: #003d88;
  border: 0;
  border-radius: 12px;
  font-weight: 700;
  padding: 0.7rem 1rem;
}

.btn-task-create:hover {
  background: #98b8ff;
  color: #003d88;
}

@media (max-width: 768px) {
  .task-composer {
    padding: 18px;
  }

  .task-title-input {
    font-size: 1.65rem;
  }

  .d-flex.flex-wrap.align-items-center.justify-content-between.gap-3.mt-4 {
    flex-direction: column;
    align-items: stretch !important;
  }

  .btn-task-create,
  .btn-task-ghost {
    width: 100%;
  }
}
</style>