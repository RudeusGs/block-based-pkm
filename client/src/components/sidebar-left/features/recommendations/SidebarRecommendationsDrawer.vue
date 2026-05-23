<template>
  <Teleport to="body">
    <Transition name="notion-ai-overlay">
      <div
        v-if="open"
        class="notion-ai-overlay"
        @click="emit('close')"
      ></div>
    </Transition>

    <Transition name="notion-ai-panel">
      <aside
        v-if="open"
        class="notion-ai-panel"
        role="dialog"
        aria-modal="true"
        aria-label="AI task suggestions"
        @click.stop
      >
        <header class="notion-ai-header">
          <div class="notion-ai-title-group">
            <div class="notion-ai-logo">
              <i class="bi bi-stars"></i>
            </div>

            <div class="notion-ai-title-content">
              <span>AI Prioritizer</span>
              <h2>Task nên ưu tiên</h2>
            </div>
          </div>

          <button
            type="button"
            class="notion-ai-icon-btn"
            title="Đóng"
            aria-label="Đóng AI Prioritizer"
            @click="emit('close')"
          >
            <i class="bi bi-x-lg"></i>
          </button>
        </header>

        <main class="notion-ai-body">
          <section class="notion-ai-meta">
            <div class="notion-ai-meta-item">
              <i class="bi bi-grid-1x2"></i>
              <div>
                <span>Bộ lọc thông minh</span>
                <strong>Gộp task trùng nội dung</strong>
              </div>
            </div>

            <div class="notion-ai-meta-item">
              <i class="bi bi-broadcast-pin"></i>
              <div>
                <span>Cách xếp hạng</span>
                <strong>Deadline · assignee · priority</strong>
              </div>
            </div>
          </section>

          <p
            v-if="realtimeError"
            class="notion-ai-realtime-warning"
          >
            <i class="bi bi-wifi-off"></i>
            {{ realtimeError }}
          </p>

          <section class="notion-ai-command-box">
            <div>
              <h3>Cố vấn ưu tiên công việc</h3>
              <p>
                AI lọc task đang mở, gộp các việc trùng nội dung như “học bài” và “học bài 1”, rồi chỉ đề xuất việc đáng làm nhất.
              </p>
            </div>

            <button
              type="button"
              class="notion-ai-primary"
              :disabled="!workspaceId || isGenerating"
              @click="emit('generate')"
            >
              <span
                v-if="isGenerating"
                class="notion-ai-spinner"
              ></span>

              <i
                v-else
                class="bi bi-magic"
              ></i>

              {{ isGenerating ? 'Đang phân tích' : 'Phân tích task' }}
            </button>
          </section>

          <section class="notion-ai-list-head">
            <div>
              <strong>{{ recommendations.length }}</strong>
              <span>task ưu tiên</span>
            </div>

            <button
              type="button"
              class="notion-ai-ghost"
              :disabled="isLoading"
              @click="emit('retry')"
            >
              <i
                class="bi bi-arrow-clockwise"
                :class="{ spinning: isLoading }"
              ></i>
              Tải lại
            </button>
          </section>

          <section class="notion-ai-content">
            <div
              v-if="error"
              class="notion-ai-empty notion-ai-error"
            >
              <div class="notion-ai-empty-icon">
                <i class="bi bi-exclamation-triangle"></i>
              </div>

              <h3>Không tải được gợi ý</h3>
              <p>{{ error }}</p>

              <button
                type="button"
                class="notion-ai-primary small"
                @click="emit('retry')"
              >
                Thử lại
              </button>
            </div>

            <div
              v-else-if="isLoading"
              class="notion-ai-skeleton-list"
            >
              <div
                v-for="index in 4"
                :key="index"
                class="notion-ai-skeleton-card"
              >
                <span></span>
                <span></span>
                <span></span>
              </div>
            </div>

            <div
              v-else-if="!recommendations.length"
              class="notion-ai-empty"
            >
              <div class="notion-ai-empty-icon">
                <i class="bi bi-lightbulb"></i>
              </div>

              <h3>Chưa có task nên ưu tiên</h3>
              <p>
                AI chưa thấy task nào đủ tín hiệu để đề xuất. Hãy thêm deadline, priority hoặc assignee để gợi ý chính xác hơn.
              </p>
            </div>

            <div
              v-else
              class="notion-ai-list"
            >
              <article
                v-for="recommendation in recommendations"
                :key="recommendation.id"
                class="notion-ai-card"
              >
                <div class="notion-ai-card-header">
                  <span
                    class="notion-ai-priority"
                    :class="priorityClass(recommendation.taskPriority)"
                  >
                    {{ priorityLabel(recommendation.taskPriority) }}
                  </span>

                  <span class="notion-ai-score">
                    Độ ưu tiên {{ scoreLabel(recommendation.score) }}
                  </span>
                </div>

                <h3>{{ recommendation.taskTitle }}</h3>

                <div class="notion-ai-location">
                  <span>
                    <i class="bi bi-grid-1x2"></i>
                    {{ recommendation.workspaceName }}
                  </span>

                  <span>
                    <i class="bi bi-file-earmark-text"></i>
                    {{ recommendation.pageName }}
                  </span>
                </div>

                <p
                  v-if="recommendation.taskDescription"
                  class="notion-ai-description"
                >
                  {{ recommendation.taskDescription }}
                </p>

                <div
                  v-if="recommendation.reason"
                  class="notion-ai-reason"
                >
                  <i class="bi bi-stars"></i>
                  <span>{{ recommendation.reason }}</span>
                </div>

                <div class="notion-ai-actions">
                  <button
                    type="button"
                    class="notion-ai-create"
                    @click="emit('accept', recommendation.id)"
                  >
                    <i class="bi bi-check2"></i>
                    Bắt đầu làm
                  </button>

                  <button
                    type="button"
                    class="notion-ai-skip"
                    @click="emit('reject', recommendation.id)"
                  >
                    Bỏ qua
                  </button>
                </div>
              </article>
            </div>
          </section>
        </main>
      </aside>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import type { Guid } from '@/api/models/common.model'
