<template>
  <div
    class="lunar-rail"
    role="navigation"
    aria-label="Sidebar thu gọn"
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
      title="Không gian"
      aria-label="Mở sidebar không gian"
      @click.stop="emit('expand')"
    >
      <i class="bi bi-folder2-open"></i>
    </button>

    <button
      type="button"
      class="lunar-rail-btn"
      title="Việc của tôi"
      aria-label="Mở việc của tôi"
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
      title="Gợi ý AI"
      aria-label="Mở gợi ý AI"
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
      title="Cài đặt"
      aria-label="Mở cài đặt"
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
