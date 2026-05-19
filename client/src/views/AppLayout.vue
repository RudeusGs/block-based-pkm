<template>
  <div class="workspace-shell min-vh-100 d-flex text-on-surface">
    <SidebarLeft />

    <section class="workspace-page-center flex-grow-1 min-vh-100 text-on-surface">
      <header class="top-app-bar sticky-top d-flex align-items-center justify-content-between px-3 border-bottom border-soft">
        <nav class="breadcrumb-nav d-flex align-items-center gap-1 small fw-medium text-on-surface-variant">
          <button class="breadcrumb-pill btn btn-sm border-0 px-2 py-1">Workspace</button>
          <span class="text-outline">/</span>
          <button class="breadcrumb-pill btn btn-sm border-0 px-2 py-1">Projects</button>
          <span class="text-outline">/</span>
          <span class="text-on-surface fw-semibold px-2 py-1">Q4 Roadmap</span>
        </nav>

        <div class="d-flex align-items-center gap-1">
          <button
            class="task-jump-btn btn btn-sm border-0 d-inline-flex align-items-center gap-1"
            title="Jump to Work Tasks"
            @click="scrollToTasks"
          >
            <span class="material-symbols-outlined">table_chart</span>
            Tasks
          </button>

          <button class="top-icon-btn btn border-0" title="Share">
            <span class="material-symbols-outlined">share</span>
          </button>

          <button class="top-icon-btn btn border-0" title="Star">
            <span class="material-symbols-outlined">star</span>
          </button>

          <button class="top-icon-btn btn border-0" title="History">
            <span class="material-symbols-outlined">history</span>
          </button>

          <button class="top-icon-btn btn border-0" title="More">
            <span class="material-symbols-outlined">more_vert</span>
          </button>
        </div>
      </header>

      <main class="page-scroll pb-5">
        <div class="page-container container position-relative px-4 pt-5 pb-5">
          <div class="format-toolbar position-absolute d-none d-md-flex align-items-center gap-1 p-1 rounded-3 shadow-lg">
            <button class="toolbar-btn btn border-0 p-1" title="Bold">
              <span class="material-symbols-outlined">format_bold</span>
            </button>

            <button class="toolbar-btn btn border-0 p-1" title="Italic">
              <span class="material-symbols-outlined">format_italic</span>
            </button>

            <button class="toolbar-btn btn border-0 p-1" title="Underline">
              <span class="material-symbols-outlined">format_underlined</span>
            </button>

            <span class="toolbar-divider"></span>

            <button class="toolbar-btn btn border-0 p-1" title="Code">
              <span class="material-symbols-outlined">code</span>
            </button>

            <button class="toolbar-btn btn border-0 p-1" title="Link">
              <span class="material-symbols-outlined">link</span>
            </button>

            <span class="toolbar-divider"></span>

            <button class="toolbar-btn btn border-0 p-1" title="More">
              <span class="material-symbols-outlined">more_horiz</span>
            </button>
          </div>

          <section class="page-header mb-5">
            <div class="cover-wrap position-relative rounded-4 overflow-hidden mb-4">
              <img
                class="cover-image w-100 h-100 object-fit-cover"
                src="https://lh3.googleusercontent.com/aida-public/AB6AXuBNrH2IqDS1nw-O5efFDqmWmrlhvlcn46tJr6h0gIkgw2tkipH-LW2Qz2l7ymULHHV57PnluCGimtxrIupw7-0R_4ZZklC3s_ux5thQFjKdZpyc94GJFRXh7gsjwEFUs88KDXTLmZQGLjZhoxL5KoStiewu1DGGOTrjBg7h1eAM3h-Pzqri-5QiW5lbxh032FaBpjwHG5Xgg-kln_DuMI6gbuqlhuM4JnaBA6KNJoHAwP-iH4ahmbcjwa7U3_nxUiQqi8tNc2Bk7eY"
                alt="Abstract dark purple workspace cover"
              />

              <button class="change-cover-btn btn btn-sm position-absolute d-flex align-items-center gap-2">
                <span class="material-symbols-outlined">image</span>
                Change cover
              </button>
            </div>

            <div class="page-emoji-wrap position-relative d-inline-flex align-items-center justify-content-center mb-3">
              <span class="page-emoji">🚀</span>

              <button class="emoji-overlay btn border-0 p-0 position-absolute rounded-circle" title="Change icon">
                <span class="material-symbols-outlined">emoji_emotions</span>
              </button>
            </div>

            <input
              v-model="page.title"
              class="page-title-input form-control bg-transparent border-0 shadow-none px-0 py-0 mb-3"
              placeholder="Untitled"
            />

            <div class="page-meta d-flex flex-wrap align-items-center gap-3 small fw-medium text-on-surface-variant">
              <span class="d-inline-flex align-items-center gap-2">
                <span class="material-symbols-outlined meta-icon">schedule</span>
                Edited {{ page.updatedDate }}
              </span>

              <span class="d-inline-flex align-items-center gap-2">
                <span class="material-symbols-outlined meta-icon">sell</span>
                v{{ page.currentRevision }}
              </span>

              <span class="status-chip d-inline-flex align-items-center gap-2 px-2 py-1 rounded-2 border">
                <span class="status-dot"></span>
                Planning
              </span>

              <span class="avatar-stack d-inline-flex align-items-center">
                <img
                  v-for="member in collaborators"
                  :key="member.id"
                  class="avatar rounded-circle"
                  :src="member.avatarUrl"
                  :alt="member.name"
                />
              </span>
            </div>
          </section>

          <section class="block-editor mb-5" @click.self="focusEmptyBlock">
            <article class="editor-block block-hover position-relative row gx-0 align-items-start py-1">
              <BlockHandles />

              <div class="col block-content">
                <h2 class="editor-heading outline-none mb-2 mt-4" contenteditable="true">
                  Strategic Objectives
                </h2>
              </div>
            </article>

            <article class="editor-block block-hover position-relative row gx-0 align-items-start py-1">
              <BlockHandles />

              <div class="col block-content">
                <p class="editor-paragraph text-on-surface-variant mb-0 outline-none" contenteditable="true">
                  This quarter focuses on scaling our core infrastructure while expanding market reach. The primary goal is to achieve
                  <span class="highlight-text px-1 rounded fw-semibold">seamless integration</span>
                  across all major platforms.
                </p>
              </div>
            </article>

            <article class="editor-block block-hover position-relative row gx-0 align-items-start py-2 my-3">
              <BlockHandles align="top-lg" />

              <div class="col block-content">
                <div class="callout-block d-flex gap-3 p-3 rounded-3 border">
                  <span class="callout-emoji">💡</span>

                  <div class="editor-paragraph mb-0 outline-none" contenteditable="true">
                    <strong>Key Insight:</strong> Q3 metrics indicate a strong preference for API-first solutions among enterprise clients. We must prioritize documentation.
                  </div>
                </div>
              </div>
            </article>

            <article class="editor-block block-hover position-relative row gx-0 align-items-start py-1">
              <BlockHandles />

              <div class="col block-content">
                <label class="todo-line d-flex align-items-start gap-3 mb-0">
                  <span class="todo-box todo-box-checked d-inline-flex align-items-center justify-content-center rounded-1">
                    <span class="material-symbols-outlined">check</span>
                  </span>

                  <span class="text-on-surface-variant text-decoration-line-through outline-none flex-grow-1" contenteditable="true">
                    Finalize Q3 post-mortem analysis
                  </span>
                </label>
              </div>
            </article>

            <article class="editor-block block-hover position-relative row gx-0 align-items-start py-1">
              <BlockHandles />

              <div class="col block-content">
                <label class="todo-line d-flex align-items-start gap-3 mb-0">
                  <span class="todo-box d-inline-flex align-items-center justify-content-center rounded-1"></span>

                  <span class="outline-none flex-grow-1" contenteditable="true">
                    Draft engineering OKRs
                  </span>
                </label>
              </div>
            </article>

            <article class="editor-block block-hover position-relative row gx-0 align-items-start py-1 mt-3">
              <BlockHandles />

              <div class="col block-content">
                <ul class="editor-list text-on-surface-variant mb-0 outline-none" contenteditable="true">
                  <li>Phase 1: Research and Prototyping</li>
                  <li>Phase 2: Alpha Release (Internal)</li>
                </ul>
              </div>
            </article>

            <article class="editor-block block-hover position-relative row gx-0 align-items-start py-2 mt-3">
              <BlockHandles align="top-lg" />

              <div class="col block-content">
                <div class="code-card rounded-3 overflow-hidden border">
                  <div class="code-header d-flex align-items-center justify-content-between px-3 py-2 small fw-medium border-bottom">
                    <span>JSON</span>

                    <button class="copy-btn btn btn-sm border-0 d-flex align-items-center gap-1 p-0">
                      <span class="material-symbols-outlined">content_copy</span>
                      Copy
                    </button>
                  </div>

                  <pre class="code-body mb-0 p-3 outline-none" contenteditable="true">{
  "milestone": "Q4_Alpha",
  "features": [
    "dark_mode_editor",
    "block_drag_drop"
  ],
  "status": "in_progress"
}</pre>
                </div>
              </div>
            </article>

            <article class="editor-block block-hover position-relative row gx-0 align-items-start py-1 mt-2">
              <BlockHandles />

              <div class="col block-content">
                <p ref="emptyBlockRef" class="empty-block mb-0 outline-none fst-italic" contenteditable="true">
                  Type '/' for commands
                </p>
              </div>
            </article>
          </section>

          <section ref="taskSectionRef" class="work-tasks-section mt-5 mb-5">
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
                3 To Do
              </span>

              <span class="d-inline-flex align-items-center gap-2">
                <span class="summary-dot summary-doing"></span>
                2 Doing
              </span>

              <span class="d-inline-flex align-items-center gap-2">
                <span class="summary-dot summary-done"></span>
                3 Done
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
                        'task-row-selected': selectedTask?.id === task.id
                      }"
                      tabindex="0"
                      @click="openTaskDetail(task)"
                      @keydown.enter="openTaskDetail(task)"
                    >
                      <td class="ps-4 task-name-cell">
                        <div class="d-flex align-items-center gap-2 fw-semibold text-on-surface">
                          <span class="material-symbols-outlined drag-row-icon">drag_indicator</span>

                          <span :class="{ 'text-decoration-line-through': task.status === 'done' }">
                            {{ task.title }}
                          </span>
                        </div>

                        <div class="task-description small mt-1 ms-4 text-truncate">
                          {{ task.description }}
                        </div>
                      </td>

                      <td>
                        <span class="status-pill d-inline-flex align-items-center gap-2 rounded-pill px-2 py-1" :class="statusClass(task.status)">
                          <span class="status-pill-dot"></span>
                          {{ statusLabel(task.status) }}
                        </span>
                      </td>

                      <td>
                        <span class="priority-pill d-inline-block rounded-2 px-2 py-1" :class="priorityClass(task.priority)">
                          {{ priorityLabel(task.priority) }}
                        </span>
                      </td>

                      <td>
                        <span class="task-date small" :class="{ 'date-overdue': task.overdue }">
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
import { computed, defineComponent, h, ref } from 'vue'
import SidebarLeft from '@/components/sidebar-left/SidebarLeft.vue'
import TaskDetailDrawer from '@/components/task/TaskDetailDrawer.vue'