import type { TaskRecommendationViewModel } from './useSidebarRecommendations'

defineProps<{
  open: boolean
  workspaceId: Guid | null
  workspaceName: string
  pageName: string
  recommendations: TaskRecommendationViewModel[]
  isLoading: boolean
  isGenerating: boolean
  error: string | null
  realtimeError?: string | null
}>()

const emit = defineEmits<{
  close: []
  generate: []
  retry: []
  accept: [recommendationId: Guid]
  reject: [recommendationId: Guid]
}>()

function priorityLabel(priority: string) {
  const normalized = priority.trim().toLowerCase()

  if (normalized === 'high') return 'High'
  if (normalized === 'medium') return 'Medium'
  if (normalized === 'low') return 'Low'

  return priority || 'Priority'
}

function priorityClass(priority: string) {
  const normalized = priority.trim().toLowerCase()

  return {
    high: normalized === 'high',
    medium: normalized === 'medium',
    low: normalized === 'low',
  }
}

function scoreLabel(score: number) {
  if (!Number.isFinite(score)) return '0'

  return Number.isInteger(score) ? `${score}` : score.toFixed(1)
}
</script>

<style scoped>
.notion-ai-overlay {
  position: fixed;
  inset: 0;
  z-index: 940;
  background: rgba(0, 0, 0, 0.42);
  backdrop-filter: blur(2px);
}

.notion-ai-panel {
  position: fixed;
  z-index: 950;
  top: 0;
  right: 0;
  bottom: 0;
  left: auto;
  width: min(560px, calc(100vw - 54px));
  display: flex;
  flex-direction: column;
  overflow: hidden;
  border-left: 1px solid #2a2a2a;
  border-top: 0;
  border-right: 0;
  border-bottom: 0;
  border-radius: 0;
  color: #e6e6e6;
  background:
    linear-gradient(180deg, rgba(32, 32, 32, 0.98), rgba(20, 20, 20, 0.98));
  box-shadow:
    -24px 0 80px rgba(0, 0, 0, 0.5),
    inset 1px 0 0 rgba(255, 255, 255, 0.04);
}

