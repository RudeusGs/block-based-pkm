<template>
  <ul class="sidebar-page-tree">
    <li
      v-for="page in pages"
      :key="page.id"
      class="sidebar-page-node"
    >
      <div
        class="sidebar-page-row"
        :class="{ 'sidebar-page-row-active': page.id === selectedPageId }"
        :style="{ paddingLeft: `calc(4px + ${Math.min(depth, maxVisualDepth)} * 12px)` }"
        @click.stop="emit('selectPage', page)"
      >
        <button
          v-if="page.children.length > 0"
          type="button"
          class="sidebar-page-arrow"
          :class="{ 'sidebar-page-arrow-open': openedPageIds.has(page.id) }"
          @click.stop="emit('togglePage', page)"
        >
          <span class="material-symbols-outlined">arrow_right</span>
        </button>
        <div v-else class="sidebar-page-arrow-spacer"></div>

        <div class="sidebar-page-main">
          <span class="sidebar-page-icon">
            {{ page.icon || '📄' }}
          </span>

          <span class="sidebar-page-title">
            {{ page.title }}
          </span>
        </div>

        <button
          type="button"
          class="sidebar-page-plus-button"
          title="Tạo subpage"
          @click.stop="emit('createChild', page)"
        >
          <span class="material-symbols-outlined">add</span>
        </button>
      </div>

      <SidebarPageTree
        v-if="page.children.length > 0 && openedPageIds.has(page.id)"
        class="sidebar-page-children"
        :pages="page.children"
        :selected-page-id="selectedPageId"
        :opened-page-ids="openedPageIds"
        :depth="depth + 1"
        :max-visual-depth="maxVisualDepth"
        @select-page="emit('selectPage', $event)"
        @create-child="emit('createChild', $event)"
        @toggle-page="emit('togglePage', $event)"
      />
    </li>
  </ul>
</template>

<script setup lang="ts">
import type { PageTreeItem } from '@/components/types/sidebar.types'

defineOptions({
  name: 'SidebarPageTree',
})

withDefaults(
  defineProps<{
    pages: PageTreeItem[]
    selectedPageId: string | null
    openedPageIds: Set<string>
    depth?: number
    maxVisualDepth?: number
  }>(),
  {
    depth: 0,
    maxVisualDepth: 4,
  }
)

const emit = defineEmits<{
  selectPage: [page: PageTreeItem]
  createChild: [page: PageTreeItem]
  togglePage: [page: PageTreeItem]
}>()
</script>

<style scoped src="./css/SidebarPageTree.css"></style>