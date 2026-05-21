<template>
  <div class="workspace-shell min-vh-100 d-flex text-on-surface">
    <SidebarLeft />

    <section class="workspace-page-center flex-grow-1 min-vh-100 text-on-surface">
      <AppTopNav
        @jump-to-tasks="scrollToTasks"
        @open-members="workspaceMembers.open"
      />

      <main class="page-scroll pb-5">
        <div class="page-container container position-relative px-4 pt-4 pb-5">
          <PageEditor
            :page-id="currentPageId"
            :page-title="workspaceNavigation.pageName.value"
            :page-icon="workspaceNavigation.pageIcon.value"
            :workspace-name="workspaceNavigation.workspaceName.value"
          />

          <section
            ref="taskSectionRef"
            class="work-tasks-section mt-5 mb-5"
          >
            <div class="task-section-toolbar d-flex flex-column flex-lg-row justify-content-between align-items-lg-end gap-3 pb-3 border-bottom border-soft mb-3">
              <div>
                <h2 class="section-title d-flex align-items-center gap-2 mb-1">
                  <span class="material-symbols-outlined">table_chart</span>
                  Work Tasks
                </h2>

                <p class="section-subtitle d-flex flex-wrap align-items-center gap-2 mb-0 small text-on-surface-variant">
                  <span>{{ tasks.length }} tasks</span>
                  <span class="tiny-separator"></span>
                  <span>Tasks linked to this page</span>
                </p>
              </div>

              <div class="d-flex flex-wrap align-items-center gap-2">
                <button class="task-action-btn btn btn-sm d-flex align-items-center gap-1">
                  <span class="material-symbols-outlined">filter_list</span>
                  Filter
                </button>

                <button class="task-action-btn btn btn-sm d-flex align-items-center gap-1">
                  <span class="material-symbols-outlined">sort</span>
                  Sort
                </button>

                <button class="new-task-btn btn btn-sm d-flex align-items-center gap-1 ms-lg-2">
                  <span class="material-symbols-outlined">add</span>
                  New Task
                </button>
              </div>
            </div>

            <div class="task-summary d-flex flex-wrap gap-3 small fw-medium text-on-surface-variant mb-3">
              <span class="d-inline-flex align-items-center gap-2">
                <span class="summary-dot summary-todo"></span>
                {{ taskSummary.todo }} To Do
              </span>

              <span class="d-inline-flex align-items-center gap-2">
                <span class="summary-dot summary-doing"></span>
                {{ taskSummary.doing }} Doing
              </span>

              <span class="d-inline-flex align-items-center gap-2">
                <span class="summary-dot summary-done"></span>
                {{ taskSummary.done }} Done
              </span>
            </div>

            <div class="task-database rounded-3 overflow-hidden border">
              <div class="table-responsive">
                <table class="table task-table align-middle mb-0">
                  <thead>
                    <tr>
                      <th class="fw-normal ps-4">Task</th>
                      <th class="fw-normal">Status</th>
                      <th class="fw-normal">Priority</th>
                      <th class="fw-normal">Due Date</th>
                      <th class="fw-normal">Assignee</th>
                    </tr>
                  </thead>

                  <tbody>
                    <tr
                      v-for="task in tasks"
                      :key="task.id"
                      class="task-row"
                      :class="{
                        'task-completed': task.status === 'done',
                        'task-row-selected': selectedTask?.id === task.id,
                      }"
                      tabindex="0"
                      @click="openTaskDetail(task)"
                      @keydown.enter="openTaskDetail(task)"
                    >
                      <td class="ps-4 task-name-cell">
                        <div class="d-flex align-items-center gap-2 fw-semibold text-on-surface">
                          <span class="material-symbols-outlined drag-row-icon">
                            drag_indicator
                          </span>

                          <span :class="{ 'text-decoration-line-through': task.status === 'done' }">
                            {{ task.title }}
                          </span>
                        </div>

                        <div class="task-description small mt-1 ms-4 text-truncate">
                          {{ task.description }}
                        </div>
                      </td>

                      <td>
                        <span
                          class="status-pill d-inline-flex align-items-center gap-2 rounded-pill px-2 py-1"
                          :class="statusClass(task.status)"
                        >
                          <span class="status-pill-dot"></span>
                          {{ statusLabel(task.status) }}
                        </span>
                      </td>

                      <td>
                        <span
                          class="priority-pill d-inline-block rounded-2 px-2 py-1"
                          :class="priorityClass(task.priority)"
                        >
                          {{ priorityLabel(task.priority) }}
                        </span>
                      </td>

                      <td>
                        <span
                          class="task-date small"
                          :class="{ 'date-overdue': task.overdue }"
                        >
                          {{ task.dueDate }}
                        </span>
                      </td>

                      <td>
                        <img
                          class="avatar rounded-circle"
                          :class="{ 'avatar-muted': task.status === 'done' }"
                          :src="task.assignee.avatarUrl"
                          :alt="task.assignee.name"
                        />
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>

              <button class="add-row-btn btn w-100 text-start rounded-0 d-flex align-items-center gap-2 px-4 py-2">
                <span class="material-symbols-outlined">add</span>
                Add a row
              </button>
            </div>
          </section>

          <section class="related-pages-section mt-5 pt-4 border-top border-soft">
            <h3 class="related-title d-flex align-items-center gap-2 mb-3 text-on-surface-variant">
              <span class="material-symbols-outlined">account_tree</span>
              Related Pages
            </h3>

            <div class="row g-3">
              <div
                v-for="subpage in subpages"
                :key="subpage.id"
                class="col-12 col-md-6"
              >
                <a
                  class="related-card d-flex align-items-center gap-3 p-3 rounded-3 border text-decoration-none"
                  href="#"
                >
                  <span class="related-icon d-inline-flex align-items-center justify-content-center rounded-3">
                    {{ subpage.icon }}
                  </span>

                  <span class="flex-grow-1 min-w-0">
                    <span class="related-card-title d-block fw-semibold">
                      {{ subpage.title }}
                    </span>

                    <span class="small text-outline d-block mt-1">
                      Updated {{ subpage.updatedDate }}
                    </span>
                  </span>
                </a>
              </div>
            </div>
          </section>
        </div>
      </main>
    </section>

    <WorkspaceMembersSidebar
      :open="workspaceMembers.isOpen.value"
      :workspace-name="workspaceNavigation.workspaceName.value"
      :members="workspaceMembers.members.value"
      :online-members="workspaceMembers.onlineMembers.value"
      :offline-members="workspaceMembers.offlineMembers.value"
      :member-count-label="workspaceMembers.memberCountLabel.value"
      :is-loading="workspaceMembers.isLoading.value"
      :error="workspaceMembers.error.value"
      @close="workspaceMembers.close"
      @refresh="workspaceMembers.refresh"
    />

    <TaskDetailDrawer
      :open="isTaskDetailOpen"
      :task="selectedTask"
      :comments="selectedTaskComments"
      @close="closeTaskDetail"
      @add-comment="addTaskComment"
    />
  </div>