.notion-ai-header {
  min-height: 64px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 14px 16px;
  border-bottom: 1px solid #2a2a2a;
  background: rgba(25, 25, 25, 0.92);
}

.notion-ai-title-group {
  min-width: 0;
  display: flex;
  align-items: center;
  gap: 11px;
}

.notion-ai-logo {
  width: 34px;
  height: 34px;
  border: 1px solid #333;
  border-radius: 8px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: #f1f1f1;
  background: #202020;
}

.notion-ai-title-content {
  min-width: 0;
}

.notion-ai-title-content span {
  display: block;
  color: #8f8f8f;
  font-size: 12px;
  font-weight: 500;
}

.notion-ai-title-content h2 {
  overflow: hidden;
  margin: 2px 0 0;
  color: #f3f3f3;
  font-size: 16px;
  font-weight: 650;
  line-height: 1.25;
  white-space: nowrap;
  text-overflow: ellipsis;
}

.notion-ai-icon-btn {
  width: 32px;
  height: 32px;
  border: 0;
  border-radius: 7px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: #9b9b9b;
  background: transparent;
  transition:
    background 0.15s ease,
    color 0.15s ease;
}

.notion-ai-icon-btn:hover {
  color: #f1f1f1;
  background: #2a2a2a;
}

.notion-ai-body {
  min-height: 0;
  display: flex;
  flex: 1;
  flex-direction: column;
  overflow: hidden;
}

.notion-ai-meta {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 8px;
  padding: 14px 16px 0;
}

.notion-ai-meta-item {
  min-width: 0;
  border: 1px solid #2a2a2a;
  border-radius: 9px;
  display: flex;
  align-items: center;
  gap: 9px;
  padding: 9px 10px;
  background: #1f1f1f;
}

.notion-ai-meta-item i {
  flex-shrink: 0;
  color: #9b9b9b;
  font-size: 14px;
}

.notion-ai-meta-item div {
  min-width: 0;
}

.notion-ai-meta-item span {
  display: block;
  color: #7f7f7f;
  font-size: 11px;
  line-height: 1.2;
}

.notion-ai-meta-item strong {
  display: block;
  overflow: hidden;
  margin-top: 3px;
  color: #dcdcdc;
  font-size: 12px;
  font-weight: 560;
  white-space: nowrap;
  text-overflow: ellipsis;
}

.notion-ai-realtime-warning {
  display: flex;
  align-items: center;
  gap: 8px;
  margin: 12px 16px 0;
  padding: 10px 12px;
  border: 1px solid rgba(245, 158, 11, 0.32);
  border-radius: 12px;
  color: #fbbf24;
  background: rgba(245, 158, 11, 0.08);
  font-size: 12px;
}

.notion-ai-command-box {
  margin: 12px 16px 0;
  border: 1px solid #303030;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 12px;
  background: #202020;
}

.notion-ai-command-box h3 {
  margin: 0;
  color: #f1f1f1;
  font-size: 14px;
  font-weight: 620;
}

.notion-ai-command-box p {
  margin: 5px 0 0;
  color: #9b9b9b;
  font-size: 12px;
  line-height: 1.45;
}

.notion-ai-primary,
.notion-ai-create {
  min-height: 34px;
  border: 0;
  border-radius: 7px;
  display: inline-flex;
  flex-shrink: 0;
  align-items: center;
  justify-content: center;
  gap: 7px;
  padding: 0 11px;
  color: #191919;
  background: #f1f1f1;
  font-size: 13px;
  font-weight: 620;
  transition:
    background 0.15s ease,
    opacity 0.15s ease,
    transform 0.15s ease;
}

.notion-ai-primary:hover:not(:disabled),
.notion-ai-create:hover {
  background: #ffffff;
}

