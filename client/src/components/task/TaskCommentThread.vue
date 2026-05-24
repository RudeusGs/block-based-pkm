<template>
  <article
    class="task-comment-thread"
    :class="{ 'is-reply': depth > 0 }"
  >
    <img
      class="task-comment-avatar"
      :class="{ small: depth > 0 }"
      :src="comment.author.avatarUrl"
      :alt="comment.author.name"
    />

    <div class="task-comment-thread-main">
      <div class="task-comment-bubble">
        <div class="task-comment-author-row">
          <strong>{{ comment.author.name }}</strong>
          <span>{{ comment.author.role }}</span>
        </div>

        <p>{{ comment.content }}</p>
      </div>

      <div class="task-comment-actions">
        <span>{{ comment.createdAt }}</span>

        <button
          v-if="canReply"
          type="button"
          :disabled="isAddingComment"
          @click="toggleReplyComposer"
        >
          Trả lời
        </button>
      </div>

      <form
        v-if="isReplying"
        class="task-reply-composer"
        @submit.prevent="submitReply"
      >
        <textarea
          ref="replyInputRef"
          v-model="replyDraft"
          rows="1"
          :placeholder="`Trả lời ${comment.author.name}...`"
          :disabled="isAddingComment"
          @keydown.enter.exact.prevent="submitReply"
          @keydown.esc.prevent="closeReplyComposer"
        ></textarea>

        <div class="task-reply-actions">
          <button
            type="button"
            :disabled="isAddingComment"
            @click="closeReplyComposer"
          >
            Hủy
          </button>

          <button
            type="submit"
            class="primary"
            :disabled="!replyDraft.trim() || isAddingComment"
          >
            {{ isAddingComment ? 'Đang gửi...' : 'Trả lời' }}
          </button>
        </div>
      </form>

      <div
        v-if="comment.replies.length"
        class="task-comment-children"
      >
        <TaskCommentThread
          v-for="reply in comment.replies"
          :key="reply.id"
          :comment="reply"
          :depth="depth + 1"
          :is-adding-comment="isAddingComment"
          :can-reply="canReply"
          @add-reply="forwardReply"
        />
      </div>
    </div>
  </article>
</template>

<script setup lang="ts">
import { nextTick, ref } from 'vue'
import type { Guid } from '@/api/models/common.model'
import type { TaskCommentView } from './task.types'

defineOptions({
  name: 'TaskCommentThread',
})

const props = withDefaults(
  defineProps<{
    comment: TaskCommentView
    depth?: number
    isAddingComment?: boolean
    canReply?: boolean
  }>(),
  {
    depth: 0,
    isAddingComment: false,
    canReply: true,
  }
)

const emit = defineEmits<{
  'add-reply': [content: string, parentId: Guid]
}>()

const isReplying = ref(false)
const replyDraft = ref('')
const replyInputRef = ref<HTMLTextAreaElement | null>(null)

async function toggleReplyComposer() {
  if (!props.canReply) return

  isReplying.value = !isReplying.value

  if (isReplying.value) {
    await nextTick()
    replyInputRef.value?.focus()
  }
}

function closeReplyComposer() {
  isReplying.value = false
  replyDraft.value = ''
}

function submitReply() {
  const content = replyDraft.value.trim()

  if (!content || props.isAddingComment || !props.canReply) return

  emit('add-reply', content, props.comment.id)
  closeReplyComposer()
}

function forwardReply(content: string, parentId: Guid) {
  emit('add-reply', content, parentId)
}
</script>

<style scoped>
.task-comment-thread {
  position: relative;
  display: flex;
  align-items: flex-start;
  gap: 10px;
}

.task-comment-thread.is-reply {
  margin-top: 10px;
}

.task-comment-avatar {
  width: 28px;
  height: 28px;
  flex-shrink: 0;
  border-radius: 999px;
  object-fit: cover;
  border: 1px solid #2f2f2f;
  background: #242424;
}

.task-comment-avatar.small {
  width: 24px;
  height: 24px;
}

.task-comment-thread-main {
  min-width: 0;
  flex: 1;
}

.task-comment-bubble {
  width: fit-content;
  max-width: 100%;
  padding: 9px 11px;
  border: 1px solid #242424;
  border-radius: 15px;
  border-top-left-radius: 5px;
  background: #1a1a1a;
}

.task-comment-thread.is-reply .task-comment-bubble {
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
  font-weight: 750;
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
  white-space: pre-wrap;
}

.task-comment-actions {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
  padding: 5px 2px 0;
}

.task-comment-actions span,
.task-comment-actions button {
  color: #737373;
  font-size: 11px;
  line-height: 1;
}

.task-comment-actions button {
  border: 0;
  padding: 0;
  background: transparent;
  font-weight: 750;
}

.task-comment-actions button:hover:not(:disabled) {
  color: #d4d4d4;
  text-decoration: underline;
}

.task-comment-actions button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.task-comment-children {
  position: relative;
  margin-top: 8px;
  margin-left: 2px;
  padding-left: 18px;
  display: flex;
  flex-direction: column;
  gap: 0;
}

.task-comment-children::before {
  content: '';
  position: absolute;
  top: 0;
  bottom: 10px;
  left: 5px;
  width: 1px;
  background: #2f2f2f;
}

.task-reply-composer {
  margin-top: 8px;
  padding: 8px;
  border: 1px solid #2a2a2a;
  border-radius: 13px;
  background: #141414;
}

.task-reply-composer textarea {
  width: 100%;
  min-height: 34px;
  max-height: 110px;
  border: 0;
  outline: 0;
  resize: vertical;
  padding: 3px 2px;
  color: #f1f1f1;
  background: transparent;
  font-size: 13px;
  line-height: 1.45;
}

.task-reply-composer textarea::placeholder {
  color: #737373;
}

.task-reply-actions {
  display: flex;
  justify-content: flex-end;
  gap: 7px;
  padding-top: 6px;
}

.task-reply-actions button {
  min-height: 28px;
  border: 1px solid #2f2f2f;
  border-radius: 8px;
  padding: 0 10px;
  color: #d4d4d4;
  background: #101010;
  font-size: 12px;
  font-weight: 750;
}

.task-reply-actions button.primary {
  color: #101010;
  background: #f1f1f1;
  border-color: #f1f1f1;
}

.task-reply-actions button:hover:not(:disabled) {
  filter: brightness(1.08);
}

.task-reply-actions button:disabled,
.task-reply-composer textarea:disabled {
  opacity: 0.48;
  cursor: not-allowed;
}
</style>
