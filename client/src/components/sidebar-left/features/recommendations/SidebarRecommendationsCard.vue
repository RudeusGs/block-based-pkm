<template>
  <section class="lunar-ai-card">
    <div class="lunar-ai-head">
      <div class="lunar-ai-title-wrap">
        <span class="lunar-ai-icon">
          <i class="bi bi-magic"></i>
        </span>

        <div class="lunar-ai-title-text">
          <span class="lunar-ai-title">
            AI gợi ý task
          </span>

          <span class="lunar-ai-subtitle">
            Dựa trên workspace/page đang chọn
          </span>
        </div>
      </div>

      <button
        type="button"
        class="lunar-ai-generate-btn"
        :disabled="!workspaceId || isGenerating"
        title="Tạo gợi ý task"
        @click.stop="emit('generate')"
      >
        <span
          v-if="isGenerating"
          class="lunar-ai-spinner"
        ></span>

        <i
          v-else
          class="bi bi-arrow-clockwise"
        ></i>
      </button>
    </div>

    <div
      v-if="!workspaceId"
      class="lunar-ai-empty"
    >
      Chọn workspace để AI gợi ý task.
    </div>

    <div
      v-else-if="error"
      class="lunar-ai-error"
    >
      <p>{{ error }}</p>

      <button
        type="button"
        @click.stop="emit('retry')"
      >
        Tải lại
      </button>
    </div>

    <div
      v-else-if="isLoading"
      class="lunar-ai-empty"
    >
      Đang tải gợi ý…
    </div>

    <div
      v-else-if="!recommendations.length"
      class="lunar-ai-empty"
    >
      Chưa có gợi ý. Nhấn làm mới để AI tạo task phù hợp.
    </div>

    <div
      v-else
      class="lunar-ai-list"
    >
      <article
        v-for="recommendation in recommendations"
        :key="recommendation.id"
        class="lunar-ai-item"
      >
        <div class="lunar-ai-item-main">
          <div class="lunar-ai-item-title">
            {{ recommendation.taskTitle }}
          </div>

          <div
            v-if="recommendation.reason"
            class="lunar-ai-item-reason"
          >
            {{ recommendation.reason }}
          </div>

          <div class="lunar-ai-item-meta">
            <span>{{ recommendation.taskPriority }}</span>
            <span>Score {{ recommendation.score }}</span>
          </div>
        </div>

        <div class="lunar-ai-actions">
          <button
            type="button"
            title="Chấp nhận gợi ý"
            @click.stop="emit('accept', recommendation.id)"
          >
            <i class="bi bi-check-lg"></i>
          </button>

          <button
            type="button"
            title="Từ chối gợi ý"
            @click.stop="emit('reject', recommendation.id)"
          >
            <i class="bi bi-x-lg"></i>
          </button>
        </div>
      </article>
    </div>
  </section>
</template>

<script setup lang="ts">
import type { Guid } from '@/api/models/common.model'
import type { TaskRecommendationResponse } from '@/api/models/recommendation.model'

defineProps<{
  workspaceId: Guid | null
  recommendations: TaskRecommendationResponse[]
  isLoading: boolean
  isGenerating: boolean
  error: string | null
}>()

const emit = defineEmits<{
  generate: []
  retry: []
  accept: [recommendationId: Guid]
  reject: [recommendationId: Guid]
}>()
</script>