.notion-ai-primary:active:not(:disabled),
.notion-ai-create:active {
  transform: translateY(1px);
}

.notion-ai-primary:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}

.notion-ai-primary.small {
  margin-top: 14px;
  min-height: 32px;
}

.notion-ai-list-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 14px 16px 10px;
}

.notion-ai-list-head div {
  display: flex;
  align-items: baseline;
  gap: 6px;
}

.notion-ai-list-head strong {
  color: #f1f1f1;
  font-size: 17px;
  font-weight: 680;
}

.notion-ai-list-head span {
  color: #8f8f8f;
  font-size: 12px;
}

.notion-ai-ghost {
  min-height: 30px;
  border: 1px solid #303030;
  border-radius: 7px;
  display: inline-flex;
  align-items: center;
  gap: 7px;
  padding: 0 10px;
  color: #cfcfcf;
  background: #202020;
  font-size: 12px;
  font-weight: 560;
  transition:
    background 0.15s ease,
    color 0.15s ease,
    opacity 0.15s ease;
}

.notion-ai-ghost:hover:not(:disabled) {
  color: #f1f1f1;
  background: #292929;
}

.notion-ai-ghost:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}

.notion-ai-content {
  min-height: 0;
  flex: 1;
  overflow: hidden;
}

.notion-ai-list {
  height: 100%;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 0 16px 16px;
}

.notion-ai-list::-webkit-scrollbar {
  width: 10px;
}

.notion-ai-list::-webkit-scrollbar-track {
  background: transparent;
}

.notion-ai-list::-webkit-scrollbar-thumb {
  border: 3px solid transparent;
  border-radius: 999px;
  background: #3a3a3a;
  background-clip: content-box;
}

.notion-ai-card {
  border: 1px solid #2a2a2a;
  border-radius: 10px;
  padding: 12px;
  background: #202020;
  transition:
    background 0.15s ease,
    border-color 0.15s ease,
    transform 0.15s ease;
}

.notion-ai-card:hover {
  border-color: #3a3a3a;
  background: #242424;
}

.notion-ai-card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
}

.notion-ai-priority,
.notion-ai-score {
  border-radius: 999px;
  font-size: 11px;
  line-height: 1;
}

.notion-ai-priority {
  padding: 5px 7px;
  color: #cfcfcf;
  background: #2b2b2b;
  font-weight: 620;
}

.notion-ai-priority.high {
  color: #ffb4b4;
  background: rgba(255, 120, 120, 0.14);
}

.notion-ai-priority.medium {
  color: #f2d48b;
  background: rgba(242, 212, 139, 0.13);
}

.notion-ai-priority.low {
  color: #9fd3ad;
  background: rgba(159, 211, 173, 0.13);
}

.notion-ai-score {
  color: #8f8f8f;
  font-weight: 520;
}

.notion-ai-card h3 {
  margin: 11px 0 0;
  color: #eeeeee;
  font-size: 14px;
  font-weight: 620;
  line-height: 1.4;
}

.notion-ai-location {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin: 8px 0 10px;
}

.notion-ai-location span {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  max-width: 100%;
  padding: 5px 8px;
  border: 1px solid #303030;
  border-radius: 999px;
  color: #bdbdbd;
  background: #202020;
  font-size: 12px;
  line-height: 1.2;
}

.notion-ai-location i {
  color: #8b8b8b;
  font-size: 13px;
}

.notion-ai-description {
  margin: 7px 0 0;
  color: #a8a8a8;
  font-size: 13px;
  line-height: 1.5;
}

.notion-ai-reason {
  margin-top: 10px;
  border-left: 2px solid #444;
  display: flex;
  gap: 8px;
  padding: 7px 9px;
  color: #b9b9b9;
  background: #252525;
  font-size: 12px;
  line-height: 1.45;
}

.notion-ai-reason i {
  margin-top: 1px;
  color: #d6d6d6;
}

.notion-ai-actions {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-top: 12px;
}

.notion-ai-create {
  min-height: 32px;
}

