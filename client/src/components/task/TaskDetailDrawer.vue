<template>
  <Teleport to="body">
    <Transition name="task-detail-backdrop">
      <div
        v-if="open && task"
        class="task-detail-backdrop"
        @click.self="emit('close')"
      ></div>
    </Transition>

    <Transition name="task-detail-drawer">
      <aside
        v-if="open && task"
        class="task-detail-drawer"
        role="dialog"
        aria-modal="true"
      >
        <header class="task-detail-header">
          <div>
            <p class="task-detail-kicker mb-1">Task detail</p>
            <h2 class="task-detail-title mb-0">
              {{ task.title }}
            </h2>
          </div>

          <button
            type="button"
            class="task-detail-close"
            title="Close"
            @click="emit('close')"
          >
            <span class="material-symbols-outlined">close</span>
          </button>
        </header>

        <section class="task-detail-body">
          <div class="task-detail-status-row">
            <span
              class="status-pill d-inline-flex align-items-center gap-2 rounded-pill px-2 py-1"
              :class="statusClass(task.status)"
            >
              <span class="status-pill-dot"></span>
              {{ statusLabel(task.status) }}
            </span>

            <span
              class="priority-pill d-inline-block rounded-2 px-2 py-1"
              :class="priorityClass(task.priority)"
            >
              {{ priorityLabel(task.priority) }}
            </span>

            <span
              v-if="task.overdue"
              class="task-detail-overdue"
            >
              Overdue
            </span>
          </div>

          <div class="task-detail-grid">
            <div class="task-detail-field">
              <span class="task-detail-field-label">Assignee</span>

              <div class="task-detail-person">
                <img
                  class="task-detail-avatar"
                  :src="task.assignee.avatarUrl"
                  :alt="task.assignee.name"
                />

                <span>{{ task.assignee.name }}</span>
              </div>
            </div>

            <div class="task-detail-field">
              <span class="task-detail-field-label">Due date</span>

              <span
                class="task-detail-value"
                :class="{ 'task-detail-value-danger': task.overdue }"
              >
                {{ task.dueDate }}
              </span>
            </div>

            <div class="task-detail-field">
              <span class="task-detail-field-label">Task ID</span>

              <span class="task-detail-value">
                {{ task.id }}
              </span>
            </div>

            <div class="task-detail-field">
              <span class="task-detail-field-label">Linked page</span>

              <span class="task-detail-value">
                Q4 Roadmap
              </span>
            </div>
          </div>

          <section class="task-detail-section">
            <h3 class="task-detail-section-title">
              Description
            </h3>

            <p class="task-detail-description">
              {{ task.description || 'No description yet.' }}
            </p>
          </section>

          <section class="task-detail-section">
            <h3 class="task-detail-section-title">
              Activity
            </h3>

            <div class="task-activity-list">
              <div class="task-activity-item">
                <span class="task-activity-dot"></span>

                <div>
                  <p class="task-activity-text">
                    <strong>{{ task.assignee.name }}</strong> was assigned to this task.
                  </p>

                  <span class="task-activity-time">
                    2h ago
                  </span>
                </div>
              </div>

              <div class="task-activity-item">
                <span class="task-activity-dot"></span>

                <div>
                  <p class="task-activity-text">
                    Status changed to <strong>{{ statusLabel(task.status) }}</strong>.
                  </p>

                  <span class="task-activity-time">
                    1h ago
                  </span>
                </div>
              </div>
            </div>
          </section>

          <section class="task-detail-section task-comment-section">
            <div class="task-comment-heading">
              <h3 class="task-detail-section-title mb-0">
                Comments
              </h3>

              <span class="task-comment-count">
                {{ comments.length }}
              </span>
            </div>

            <div
              v-if="!comments.length"
              class="task-comment-empty"
            >
              Chưa có comment. Hãy là người đầu tiên trao đổi về task này.
            </div>

            <div
              v-else
              class="task-comment-list"
            >
              <article
                v-for="comment in comments"
                :key="comment.id"
                class="task-comment-item"
              >
                <img
                  class="task-comment-avatar"
                  :src="comment.author.avatarUrl"
                  :alt="comment.author.name"
                />

                <div class="task-comment-main">
                  <div class="task-comment-bubble">
                    <div class="task-comment-author-row">
                      <strong>{{ comment.author.name }}</strong>

                      <span>{{ comment.author.role }}</span>
                    </div>

                    <p>
                      {{ comment.content }}
                    </p>
                  </div>

                  <div class="task-comment-actions">
                    <button
                      type="button"
                      :class="{ active: isLiked(comment.id) }"
                      @click="toggleLike(comment.id)"
                    >
                      {{ isLiked(comment.id) ? 'Liked' : 'Like' }}
                    </button>

                    <button type="button">
                      Reply
                    </button>

                    <span>
                      {{ likeCount(comment) }} likes
                    </span>

                    <span>
                      {{ comment.createdAt }}
                    </span>
                  </div>

                  <div
                    v-if="comment.replies?.length"
                    class="task-comment-replies"
                  >
                    <article
                      v-for="reply in comment.replies"
                      :key="reply.id"
                      class="task-comment-reply"
                    >
                      <img
                        class="task-comment-avatar small"
                        :src="reply.author.avatarUrl"
                        :alt="reply.author.name"
                      />

                      <div class="task-comment-bubble reply">
                        <div class="task-comment-author-row">
                          <strong>{{ reply.author.name }}</strong>

                          <span>{{ reply.author.role }}</span>
                        </div>

                        <p>
                          {{ reply.content }}
                        </p>
                      </div>
                    </article>
                  </div>
                </div>
              </article>
            </div>
          </section>
        </section>

        <footer class="task-detail-composer">
          <img
            class="task-comment-avatar"
            :src="task.assignee.avatarUrl"
            :alt="task.assignee.name"
          />

          <div class="task-comment-input-wrap">
            <textarea
              v-model="draftComment"
              class="task-comment-input"
              rows="1"
              placeholder="Viết comment cho task này..."
              @keydown.enter.exact.prevent="submitComment"
            ></textarea>

            <div class="task-comment-composer-actions">
              <button
                type="button"
                title="Attach"
              >
                <span class="material-symbols-outlined">attach_file</span>
              </button>

              <button
                type="button"
                title="Mention"
              >
                <span class="material-symbols-outlined">alternate_email</span>
              </button>

              <button
                type="button"
                class="send-comment-btn"
                :disabled="!draftComment.trim()"
                @click="submitComment"
              >
                Send
              </button>
            </div>
          </div>
        </footer>
      </aside>
    </Transition>
  </Teleport>
