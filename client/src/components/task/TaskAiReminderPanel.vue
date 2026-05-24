<template>
  <Teleport to="body">
    <Transition name="ai-reminder-scrim">
      <div
        v-if="open"
        class="ai-reminder-scrim"
        @click="emit('close')"
      ></div>
    </Transition>

    <Transition name="ai-reminder-panel">
      <aside
        v-if="open"
        class="ai-reminder-panel"
        role="dialog"
        aria-modal="true"
        aria-labelledby="ai-reminder-title"
        @click.stop
      >
        <header class="ai-reminder-header">
          <div class="ai-reminder-title-block">
            <div>
              <p>Trung tâm nhắc việc AI</p>
              <h2 id="ai-reminder-title">Nhắc việc thông minh</h2>
            </div>
          </div>

          <div class="ai-reminder-header-actions">
            <button
              type="button"
              class="ai-reminder-icon-btn"
              title="Tải lại nhắc việc"
              :disabled="isLoading"
              @click="emit('refresh')"
            >
              <span
                class="material-symbols-outlined"
                :class="{ spinning: isLoading }"
              >
                refresh
              </span>
            </button>

            <button
              type="button"
              class="ai-reminder-icon-btn"
              title="Đóng"
              @click="emit('close')"
            >
              <span class="material-symbols-outlined">close</span>
            </button>
          </div>
        </header>

        <section class="ai-reminder-hero">
          <div>
            <span class="ai-reminder-kicker">{{ heroKicker }}</span>
            <strong>{{ heroTitle }}</strong>
            <p>{{ heroMessage }}</p>
          </div>

          <span class="ai-reminder-score" :class="heroSeverityClass">
            {{ summary.critical + summary.warning }}
          </span>
        </section>

        <section class="ai-reminder-stats" aria-label="Tóm tắt nhắc việc">
          <article>
            <span>Quá hạn</span>
            <strong>{{ summary.overdue }}</strong>
          </article>

          <article>
            <span>Hôm nay</span>
            <strong>{{ summary.dueToday + summary.dueSoon }}</strong>
          </article>

          <article>
            <span>Ưu tiên cao</span>
            <strong>{{ summary.highPriority }}</strong>
          </article>

          <article>
            <span>Chưa có hạn</span>
            <strong>{{ summary.unscheduled }}</strong>
          </article>
        </section>

        <section class="ai-reminder-meta-row">
          <span>{{ sourceLabel }}</span>
          <span v-if="generatedAt">Cập nhật lúc {{ formatGeneratedAt(generatedAt) }}</span>
        </section>

        <section class="ai-reminder-tabs" aria-label="Bộ lọc nhắc việc">
          <button
            v-for="filter in filters"
            :key="filter.value"
            type="button"
            :class="{ active: activeFilter === filter.value }"
            @click="activeFilter = filter.value"
          >
            {{ filter.label }}
            <span>{{ filter.count }}</span>
          </button>
        </section>

        <main class="ai-reminder-body">
          <div
            v-if="error"
            class="ai-reminder-warning"
          >
            <span class="material-symbols-outlined">info</span>
            <p>{{ error }}</p>
          </div>

          <div
            v-if="isLoading && !reminders.length"
            class="ai-reminder-skeleton-list"
          >
            <div
              v-for="index in 5"
              :key="index"
              class="ai-reminder-skeleton-card"
            >
              <span></span>
              <span></span>
              <span></span>
            </div>
          </div>

          <section
            v-else-if="!visibleReminders.length"
            class="ai-reminder-empty"
          >
            <span class="material-symbols-outlined">task_alt</span>
            <h3>Hiện tại khá ổn áp</h3>
            <p>AI chưa thấy task nào cần réo bạn quá gắt. Nhưng nhớ refresh nếu vừa tạo task mới nha.</p>
          </section>

          <template v-else>
            <section
              v-if="focusPlan.length"
              class="ai-reminder-focus"
            >
              <div class="ai-reminder-section-head">
                <span>Kế hoạch tập trung</span>
                <strong>{{ focusPlan.length }} việc nên nhìn trước</strong>
              </div>

              <ol>
                <li
                  v-for="reminder in focusPlan"
                  :key="`focus:${reminder.id}`"
                >
                  <span :class="`severity-${reminder.severity}`"></span>
                  <strong>{{ reminder.title }}</strong>
                  <small>{{ reminder.timeLabel }}</small>
                </li>
              </ol>
            </section>

            <section class="ai-reminder-list">
              <article
                v-for="reminder in visibleReminders"
                :key="reminder.id"
                class="ai-reminder-card"
                :class="[`severity-${reminder.severity}`, `category-${reminder.category}`]"
              >
                <span class="ai-reminder-card-icon">
                  <span class="material-symbols-outlined">{{ reminder.icon }}</span>
                </span>

                <div class="ai-reminder-card-main">
                  <div class="ai-reminder-card-top">
                    <span>{{ categoryLabel(reminder.category) }}</span>
                    <time>{{ reminder.timeLabel }}</time>
                  </div>

                  <h3>{{ reminder.title }}</h3>
                  <strong>{{ reminder.headline }}</strong>
                  <p>{{ reminder.message }}</p>

                  <div class="ai-reminder-task-meta">
                    <span>{{ statusLabel(reminder.status) }}</span>
                    <span>{{ priorityLabel(reminder.priority) }}</span>
                    <span>{{ reminder.source === 'assigned' ? 'Giao cho tôi' : 'Việc trong không gian' }}</span>
                  </div>

                  <div class="ai-reminder-card-actions">
                    <button
                      type="button"
                      @click="jumpToTaskSection"
                    >
                      <span class="material-symbols-outlined">arrow_downward</span>
                      {{ reminder.actionText }}
                    </button>
                  </div>
                </div>
              </article>
            </section>
          </template>
        </main>
      </aside>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import type {
  AiReminderCategory,
  AiReminderSummary,
  AiTaskReminder,
} from '@/modules/task/composables/useTaskAiReminders'

