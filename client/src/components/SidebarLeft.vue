<template>
  <aside 
    class="sidebar-left d-flex flex-column py-4 px-3 border-end border-outline-variant transition-300"
    :class="{ 'collapsed': isCollapsed, 'sidebar-clickable': isCollapsed }"
    @click="expandSidebar"
  >
    <div class="sidebar-header d-flex align-items-center mb-4 px-2" :class="isCollapsed ? 'justify-content-center' : 'justify-content-between'">
      <div class="d-flex align-items-center overflow-hidden">
        <div class="logo-box rounded-3 bg-dark d-flex align-items-center justify-content-center text-white flex-shrink-0">
          <span class="material-symbols-outlined fill-1">grid_view</span>
        </div>
        <div v-if="!isCollapsed" class="d-flex flex-column overflow-hidden ms-3 fade-in">
          <span class="fs-6 fw-bold text-light text-nowrap">Block-based</span>
          <span class="text-uppercase text-on-surface-variant tracking-widest" style="font-size: 10px;">Pro Workspace</span>
        </div>
      </div>

      <button 
        v-if="!isCollapsed" 
        @click.stop="isCollapsed = true" 
        class="btn-text-toggle fade-in flex-shrink-0"
      >
        &lt;&lt;
      </button>
    </div>

    <div class="mb-4 px-1">
      <div class="search-wrapper d-flex align-items-center rounded-4 border border-outline-variant transition-200" :class="isCollapsed ? 'justify-content-center py-2' : 'px-3 py-2 gap-2'">
        <span class="material-symbols-outlined text-on-surface-variant fs-6 flex-shrink-0">search</span>
        <input 
          v-if="!isCollapsed" 
          type="text" 
          class="search-input bg-transparent border-0 flex-grow-1 text-white fs-7 fade-in" 
          placeholder="Search..."
        >
      </div>
    </div>

    <nav class="flex-grow-1 overflow-x-hidden overflow-y-auto scrollbar-hide">
      <div class="px-1 pb-2">
        <div class="accordion-projects position-relative">
          
          <div class="d-flex align-items-center nav-link-custom group-item position-relative" :class="{ 'justify-content-center ps-0': isCollapsed }">
            <button @click.stop="toggleProjects" class="border-0 d-flex align-items-center bg-transparent text-inherit w-100" :class="isCollapsed ? 'justify-content-center p-0' : 'gap-2 py-2 ps-3'">
              <span class="material-symbols-outlined flex-shrink-0 text-white folder-icon">folder</span>
              
              <span v-if="!isCollapsed" class="fade-in text-start flex-grow-1 ms-1 fw-medium text-white">Projects</span>
              <span v-if="!isCollapsed" class="material-symbols-outlined fs-7 transition-200 me-2 text-on-surface-variant" :class="{ 'rotate-minus-90': !isProjectsOpen }">expand_more</span>
            </button>
            <button v-if="!isCollapsed" @click.stop="createProject" class="btn-add-small fade-in ms-auto me-2"><span class="material-symbols-outlined">add</span></button>
          </div>

          <Transition name="expand">
            <div v-show="!isCollapsed && isProjectsOpen" class="project-tree-container ms-4 ps-2 mt-1 border-start">
              
              <div class="project-branch group-item">
                <div class="d-flex align-items-center pe-2 rounded-2 position-relative">
                  <button @click.stop="isSubBranchOpen = !isSubBranchOpen" class="project-child-link border-0 d-flex align-items-center py-1 px-0 bg-transparent fs-7 flex-grow-1">
                    <span class="tree-dot me-2"></span>
                    <span class="flex-grow-1 text-start">Q4 Roadmap</span>
                    <span class="material-symbols-outlined fs-8 transition-200" :class="{ 'rotate-minus-90': !isSubBranchOpen }">chevron_right</span>
                  </button>
                  <button @click.stop="addChildAction" class="btn-add-child"><span class="material-symbols-outlined">add</span></button>
                </div>

                <Transition name="expand">
                  <div v-show="isSubBranchOpen" class="ms-3 ps-2 border-start border-tree-sub mt-1">
                    <div class="d-flex align-items-center pe-2 group-item">
                      <a href="#" class="project-child-link py-1 fs-7 flex-grow-1 text-decoration-none d-flex align-items-center">
                        <span class="tree-dot-sub me-2"></span>UI Design
                      </a>
                      <button class="btn-add-child"><span class="material-symbols-outlined">add</span></button>
                    </div>
                  </div>
                </Transition>
              </div>

              <div class="d-flex align-items-center pe-2 group-item">
                <a href="#" class="project-child-link d-flex align-items-center py-1 text-decoration-none fs-7 flex-grow-1">
                  <span class="tree-dot me-2"></span>Client Briefs
                </a>
                <button class="btn-add-child"><span class="material-symbols-outlined">add</span></button>
              </div>

            </div>
          </Transition>
        </div>

        <div class="d-flex align-items-center nav-link-custom position-relative" :class="[isCollapsed ? 'justify-content-center ps-0 mt-4' : 'mt-1']">
          <button class="border-0 d-flex align-items-center bg-transparent text-inherit w-100" :class="isCollapsed ? 'justify-content-center p-0' : 'gap-2 py-2 ps-3'">
            <span class="material-symbols-outlined flex-shrink-0 text-white folder-icon">task_alt</span>
            <span v-if="!isCollapsed" class="fade-in text-start flex-grow-1 ms-1 fw-medium text-white">My Tasks</span>
          </button>
        </div>
      </div>
    </nav>

    <div class="mt-auto px-1 pb-2 pt-3 border-top border-outline-variant">
      <div class="d-flex flex-column gap-1">
        <a href="#" class="nav-link-custom d-flex align-items-center rounded-3" :class="isCollapsed ? 'justify-content-center py-2' : 'gap-3 py-2 ps-3'">
          <span class="material-symbols-outlined flex-shrink-0">account_circle</span>
          <span v-if="!isCollapsed" class="fade-in">Profile</span>
        </a>
        <a href="#" class="nav-link-custom d-flex align-items-center rounded-3" :class="isCollapsed ? 'justify-content-center py-2' : 'gap-3 py-2 ps-3'">
          <span class="material-symbols-outlined flex-shrink-0">settings</span>
          <span v-if="!isCollapsed" class="fade-in">Settings</span>
        </a>
      </div>
    </div>
  </aside>
</template>

<script setup>
import { ref } from 'vue';

const isCollapsed = ref(false);
const isProjectsOpen = ref(true);
const isSubBranchOpen = ref(false);

const expandSidebar = () => { if (isCollapsed.value) isCollapsed.value = false; };
const toggleProjects = () => {
  if (isCollapsed.value) { isCollapsed.value = false; isProjectsOpen.value = true; }
  else { isProjectsOpen.value = !isProjectsOpen.value; }
};
const createProject = () => console.log('Main add');
const addChildAction = () => console.log('Child add');
</script>

<style scoped src="./css/SidebarLeft.css"></style>