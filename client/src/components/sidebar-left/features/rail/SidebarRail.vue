<template>
  <div
    class="lunar-rail"
    role="navigation"
    aria-label="Collapsed sidebar"
    @click.stop="emit('expand')"
  >
    <button
      type="button"
      class="lunar-rail-avatar"
      title="Mở sidebar"
      aria-label="Mở sidebar"
      @click.stop="emit('expand')"
    >
      <img
        v-if="canShowAvatar"
        :key="avatarSrc"
        :src="avatarSrc"
        :alt="displayName"
        referrerpolicy="no-referrer"
        @error="avatarLoadFailed = true"
      />

      <span v-else>
        {{ initial }}
      </span>
    </button>

    <button
      type="button"
      class="lunar-rail-btn"
      title="Workspaces"
      aria-label="Mở sidebar workspace"
      @click.stop="emit('expand')"
    >
      <i class="bi bi-folder2-open"></i>
    </button>

    <button
      type="button"
      class="lunar-rail-btn"
      title="My Tasks"
      aria-label="Mở My Tasks"
      @click.stop="emit('open-my-tasks')"
    >
      <i class="bi bi-check2-square"></i>

      <span
        v-if="openTaskCount > 0"
        class="lunar-rail-dot"
      ></span>
    </button>

    <button
      type="button"
      class="lunar-rail-btn"
      title="AI Suggestions"
      aria-label="Mở AI Suggestions"
      @click.stop="emit('open-recommendations')"
    >
      <i class="bi bi-stars"></i>

      <span
        v-if="recommendationCount > 0"
        class="lunar-rail-dot"
      ></span>
    </button>

    <button
      type="button"
      class="lunar-rail-btn mt-auto"
      title="Settings"
      aria-label="Mở Settings"
      @click.stop="emit('open-settings')"
    >
      <i class="bi bi-sliders2"></i>
    </button>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'

const props = defineProps<{
  displayName: string
  avatarUrl: string | null
  initial: string
  openTaskCount: number
  recommendationCount: number
}>()

const emit = defineEmits<{
  expand: []
  'open-my-tasks': []
  'open-recommendations': []
  'open-settings': []
}>()

const avatarLoadFailed = ref(false)

const avatarSrc = computed(() => {
  return props.avatarUrl?.trim() || undefined
})

const canShowAvatar = computed(() => {
  return Boolean(avatarSrc.value) && !avatarLoadFailed.value
})

watch(
  () => props.avatarUrl,
  () => {
    avatarLoadFailed.value = false
  }
)
</script>