</template>

<script setup>
import { onBeforeUnmount, onMounted, ref, watch } from 'vue'

const props = defineProps({
  open: {
    type: Boolean,
    default: false
  },
  task: {
    type: Object,
    default: null
  },
  comments: {
    type: Array,
    default: () => []
  }
})

const emit = defineEmits(['close', 'add-comment'])

const draftComment = ref('')
const likedCommentIds = ref(new Set())

watch(
  () => props.open,
  (isOpen) => {
    document.body.classList.toggle('task-detail-lock-scroll', isOpen)

    if (!isOpen) {
      draftComment.value = ''
    }
  }
)

function submitComment() {
  const content = draftComment.value.trim()

  if (!content) return

  emit('add-comment', content)
  draftComment.value = ''
}

function toggleLike(commentId) {
  const next = new Set(likedCommentIds.value)

  if (next.has(commentId)) {
    next.delete(commentId)
  } else {
    next.add(commentId)
  }

  likedCommentIds.value = next
}

function isLiked(commentId) {
  return likedCommentIds.value.has(commentId)
}

function likeCount(comment) {
  return comment.likes + (isLiked(comment.id) ? 1 : 0)
}

function statusLabel(status) {
  return {
    todo: 'To Do',
    doing: 'Doing',
    done: 'Done'
  }[status] ?? status
}

function statusClass(status) {
  return {
    todo: 'status-todo',
    doing: 'status-doing',
    done: 'status-done'
  }[status] ?? 'status-todo'
}

function priorityLabel(priority) {
  return {
    low: 'Low',
    medium: 'Med',
    high: 'High'
  }[priority] ?? priority
}

function priorityClass(priority) {
  return {
    low: 'priority-low',
    medium: 'priority-medium',
    high: 'priority-high'
  }[priority] ?? 'priority-low'
}

function handleEscape(event) {
  if (event.key === 'Escape' && props.open) {
    emit('close')
  }
}

onMounted(() => {
  window.addEventListener('keydown', handleEscape)
})

onBeforeUnmount(() => {
  window.removeEventListener('keydown', handleEscape)
  document.body.classList.remove('task-detail-lock-scroll')
})
</script>

<style scoped>
.task-detail-backdrop {
  position: fixed;
  inset: 0;
  z-index: 900;
  background: rgba(0, 0, 0, 0.42);
  backdrop-filter: blur(2px);
}

.task-detail-drawer {
  position: fixed;
  top: 0;
  right: 0;
  z-index: 901;
  width: min(440px, 100vw);
  height: 100vh;
  display: flex;
  flex-direction: column;
  color: #f1f1f1;
  background: #101010;
  border-left: 1px solid #2f2f2f;
  box-shadow: -24px 0 80px rgba(0, 0, 0, 0.48);
}

