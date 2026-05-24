<template>
  <div
    v-if="hasAnyAction"
    ref="menuRef"
    class="lunar-page-actions"
    :class="{ 'is-open': isOpen }"
    @click.stop
  >
    <button
      type="button"
      class="lunar-page-menu-trigger"
      title="Tùy chọn trang"
      aria-label="Tùy chọn trang"
      :aria-expanded="isOpen"
      @click.stop="toggleMenu"
    >
      <i class="bi bi-three-dots"></i>
    </button>

    <Transition name="lunar-page-menu-pop">
      <section
        v-if="isOpen"
        class="lunar-page-menu"
        role="menu"
        aria-label="Thao tác trang"
      >
        <button
          v-if="canSettings"
          type="button"
          class="lunar-page-menu-item"
          role="menuitem"
          @click="selectAction('settings')"
        >
          <i class="bi bi-gear"></i>

          <span>
            <strong>Cài đặt trang</strong>
            <small>Tên, icon và thuộc tính</small>
          </span>
        </button>

        <button
          v-if="canShare"
          type="button"
          class="lunar-page-menu-item"
          role="menuitem"
          @click="selectAction('share')"
        >
          <i class="bi bi-share"></i>

          <span>
            <strong>Chia sẻ</strong>
            <small>Sao chép liên kết trang</small>
          </span>
        </button>

        <button
          v-if="canFavorite"
          type="button"
          class="lunar-page-menu-item"
          role="menuitem"
          @click="selectAction('favorite')"
        >
          <i class="bi" :class="isFavorite ? 'bi-star-fill' : 'bi-star'"></i>

          <span>
            <strong>{{ isFavorite ? 'Bỏ yêu thích' : 'Thêm vào yêu thích' }}</strong>
            <small>Ghim trang vào danh sách nhanh</small>
          </span>
        </button>

        <button
          v-if="canDuplicate"
          type="button"
          class="lunar-page-menu-item"
          role="menuitem"
          @click="selectAction('duplicate')"
        >
          <i class="bi bi-copy"></i>

          <span>
            <strong>Nhân bản</strong>
            <small>Tạo bản sao trang và khối</small>
          </span>
        </button>

        <div
          v-if="canDelete && (canSettings || canShare || canFavorite || canDuplicate)"
          class="lunar-page-menu-separator"
        ></div>

        <button
          v-if="canDelete"
          type="button"
          class="lunar-page-menu-item danger"
          role="menuitem"
          @click="selectAction('delete')"
        >
          <i class="bi bi-trash3"></i>

          <span>
            <strong>Xóa trang</strong>
            <small>Xóa trang khỏi không gian</small>
          </span>
        </button>
      </section>
    </Transition>
  </div>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import type { PageTreeItem } from '@/components/sidebar-left/types/sidebar.types'

const props = withDefaults(
  defineProps<{
    page: PageTreeItem
    canSettings?: boolean
    canShare?: boolean
    canDelete?: boolean
    canDuplicate?: boolean
    canFavorite?: boolean
    isFavorite?: boolean
  }>(),
  {
    canSettings: true,
    canShare: true,
    canDelete: true,
    canDuplicate: true,
    canFavorite: true,
    isFavorite: false,
  }
)

const emit = defineEmits<{
  settings: [page: PageTreeItem]
  share: [page: PageTreeItem]
  delete: [page: PageTreeItem]
  duplicate: [page: PageTreeItem]
  favorite: [page: PageTreeItem]
}>()

const menuRef = ref<HTMLElement | null>(null)
const isOpen = ref(false)

const hasAnyAction = computed(() => {
  return (
    props.canSettings ||
    props.canShare ||
    props.canDelete ||
    props.canDuplicate ||
    props.canFavorite
  )
})

function toggleMenu() {
  isOpen.value = !isOpen.value
}

function closeMenu() {
  isOpen.value = false
}

function selectAction(action: 'settings' | 'share' | 'delete' | 'duplicate' | 'favorite') {
  closeMenu()

  if (action === 'settings' && props.canSettings) {
    emit('settings', props.page)
    return
  }

  if (action === 'share' && props.canShare) {
    emit('share', props.page)
    return
  }

  if (action === 'favorite' && props.canFavorite) {
    emit('favorite', props.page)
    return
  }

  if (action === 'duplicate' && props.canDuplicate) {
    emit('duplicate', props.page)
    return
  }

  if (action === 'delete' && props.canDelete) {
    emit('delete', props.page)
  }
}

function handleDocumentClick(event: MouseEvent) {
  const target = event.target as Node | null

  if (target && menuRef.value?.contains(target)) {
    return
  }

  closeMenu()
}

function handleKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape') {
    closeMenu()
  }
}

watch(isOpen, (open) => {
  if (open) {
    document.addEventListener('click', handleDocumentClick)
    window.addEventListener('keydown', handleKeydown)
    return
  }

  document.removeEventListener('click', handleDocumentClick)
  window.removeEventListener('keydown', handleKeydown)
})