const emptyBlockRef = ref(null)
const taskSectionRef = ref(null)

const selectedTask = ref(null)
const isTaskDetailOpen = ref(false)

const page = ref({
  id: 'page-q4-roadmap',
  title: 'Q4 Strategic Roadmap',
  icon: '🚀',
  coverImage: null,
  currentRevision: '2.4.0',
  updatedDate: '2h ago'
})

const collaborators = [
  {
    id: 'u-1',
    name: 'Linh Tran',
    avatarUrl: 'https://lh3.googleusercontent.com/aida-public/AB6AXuCHtRoLysefXS_2BUp7YTnG_8qy0NC1ND6la_ZWcE_wiNzjze72VGN5BMrffpUapxHsWlWBKWF9xMkfowINkSHls55knzGFi_UVumJxJqnR9EwNj9zMN0rrw5c10hZwn9pfRQwcksYFO9uKsnEoBWKNhm4nVuEvwBCCccIyC-dpP9xMrMmAXRTw1kbvHfJe_vCtAVf65FiovwnO65mbr2-9_VxMRW34F_bcuHLilcna5-dZXXs5S9oJW7kHKdVcD93dxP5iiyn4T9k'
  },
  {
    id: 'u-2',
    name: 'Minh Pham',
    avatarUrl: 'https://lh3.googleusercontent.com/aida-public/AB6AXuDusCM62Zdd2CG4O62O1nPKPj7vxv-Yajz0_nBAKjvlZ0s5falAkkCjTxt1wKdaNLMZkyig2mEUMj-uZREmyfVzR9xs8zAnweNU9Z5GfI95GOJ6559x7wkTellI0lYVdhLsHo0XVQrLOdbqwjpF9HAErxA3TM_jRHvPIDp57xyM6VaAW4QkPhjF23jOV7FIFcgMgNFX59HfqC5Uobpf2lEZ7g5NzZcw3zm2ToZDDaclYxmRRjWty7fkPJmCGcL63WyxnpsOY89s'
  }
]

