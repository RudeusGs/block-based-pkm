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
      >
        <button
          type="button"
          class="sidebar-page-link"
          @click.stop="$emit('selectPage', page)"
        >
          <span class="sidebar-page-icon">
            {{ page.icon || '📄' }}
          </span>

          <span class="sidebar-page-title text-truncate">
            {{ page.title }}
          </span>
        </button>

        <button
          type="button"
          class="btn-add-child sidebar-page-add"
          title="Tạo subpage"
          @click.stop="$emit('createChild', page)"
        >
          <span class="material-symbols-outlined">add</span>
        </button>
      </div>

      <SidebarPageTree
        v-if="page.children.length"
        :pages="page.children"
        :selected-page-id="selectedPageId"
        @select-page="$emit('selectPage', $event)"
        @create-child="$emit('createChild', $event)"
      />
    </li>
  </ul>
</template>

<script setup lang="ts">
import type { PageTreeItem } from '@/components/composables/useSidebarLeft'

defineOptions({
  name: 'SidebarPageTree',
})

defineProps<{
  pages: PageTreeItem[]
  selectedPageId: string | null
}>()

defineEmits<{
  selectPage: [page: PageTreeItem]
  createChild: [page: PageTreeItem]
}>()
</script>

<style scoped src="./css/SidebarPageTree.css"></style>