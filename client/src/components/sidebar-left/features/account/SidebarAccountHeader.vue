<template>
  <header class="lunar-header">
    <div class="lunar-account-wrap">
      <button
        type="button"
        class="lunar-account-card"
        title="Tài khoản"
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
          {{ roleLabel }}
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

          <div class="lunar-menu-section-label">Tài khoản</div>

          <button
            type="button"
            class="lunar-menu-row"
            @click="handleSettings"
          >
            <span class="lunar-menu-icon">
              <i class="bi bi-sliders2"></i>
            </span>

            <span class="lunar-menu-content">
              <strong>Cài đặt</strong>
              <small>Hồ sơ, ưu tiên AI, bảo mật</small>
            </span>
          </button>

          <div class="lunar-menu-divider"></div>

          <button
            type="button"
            class="lunar-menu-row danger"
            @click="handleLogout"
          >
            <span class="lunar-menu-icon">
              <i class="bi bi-box-arrow-right"></i>
            </span>

            <span class="lunar-menu-content">
              <strong>Đăng xuất</strong>
              <small>Đăng xuất khỏi phiên hiện tại</small>
            </span>
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

const roleLabel = computed(() => {
  const role = props.workspaceRole?.trim().toLowerCase()

  if (role === 'owner') return 'Chủ sở hữu'
  if (role === 'manager') return 'Quản lý'
  if (role === 'viewer') return 'Người xem'

  return 'Thành viên'
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

function handleLogout() {
  closeMenu()
  emit('logout')
}
</script>