const tasks = [
  {
    id: 'task-1',
    title: 'Design System Audit',
    description: 'Review all color tokens',
    status: 'doing',
    priority: 'high',
    dueDate: 'Oct 15',
    overdue: false,
    assignee: {
      name: 'Nhi',
      avatarUrl: 'https://lh3.googleusercontent.com/aida-public/AB6AXuBPT1P6EolY0PCGDr7ySNB5Ea1K_WfIQ1e8qOSoq0OIQuBClgCzQZCvBeaozHkqUDhm_F6GB2GGyc8q5Qy1Xh4J63aIToilbMSgiG9faTvoLqIJLT3nN1KM30LzjfvD6zfdM4mF1nsaFeeta6FDKED5hmlBcX9EpstFOX7VGMpMt1QqDeWJjA6JZQ-HFQ0Oeq9fDT6UBQJQW-445PIYyO4G3Cznf9vfQBVO_DdPR3FM6cpgCowBdy0HtmIsXR3BmOjPGUTP6-bmx2E'
    }
  },
  {
    id: 'task-2',
    title: 'API Documentation',
    description: 'Draft v2 endpoints',
    status: 'todo',
    priority: 'medium',
    dueDate: 'Oct 22',
    overdue: false,
    assignee: {
      name: 'Khoa',
      avatarUrl: 'https://lh3.googleusercontent.com/aida-public/AB6AXuBtq5DUy_6uthCPqCF1e9WfQKrn2qNFfHH5dodKYK8-b81ibkIMFx-vEOttTuhK0DHhvVLOLgERmBsnjR1dH-2wX3WZi1YCdLh0sM8EWj0ahoPNFc8Ck-jR1k1xIo6s3SL1bE3f-4tWUqVhweQneE7ULt-1BZneYEYshxHe77E8PAokD_X1GXBvledcd3Qi5FviSKwNwnolr1zVxoipSIhJBR_MCsbKjYRKI7gHdkURD2geRicNFA3sGBUZJ4XtroAwim5lznJq2mY'
    }
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
      avatarUrl: 'https://lh3.googleusercontent.com/aida-public/AB6AXuDBNRnxIDiwQH219nVSLWBc_3lOAyOd5oAdiNjBBYVWYYNasbDV-CQYWP_By9lWybgPVjRdLRaIJ9ElUjrNgEK8BOGjyhQ-zYHEz8Px9hdcJC-C5qxGCPHeQ-kzUq-iasH1X79-AHGD6uOz6FwIfxXLCtHXISZY3IdbMMl5z9z-nk8l_RH601_7CCngSFL1KjOR5QxkeuzYlN6yOgSlP9qgcakxTplOP-IhrCS_4IQR7_dvy2ZbPeZReLQN6Fn7A4ViWZXgAqaCkfI'
    }
  }
]