const props = defineProps<{
  open: boolean
  reminders: AiTaskReminder[]
  summary: AiReminderSummary
  isLoading: boolean
  error: string | null
  generatedAt: Date | null
}>()

const emit = defineEmits<{
  close: []
  refresh: []
  'jump-to-tasks': []
}>()

type ReminderFilter = 'all' | 'urgent' | 'today' | 'planning'

const activeFilter = ref<ReminderFilter>('all')

const urgentCount = computed(() => {
  return props.reminders.filter(
    (reminder) => reminder.severity === 'critical' || reminder.severity === 'warning'
  ).length
})

const todayCount = computed(() => {
  return props.reminders.filter(
    (reminder) => reminder.category === 'due-today' || reminder.category === 'due-soon'
  ).length
})

const planningCount = computed(() => {
  return props.reminders.filter(
    (reminder) =>
      reminder.category === 'due-week' ||
      reminder.category === 'high-priority' ||
      reminder.category === 'unscheduled' ||
      reminder.category === 'in-progress'
  ).length
})

const filters = computed(() => [
  {
    value: 'all' as const,
    label: 'Tất cả',
    count: props.summary.total,
  },
  {
    value: 'urgent' as const,
    label: 'Gấp',
    count: urgentCount.value,
  },
  {
    value: 'today' as const,
    label: 'Hôm nay',
    count: todayCount.value,
  },
  {
    value: 'planning' as const,
    label: 'Lên kế hoạch',
    count: planningCount.value,
  },
])

const visibleReminders = computed(() => {
  if (activeFilter.value === 'urgent') {
    return props.reminders.filter(
      (reminder) => reminder.severity === 'critical' || reminder.severity === 'warning'
    )
  }

  if (activeFilter.value === 'today') {
    return props.reminders.filter(
      (reminder) => reminder.category === 'due-today' || reminder.category === 'due-soon'
    )
  }

  if (activeFilter.value === 'planning') {
    return props.reminders.filter(
      (reminder) =>
        reminder.category === 'due-week' ||
        reminder.category === 'high-priority' ||
        reminder.category === 'unscheduled' ||
        reminder.category === 'in-progress'
    )
  }

  return props.reminders
})

const focusPlan = computed(() => {
  return visibleReminders.value.slice(0, 3)
})

