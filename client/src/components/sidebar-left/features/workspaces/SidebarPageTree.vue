<template>
  <ul class="lunar-page-tree">
    <li
      v-for="page in pages"
      :key="page.id"
      class="lunar-page-node"
    >
      <div
        class="lunar-page-row"
        :class="{ active: page.id === selectedPageId }"
        :style="{ paddingLeft: `${Math.min(depth, maxVisualDepth) * 14}px` }"
        @click.stop="emit('selectPage', page)"
      >
        <button
          v-if="page.children.length > 0"
          type="button"
          class="lunar-page-toggle"
          :class="{ open: openedPageIds.has(page.id) }"
          @click.stop="emit('togglePage', page)"
        >
          <i class="bi bi-caret-right-fill"></i>
        </button>

        <span
          v-else
          class="lunar-page-toggle-spacer"
        ></span>

        <span class="lunar-page-icon">
          {{ page.icon || '◇' }}
        </span>

        <span class="lunar-page-title">
          {{ page.title }}
        </span>

        <button
          type="button"
          class="lunar-page-action"
          title="Tạo subpage"
          @click.stop="emit('createChild', page)"
        >
          <i class="bi bi-plus-lg"></i>
        </button>
      </div>

      <SidebarPageTree
        v-if="page.children.length > 0 && openedPageIds.has(page.id)"
        class="lunar-page-children"
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
import type { PageTreeItem } from '@/components/sidebar-left/types/sidebar.types'

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