</template>

<script setup>
import { computed, ref } from 'vue'
import SidebarLeft from '@/components/sidebar-left/SidebarLeft.vue'
import AppTopNav from '@/components/layout/AppTopNav.vue'
import WorkspaceMembersSidebar from '@/components/layout/WorkspaceMembersSidebar.vue'
import TaskDetailDrawer from '@/components/task/TaskDetailDrawer.vue'
import PageEditor from '@/components/editor/PageEditor.vue'
import { useWorkspaceNavigation } from '@/modules/navigation/composables/useWorkspaceNavigation'
import { useWorkspaceMembersSidebar } from '@/modules/workspaces/composables/useWorkspaceMembersSidebar'

const taskSectionRef = ref(null)
const selectedTask = ref(null)
const isTaskDetailOpen = ref(false)

const workspaceNavigation = useWorkspaceNavigation()

const currentWorkspaceId = computed(() => {
  return workspaceNavigation.workspace.value?.id ?? null
})

const currentPageId = computed(() => {
  return workspaceNavigation.page.value?.id ?? null
})

const workspaceMembers = useWorkspaceMembersSidebar(currentWorkspaceId)

const tasks = ref([
  {
    id: 'task-1',
    title: 'Design System Audit',
    description: 'Review spacing, color tokens, hover/focus states',
    status: 'doing',
    priority: 'high',
    dueDate: 'Oct 15',
    overdue: false,
    assignee: {
      name: 'Nhi',
      avatarUrl: 'https://i.pravatar.cc/80?img=12',
    },
  },
  {
    id: 'task-2',
    title: 'API Documentation',
    description: 'Draft block, page, task realtime endpoints',
    status: 'todo',
    priority: 'medium',
    dueDate: 'Oct 22',
    overdue: false,
    assignee: {
      name: 'Khoa',
      avatarUrl: 'https://i.pravatar.cc/80?img=15',
    },
  },
  {
    id: 'task-3',
    title: 'Kickoff Meeting',
    description: 'Align project scope and ownership',
    status: 'done',
    priority: 'low',
    dueDate: 'Oct 10',
    overdue: true,
    assignee: {
      name: 'An',
      avatarUrl: 'https://i.pravatar.cc/80?img=18',
    },
  },
])