.task-detail-header {
  min-height: 92px;
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  padding: 22px 22px 18px;
  border-bottom: 1px solid #242424;
  background: #101010;
}

.task-detail-kicker {
  color: #a3a3a3;
  font-size: 11px;
  font-weight: 800;
  letter-spacing: 0.12em;
  text-transform: uppercase;
}

.task-detail-title {
  color: #f1f1f1;
  font-size: 22px;
  line-height: 1.22;
  font-weight: 750;
  letter-spacing: -0.03em;
}

.task-detail-close {
  width: 32px;
  height: 32px;
  border: 1px solid transparent;
  border-radius: 9px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: #a3a3a3;
  background: transparent;
  transition:
    color 0.16s ease,
    background 0.16s ease,
    border-color 0.16s ease,
    transform 0.16s ease;
}

.task-detail-close:hover {
  color: #f1f1f1;
  background: #202020;
  border-color: #2f2f2f;
  transform: rotate(4deg);
}

.task-detail-close .material-symbols-outlined {
  font-size: 18px;
}

.task-detail-body {
  flex: 1;
  overflow-y: auto;
  padding: 20px 22px 26px;
}

.task-detail-body::-webkit-scrollbar {
  width: 10px;
}

.task-detail-body::-webkit-scrollbar-track {
  background: transparent;
}

.task-detail-body::-webkit-scrollbar-thumb {
  border: 3px solid transparent;
  border-radius: 999px;
  background: #3a3a3a;
  background-clip: content-box;
}

.task-detail-status-row {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
  margin-bottom: 20px;
}

.task-detail-overdue {
  padding: 4px 8px;
  border: 1px solid #4a4a4a;
  border-radius: 999px;
  color: #f1f1f1;
  background: #202020;
  font-size: 12px;
  font-weight: 700;
}

.task-detail-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 14px;
  margin-bottom: 24px;
}

.task-detail-field {
  min-width: 0;
  padding: 12px;
  border: 1px solid #242424;
  border-radius: 12px;
  background: #151515;
}