const taskCommentsByTaskId = ref({
  'task-1': [
    {
      id: 'comment-1',
      author: {
        name: 'Linh Tran',
        role: 'Product',
        avatarUrl: collaborators[0].avatarUrl
      },
      content: 'Mình nghĩ nên check thêm spacing token và trạng thái hover/focus để đồng bộ với sidebar mới.',
      createdAt: '12m ago',
      likes: 3,
      replies: [
        {
          id: 'reply-1',
          author: {
            name: 'Nhi',
            role: 'Assignee',
            avatarUrl: tasks[0].assignee.avatarUrl
          },
          content: 'Đúng rồi, mình sẽ thêm checklist riêng cho interaction state.'
        }
      ]
    },
    {
      id: 'comment-2',
      author: {
        name: 'Minh Pham',
        role: 'Engineer',
        avatarUrl: collaborators[1].avatarUrl
      },
      content: 'Nếu cần mình có thể map lại token từ CSS hiện tại sang format design system.',
      createdAt: '5m ago',
      likes: 1,
      replies: []
    }
  ],
  'task-2': [
    {
      id: 'comment-3',
      author: {
        name: 'Minh Pham',
        role: 'Engineer',
        avatarUrl: collaborators[1].avatarUrl
      },
      content: 'Phần endpoint v2 nên tách riêng auth, task, recommendation để dễ review.',
      createdAt: '20m ago',
      likes: 2,
      replies: []
    }
  ],
  'task-3': [
    {
      id: 'comment-4',
      author: {
        name: 'Linh Tran',
        role: 'Product',
        avatarUrl: collaborators[0].avatarUrl
      },
      content: 'Meeting này đã hoàn tất, chỉ còn cần tổng hợp lại owner cho từng hạng mục.',
      createdAt: '1d ago',
      likes: 4,
      replies: []
    }
  ]
})

