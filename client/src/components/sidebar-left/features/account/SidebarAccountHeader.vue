<template>
  <header class="lunar-header">
    <div class="lunar-account-wrap">
      <button
        type="button"
        class="lunar-account-card"
        title="Account"
        @click.stop="isMenuOpen = !isMenuOpen"
      >
        <span class="lunar-account-avatar">
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
        </span>

        <span class="lunar-account-meta">
          <span class="lunar-account-name">
            {{ isLoading ? 'Đang tải...' : displayName }}
          </span>

          <span class="lunar-account-subtitle">
            {{ subtitle }}
          </span>
        </span>

        <span class="lunar-account-badge">
          {{ workspaceRole || 'member' }}
        </span>

        <i
          class="bi bi-chevron-down lunar-account-chevron"
          :class="{ open: isMenuOpen }"
        ></i>
      </button>

      <Transition name="lunar-pop">
        <div
          v-if="isMenuOpen"
          class="lunar-account-menu"
          @click.stop
        >
          <div class="lunar-account-menu-head">
            <span class="lunar-account-menu-avatar">
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
            </span>

            <div class="lunar-account-menu-meta">
              <strong>{{ displayName }}</strong>
              <span>{{ subtitle }}</span>
            </div>
          </div>

          <button
            type="button"
            class="lunar-menu-row"
            @click="handleSettings"
          >
            <i class="bi bi-person-gear"></i>
            <span>Profile settings</span>
          </button>

          <button
            type="button"
            class="lunar-menu-row"
            @click="handleMyTasks"
          >
            <i class="bi bi-check2-square"></i>
            <span>My Tasks</span>
          </button>

          <button
            type="button"
            class="lunar-menu-row danger"
            @click="handleLogout"
          >
            <i class="bi bi-box-arrow-right"></i>
            <span>Logout</span>
          </button>
        </div>
      </Transition>
    </div>

    <button
      type="button"
      class="lunar-icon-btn"
      title="Thu gọn sidebar"
      @click.stop="emit('collapse')"
    >
      <i class="bi bi-layout-sidebar-inset"></i>
    </button>
  </header>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'

const props = defineProps<{
  displayName: string
  subtitle: string
  avatarUrl: string | null
  initial: string
  workspaceRole: string | null
  isLoading: boolean
}>()

const emit = defineEmits<{
  collapse: []
  openSettings: []
  openMyTasks: []
  logout: []
}>()

const isMenuOpen = ref(false)
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

function closeMenu() {
  isMenuOpen.value = false
}

function handleSettings() {
  closeMenu()
  emit('openSettings')
}

function handleMyTasks() {
  closeMenu()
  emit('openMyTasks')
}

function handleLogout() {
  closeMenu()
  emit('logout')
}
</script>