.task-detail-field-label {
  display: block;
  margin-bottom: 8px;
  color: #737373;
  font-size: 10px;
  font-weight: 800;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.task-detail-person {
  min-width: 0;
  display: flex;
  align-items: center;
  gap: 8px;
  color: #f1f1f1;
  font-size: 13px;
  font-weight: 650;
}

.task-detail-avatar,
.task-comment-avatar {
  width: 26px;
  height: 26px;
  flex-shrink: 0;
  border-radius: 999px;
  object-fit: cover;
  border: 1px solid #2f2f2f;
}

.task-detail-value {
  overflow: hidden;
  display: block;
  color: #d4d4d4;
  font-size: 13px;
  font-weight: 600;
  white-space: nowrap;
  text-overflow: ellipsis;
}

.task-detail-value-danger {
  color: #f1f1f1;
}

.task-detail-section {
  padding-top: 22px;
  margin-top: 22px;
  border-top: 1px solid #242424;
}

.task-detail-section-title {
  margin: 0 0 10px;
  color: #f1f1f1;
  font-size: 13px;
  font-weight: 800;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.task-detail-description {
  margin: 0;
  color: #a3a3a3;
  font-size: 14px;
  line-height: 1.65;
}

.task-activity-list {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.task-activity-item {
  display: grid;
  grid-template-columns: 14px 1fr;
  gap: 10px;
}

.task-activity-dot {
  width: 8px;
  height: 8px;
  margin-top: 7px;
  border-radius: 999px;
  background: #a3a3a3;
}

.task-activity-text {
  margin: 0;
  color: #d4d4d4;
  font-size: 13px;
  line-height: 1.45;
}

.task-activity-time {
  display: inline-block;
  margin-top: 3px;
  color: #737373;
  font-size: 11px;
}

.task-comment-heading {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}

.task-comment-count {
  min-width: 24px;
  height: 24px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border-radius: 999px;
  color: #d4d4d4;
  background: #1f1f1f;
  border: 1px solid #2f2f2f;
  font-size: 12px;
  font-weight: 700;
}

.task-comment-empty {
  padding: 14px;
  border: 1px dashed #2f2f2f;
  border-radius: 12px;
  color: #737373;
  background: #151515;
  font-size: 13px;
  line-height: 1.5;
}

.task-comment-list {
  display: flex;
  flex-direction: column;
  gap: 15px;
}

.task-comment-item {
  display: flex;
  align-items: flex-start;
  gap: 10px;
}

.task-comment-main {
  min-width: 0;
  flex: 1;
}

.task-comment-bubble {
  padding: 10px 12px;
  border-radius: 14px;
  border-top-left-radius: 4px;
  background: #1a1a1a;
  border: 1px solid #242424;
}

.task-comment-bubble.reply {
  background: #151515;
}

.task-comment-author-row {
  display: flex;
  align-items: baseline;
  gap: 7px;
  margin-bottom: 4px;
}

.task-comment-author-row strong {
  color: #f1f1f1;
  font-size: 13px;
}

.task-comment-author-row span {
  color: #737373;
  font-size: 11px;
}

.task-comment-bubble p {
  margin: 0;
  color: #d4d4d4;
  font-size: 13px;
  line-height: 1.5;
}

.task-comment-actions {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 9px;
  padding: 5px 2px 0;
}

.task-comment-actions button {
  border: 0;
  padding: 0;
  color: #737373;
  background: transparent;
  font-size: 11.5px;
  font-weight: 750;
}

.task-comment-actions button:hover,
.task-comment-actions button.active {
  color: #f1f1f1;
}

.task-comment-actions span {
  color: #737373;
  font-size: 11px;
}

.task-comment-replies {
  margin-top: 10px;
  margin-left: 8px;
  padding-left: 12px;
  border-left: 1px solid #2f2f2f;
}

.task-comment-reply {
  display: flex;
  align-items: flex-start;
  gap: 8px;
}

.task-comment-avatar.small {
  width: 22px;
  height: 22px;
}

.task-detail-composer {
  display: flex;
  align-items: flex-end;
  gap: 10px;
  padding: 14px 16px 16px;
  border-top: 1px solid #242424;
  background: #101010;
}

.task-comment-input-wrap {
  min-width: 0;
  flex: 1;
  padding: 8px;
  border: 1px solid #2f2f2f;
  border-radius: 14px;
  background: #151515;
}

.task-comment-input {
  width: 100%;
  min-height: 34px;
  max-height: 120px;
  border: 0;
  outline: 0;
  resize: vertical;
  padding: 4px 4px 0;
  color: #f1f1f1;
  background: transparent;
  font-size: 13px;
  line-height: 1.45;
}

.task-comment-input::placeholder {
  color: #737373;
}

.task-comment-composer-actions {
  display: flex;
  align-items: center;
  gap: 6px;
  padding-top: 6px;
}

.task-comment-composer-actions button {
  height: 28px;
  border: 0;
  border-radius: 8px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: #737373;
  background: transparent;
}

.task-comment-composer-actions button:hover:not(:disabled) {
  color: #f1f1f1;
  background: #202020;
}

.task-comment-composer-actions .material-symbols-outlined {
  font-size: 16px;
}

.send-comment-btn {
  margin-left: auto;
  padding: 0 12px;
  color: #101010 !important;
  background: #f1f1f1 !important;
  font-size: 12px;
  font-weight: 800;
}

.send-comment-btn:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}

.status-pill,
.priority-pill {
  font-size: 12px;
  line-height: 1.2;
  border: 1px solid transparent;
}

.status-pill-dot {
  width: 6px;
  height: 6px;
  border-radius: 999px;
}

.status-todo {
  color: #a3a3a3;
  background: #171717;
  border-color: #2f2f2f;
}

.status-todo .status-pill-dot {
  border: 1px solid #737373;
}

.status-doing {
  color: #f1f1f1;
  background: #242424;
  border-color: #404040;
}

.status-doing .status-pill-dot {
  background: #a3a3a3;
}

.status-done {
  color: #f1f1f1;
  background: #2a2a2a;
  border-color: #4a4a4a;
}

.status-done .status-pill-dot {
  background: #f1f1f1;
}

.priority-high {
  color: #f1f1f1;
  background: #2a2a2a;
  border-color: #4a4a4a;
}

.priority-medium {
  color: #d4d4d4;
  background: #202020;
  border-color: #3a3a3a;
}

.priority-low {
  color: #a3a3a3;
  background: #171717;
  border-color: #2f2f2f;
}

.task-detail-backdrop-enter-active,
.task-detail-backdrop-leave-active {
  transition: opacity 0.24s ease;
}

.task-detail-backdrop-enter-from,
.task-detail-backdrop-leave-to {
  opacity: 0;
}

.task-detail-drawer-enter-active,
.task-detail-drawer-leave-active {
  transition:
    transform 0.32s cubic-bezier(0.16, 1, 0.3, 1),
    opacity 0.24s ease;
}

.task-detail-drawer-enter-from,
.task-detail-drawer-leave-to {
  opacity: 0;
  transform: translateX(100%);
}

.task-detail-drawer-enter-to,
.task-detail-drawer-leave-from {
  opacity: 1;
  transform: translateX(0);
}

@media (max-width: 575.98px) {
  .task-detail-drawer {
    width: 100vw;
  }

  .task-detail-grid {
    grid-template-columns: 1fr;
  }
}
</style>

<style>
.task-detail-lock-scroll {
  overflow: hidden;
}
</style>