.notion-ai-skip {
  min-height: 32px;
  border: 0;
  border-radius: 7px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0 10px;
  color: #9b9b9b;
  background: transparent;
  font-size: 13px;
  font-weight: 560;
}

.notion-ai-skip:hover {
  color: #f1f1f1;
  background: #2a2a2a;
}

.notion-ai-empty {
  height: calc(100% - 16px);
  margin: 0 16px 16px;
  border: 1px dashed #323232;
  border-radius: 10px;
  display: flex;
  min-height: 240px;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 28px;
  text-align: center;
  background: #1d1d1d;
}

.notion-ai-empty-icon {
  width: 38px;
  height: 38px;
  border: 1px solid #343434;
  border-radius: 9px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: #bdbdbd;
  background: #242424;
}

.notion-ai-empty h3 {
  margin: 13px 0 0;
  color: #eeeeee;
  font-size: 15px;
  font-weight: 620;
}

.notion-ai-empty p {
  max-width: 290px;
  margin: 6px 0 0;
  color: #949494;
  font-size: 13px;
  line-height: 1.5;
}

.notion-ai-error {
  border-color: rgba(255, 120, 120, 0.28);
}

.notion-ai-error .notion-ai-empty-icon {
  color: #ffb4b4;
  border-color: rgba(255, 120, 120, 0.24);
  background: rgba(255, 120, 120, 0.1);
}

.notion-ai-skeleton-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 0 16px 16px;
}

.notion-ai-skeleton-card {
  border: 1px solid #2a2a2a;
  border-radius: 10px;
  padding: 12px;
  background: #202020;
}

.notion-ai-skeleton-card span {
  display: block;
  height: 8px;
  border-radius: 999px;
  background: linear-gradient(90deg, #252525, #323232, #252525);
  background-size: 220% 100%;
  animation: notion-ai-skeleton 1.2s ease-in-out infinite;
}

.notion-ai-skeleton-card span:nth-child(1) {
  width: 32%;
}

.notion-ai-skeleton-card span:nth-child(2) {
  width: 86%;
  margin-top: 14px;
}

.notion-ai-skeleton-card span:nth-child(3) {
  width: 58%;
  margin-top: 10px;
}

.notion-ai-spinner {
  width: 14px;
  height: 14px;
  border: 2px solid currentColor;
  border-right-color: transparent;
  border-radius: 999px;
  animation: notion-ai-spin 0.7s linear infinite;
}

.spinning {
  animation: notion-ai-spin 0.8s linear infinite;
}

.notion-ai-overlay-enter-active,
.notion-ai-overlay-leave-active,
.notion-ai-panel-enter-active,
.notion-ai-panel-leave-active {
  transition:
    opacity 0.16s ease,
    transform 0.16s ease;
}

.notion-ai-overlay-enter-from,
.notion-ai-overlay-leave-to {
  opacity: 0;
}

.notion-ai-panel-enter-from,
.notion-ai-panel-leave-to {
  opacity: 0;
  transform: translateX(12px) scale(0.985);
}

@keyframes notion-ai-spin {
  to {
    transform: rotate(360deg);
  }
}

@keyframes notion-ai-skeleton {
  0% {
    background-position: 120% 0;
  }

  100% {
    background-position: -120% 0;
  }
}

@media (max-width: 720px) {
  .notion-ai-panel {
    inset: 0;
    width: auto;
    border-radius: 0;
  }

  .notion-ai-meta {
    grid-template-columns: 1fr;
  }

  .notion-ai-command-box {
    align-items: stretch;
    flex-direction: column;
  }

  .notion-ai-primary {
    width: 100%;
  }
}

@media (prefers-reduced-motion: reduce) {
  .notion-ai-overlay-enter-active,
  .notion-ai-overlay-leave-active,
  .notion-ai-panel-enter-active,
  .notion-ai-panel-leave-active,
  .notion-ai-spinner,
  .spinning,
  .notion-ai-skeleton-card span {
    transition: none;
    animation: none;
  }
}
</style>



