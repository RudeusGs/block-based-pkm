import { ref } from 'vue'

export type SidebarPanel = 'myTasks' | 'recommendations' | 'settings' | null

export function useSidebarShell() {
  const isCollapsed = ref(false)
  const activePanel = ref<SidebarPanel>(null)

  function expandSidebar() {
    isCollapsed.value = false
  }

  function collapseSidebar() {
    isCollapsed.value = true
    activePanel.value = null
  }

  function openPanel(panel: SidebarPanel) {
    if (isCollapsed.value) {
      isCollapsed.value = false
    }

    activePanel.value = activePanel.value === panel ? null : panel
  }

  function closePanel() {
    activePanel.value = null
  }

  return {
    isCollapsed,
    activePanel,
    expandSidebar,
    collapseSidebar,
    openPanel,
    closePanel,
  }
}
