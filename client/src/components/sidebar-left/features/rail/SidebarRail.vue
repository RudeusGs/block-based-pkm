<template>
  <div class="lunar-rail">
    <button
      type="button"
      class="lunar-rail-avatar"
      title="Mở sidebar"
      @click.stop="emit('expand')"
    >
      <img
        v-if="avatarUrl"
        :src="avatarUrl"
        :alt="displayName"
      />

      <span v-else>
        {{ initial }}
      </span>
    </button>

    <button
      type="button"
      class="lunar-rail-btn"
      title="Workspaces"
      @click.stop="emit('expand')"
    >
      <i class="bi bi-folder2-open"></i>
    </button>

    <button
      type="button"
      class="lunar-rail-btn"
      title="Updates"
      @click.stop="emit('openUpdates')"
    >
      <i class="bi bi-clock-history"></i>

      <span
        v-if="unreadCount > 0"
        class="lunar-rail-dot"
      ></span>
    </button>

    <button
      type="button"
      class="lunar-rail-btn"
      title="My Tasks"
      @click.stop="emit('openMyTasks')"
    >
      <i class="bi bi-check2-square"></i>

      <span
        v-if="openTaskCount > 0"
        class="lunar-rail-dot"
      ></span>
    </button>

    <button
      type="button"
      class="lunar-rail-btn mt-auto"
      title="Settings"
      @click.stop="emit('openSettings')"
    >
      <i class="bi bi-sliders2"></i>
    </button>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  displayName: string
  avatarUrl: string | null
  initial: string
  unreadCount: number
  openTaskCount: number
}>()

const emit = defineEmits<{
  expand: []
  openUpdates: []
  openMyTasks: []
  openSettings: []
}>()
</script>