const selectedTaskComments = computed(() => {
  if (!selectedTask.value) return []

  return taskCommentsByTaskId.value[selectedTask.value.id] || []
})

const subpages = [
  { id: 'sub-1', icon: '📊', title: 'Market Research', updatedDate: 'yesterday' },
  { id: 'sub-2', icon: '👥', title: 'Resource Allocation', updatedDate: '3 days ago' }
]

const BlockHandles = defineComponent({
  name: 'BlockHandles',
  props: {
    align: {
      type: String,
      default: 'default'
    }
  },
  setup(props) {
    return () => h('div', {
      class: [
        'block-handles col-auto d-flex align-items-center gap-1 pe-2',
        props.align === 'top-lg' ? 'block-handles-top-lg' : ''
      ]
    }, [
      h('button', { class: 'handle-btn btn border-0 p-0', title: 'Add block' }, [
        h('span', { class: 'material-symbols-outlined' }, 'add')
      ]),
      h('button', { class: 'handle-btn btn border-0 p-0 cursor-grab', title: 'Drag block' }, [
        h('span', { class: 'material-symbols-outlined' }, 'drag_indicator')
      ])
    ])
  }
})

function focusEmptyBlock() {
  emptyBlockRef.value?.focus()
}

function scrollToTasks() {
  taskSectionRef.value?.scrollIntoView({
    behavior: 'smooth',
    block: 'start'
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
      name: 'Alex Rivera',
      role: 'You',
      avatarUrl: selectedTask.value.assignee.avatarUrl
    },
    content: content.trim(),
    createdAt: 'Just now',
    likes: 0,
    replies: []
  }

  taskCommentsByTaskId.value = {
    ...taskCommentsByTaskId.value,
    [taskId]: [
      ...(taskCommentsByTaskId.value[taskId] || []),
      newComment
    ]
  }
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
</script>

<style scoped>
@import "@/views/css/AppLayout.css";
</style>