const heroKicker = computed(() => {
  if (props.isLoading) return 'Đang quét công việc'
  if (props.summary.critical > 0) return 'Có việc cháy nhẹ rồi'
  if (props.summary.warning > 0) return 'Nên xử lý sớm'
  if (props.summary.total > 0) return 'Có vài việc cần để mắt'

  return 'Không gian đang ổn'
})

const heroTitle = computed(() => {
  if (props.isLoading) return 'AI đang rà soát hạn và độ ưu tiên'
  if (props.summary.critical > 0) return `${props.summary.critical} nhắc nhở gấp cần xem ngay`
  if (props.summary.warning > 0) return `${props.summary.warning} việc nên xử lý trong hôm nay`
  if (props.summary.total > 0) return `${props.summary.total} gợi ý nhắc việc được tạo`

  return 'Chưa có công việc nào cần nhắc mạnh'
})

const heroMessage = computed(() => {
  if (props.summary.overdue > 0) {
    return `Có ${props.summary.overdue} task quá hạn. AI đề xuất xử lý các task này trước để tránh trễ dây chuyền.`
  }

  if (props.summary.dueSoon > 0) {
    return `Có ${props.summary.dueSoon} task sắp tới hạn rất gần. Đây là nhóm nên ưu tiên focus.`
  }

  if (props.summary.dueToday > 0) {
    return `Có ${props.summary.dueToday} task cần xong trong hôm nay. Nhìn nhẹ thôi nhưng đừng để nó hóa boss cuối.`
  }

  if (props.summary.highPriority > 0) {
    return `Có ${props.summary.highPriority} công việc ưu tiên cao. AI đang theo dõi nhóm này cho bạn.`
  }

  return 'AI sẽ tiếp tục nhắc khi có công việc quá hạn, sắp tới hạn, ưu tiên cao hoặc lâu chưa cập nhật.'
})

const heroSeverityClass = computed(() => {
  if (props.summary.critical > 0) return 'critical'
  if (props.summary.warning > 0) return 'warning'
  if (props.summary.total > 0) return 'info'

  return 'gentle'
})

const sourceLabel = computed(() => {
  const assigned = props.summary.assignedTaskCount
  const workspace = props.summary.workspaceTaskCount

  return `${assigned} việc của bạn · ${workspace} việc trong không gian`
})

watch(
  () => props.open,
  (isOpen) => {
    document.body.classList.toggle('ai-reminder-lock-scroll', isOpen)

    if (isOpen) {
      emit('refresh')
    }
  }
)

function categoryLabel(category: AiReminderCategory) {
  return {
    overdue: 'Quá hạn',
    'due-soon': 'Sắp tới hạn',
    'due-today': 'Hôm nay',
    'due-week': 'Sắp tới',
    'high-priority': 'Ưu tiên',
    'in-progress': 'Đang làm',
    unscheduled: 'Chưa có hạn',
  }[category]
}

function statusLabel(status: string) {
  const normalized = status.trim().toLowerCase()

  if (normalized === 'todo') return 'Cần làm'
  if (normalized === 'doing') return 'Đang làm'
  if (normalized === 'done') return 'Đã xong'

  return status || 'Chưa rõ trạng thái'
}

function priorityLabel(priority: string) {
  const normalized = priority.trim().toLowerCase()

  if (normalized === 'high') return 'Ưu tiên cao'
  if (normalized === 'medium') return 'Ưu tiên vừa'
  if (normalized === 'low') return 'Ưu tiên thấp'

  return priority || 'Chưa có ưu tiên'
}

function formatGeneratedAt(value: Date) {
  return value.toLocaleTimeString('vi-VN', {
    hour: '2-digit',
    minute: '2-digit',
  })
}

function jumpToTaskSection() {
  emit('jump-to-tasks')
  emit('close')
}

function handleKeydown(event: KeyboardEvent) {
  if (!props.open) return

  if (event.key === 'Escape') {
    emit('close')
  }
}

window.addEventListener('keydown', handleKeydown)

onBeforeUnmount(() => {
  window.removeEventListener('keydown', handleKeydown)
  document.body.classList.remove('ai-reminder-lock-scroll')
})
</script>

<style scoped src="./css/TaskAiReminderPanel.css"></style>