onBeforeUnmount(() => {
  document.removeEventListener('click', handleDocumentClick)
  window.removeEventListener('keydown', handleKeydown)
})
</script>

<style scoped>
.lunar-page-actions {
  position: relative;
  z-index: 2;
  display: inline-flex;
  flex-shrink: 0;
}

.lunar-page-menu-trigger {
  width: 25px;
  height: 25px;
  margin-left: 2px;
  border: 1px solid transparent;
  border-radius: 8px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: #6f6f6f;
  background: transparent;
  transition:
    background-color 140ms ease,
    border-color 140ms ease,
    color 140ms ease;
}

.lunar-page-menu-trigger:hover,
.lunar-page-actions.is-open .lunar-page-menu-trigger {
  color: #f1f1f1;
  border-color: #2f2f2f;
  background: #202020;
}

.lunar-page-menu-trigger i {
  font-size: 14px;
}

.lunar-page-menu {
  position: absolute;
  z-index: 80;
  top: calc(100% + 7px);
  right: 0;
  width: 218px;
  overflow: hidden;
  border: 1px solid #2f2f2f;
  border-radius: 12px;
  padding: 6px;
  background: #191919;
  box-shadow: 0 18px 55px rgba(0, 0, 0, 0.48);
}

.lunar-page-menu-item {
  width: 100%;
  border: 0;
  border-radius: 9px;
  display: flex;
  align-items: flex-start;
  gap: 9px;
  padding: 8px;
  color: #cfcfcf;
  background: transparent;
  text-align: left;
  transition:
    background-color 140ms ease,
    color 140ms ease;
}

.lunar-page-menu-item:hover {
  color: #f1f1f1;
  background: #242424;
}

.lunar-page-menu-item.danger {
  color: #f1a6a6;
}

.lunar-page-menu-item.danger:hover {
  color: #ffc8c8;
  background: rgba(255, 94, 94, 0.1);
}

.lunar-page-menu-item i {
  width: 16px;
  margin-top: 1px;
  color: #858585;
  font-size: 14px;
}

.lunar-page-menu-item.danger i {
  color: currentColor;
}

.lunar-page-menu-item span {
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.lunar-page-menu-item strong {
  color: inherit;
  font-size: 12.5px;
  font-weight: 680;
  line-height: 1.25;
}

.lunar-page-menu-item small {
  color: #858585;
  font-size: 11px;
  line-height: 1.35;
}

.lunar-page-menu-separator {
  height: 1px;
  margin: 5px 3px;
  background: #2b2b2b;
}

.lunar-page-menu-pop-enter-active,
.lunar-page-menu-pop-leave-active {
  transition:
    opacity 130ms ease,
    transform 130ms ease;
}

.lunar-page-menu-pop-enter-from,
.lunar-page-menu-pop-leave-to {
  opacity: 0;
  transform: translateY(-4px) scale(0.98);
}

@media (prefers-reduced-motion: reduce) {
  .lunar-page-menu-trigger,
  .lunar-page-menu-pop-enter-active,
  .lunar-page-menu-pop-leave-active {
    transition: none;
  }
}

/* Final Notion menu polish */
.lunar-page-menu-trigger {
  width: 22px;
  height: 22px;
  margin-left: 2px;
  border: 0;
  border-radius: 4px;
  color: #85837d;
}

.lunar-page-menu-trigger:hover,
.lunar-page-actions.is-open .lunar-page-menu-trigger {
  color: #ededeb;
  border-color: transparent;
  background: rgba(255, 255, 255, 0.075);
}

.lunar-page-menu-trigger i {
  font-size: 13px;
}

.lunar-page-menu {
  width: 226px;
  top: calc(100% + 5px);
  border-color: #30302d;
  border-radius: 8px;
  padding: 5px;
  background: #202020;
  box-shadow:
    0 16px 38px rgba(0, 0, 0, 0.36),
    0 0 0 1px rgba(255, 255, 255, 0.025);
}

.lunar-page-menu-item {
  border-radius: 5px;
  gap: 8px;
  padding: 7px 8px;
  color: #d4d4d0;
}

.lunar-page-menu-item:hover {
  color: #f1f1ef;
  background: rgba(255, 255, 255, 0.065);
}

.lunar-page-menu-item.danger {
  color: #d9a1a1;
}

.lunar-page-menu-item.danger:hover {
  color: #f0b8b8;
  background: rgba(255, 255, 255, 0.06);
}

.lunar-page-menu-item i {
  color: #8f8d86;
  font-size: 13px;
}

.lunar-page-menu-item strong {
  font-size: 12.5px;
  font-weight: 600;
}

.lunar-page-menu-item small {
  color: #8f8d86;
}

.lunar-page-menu-separator {
  margin: 5px 4px;
  background: #30302d;
}

</style>