const taskCommentsByTaskId = ref({
  'task-1': [
    {
      id: 'comment-1',
      author: {
        name: 'Linh Tran',
        role: 'Product',
        avatarUrl: 'https://i.pravatar.cc/80?img=20',
      },
      content:
        'Mình nghĩ nên check thêm spacing token và trạng thái hover/focus để đồng bộ với sidebar mới.',
      createdAt: '12m ago',
      likes: 3,
      replies: [
        {
          id: 'reply-1',
          author: {
            name: 'Nhi',
            role: 'Assignee',
            avatarUrl: 'https://i.pravatar.cc/80?img=12',
          },
          content: 'Đúng rồi, mình sẽ thêm checklist riêng cho interaction state.',
        },
      ],
    },
  ],
  'task-2': [
    {
      id: 'comment-2',
      author: {
        name: 'Minh Pham',
        role: 'Engineer',
        avatarUrl: 'https://i.pravatar.cc/80?img=21',
      },
      content:
        'Phần endpoint v2 nên tách riêng auth, task, recommendation để dễ review.',
      createdAt: '20m ago',
      likes: 2,
      replies: [],
    },
  ],
  'task-3': [
    {
      id: 'comment-3',
      author: {
        name: 'Linh Tran',
        role: 'Product',
        avatarUrl: 'https://i.pravatar.cc/80?img=20',
      },
      content:
        'Meeting này đã hoàn tất, chỉ còn cần tổng hợp lại owner cho từng hạng mục.',
      createdAt: '1d ago',
      likes: 4,
      replies: [],
    },
  ],
})

const selectedTaskComments = computed(() => {
  if (!selectedTask.value) return []

  return taskCommentsByTaskId.value[selectedTask.value.id] || []
})

const taskSummary = computed(() => {
  return tasks.value.reduce(
    (summary, task) => {
      summary[task.status] += 1
      return summary
    },
    {
      todo: 0,
      doing: 0,
      done: 0,
    }
  )
})

const subpages = [
  {
    id: 'sub-1',
    icon: '📊',
    title: 'Market Research',
    updatedDate: 'yesterday',
  },
  {
    id: 'sub-2',
    icon: '👥',
    title: 'Resource Allocation',
    updatedDate: '3 days ago',
  },
]

function scrollToTasks() {
  taskSectionRef.value?.scrollIntoView({
    behavior: 'smooth',
    block: 'start',
  })
}

function openTaskDetail(task) {
  selectedTask.value = task
  isTaskDetailOpen.value = true
}

function closeTaskDetail() {
  isTaskDetailOpen.value = false
}

function addTaskComment(content) {
  if (!selectedTask.value || !content.trim()) return

  const taskId = selectedTask.value.id

  const newComment = {
    id: `comment-${Date.now()}`,
    author: {
      name: 'Bạn',
      role: 'You',
      avatarUrl: selectedTask.value.assignee.avatarUrl,
    },
    content: content.trim(),
    createdAt: 'Just now',
    likes: 0,
    replies: [],
  }

  taskCommentsByTaskId.value = {
    ...taskCommentsByTaskId.value,
    [taskId]: [
      ...(taskCommentsByTaskId.value[taskId] || []),
      newComment,
    ],
  }
}

function statusLabel(status) {
  return {
    todo: 'To Do',
    doing: 'Doing',
    done: 'Done',
  }[status] ?? status
}

function statusClass(status) {
  return {
    todo: 'status-todo',
    doing: 'status-doing',
    done: 'status-done',
  }[status] ?? 'status-todo'
}

function priorityLabel(priority) {
  return {
    low: 'Low',
    medium: 'Med',
    high: 'High',
  }[priority] ?? priority
}

function priorityClass(priority) {
  return {
    low: 'priority-low',
    medium: 'priority-medium',
    high: 'priority-high',
  }[priority] ?? 'priority-low'
}
</script>

<style scoped>
@import "@/views/css/AppLayout.css";
</style>