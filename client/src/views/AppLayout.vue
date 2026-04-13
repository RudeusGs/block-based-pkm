<template>
  <div class="app-wrapper bg-surface-lowest text-on-surface d-flex vh-100 vw-100 overflow-hidden">
    <SidebarLeft />
    
    <div class="flex-grow-1 d-flex flex-column position-relative overflow-auto">
      <header class="top-nav navbar sticky-top px-4 border-bottom border-outline-variant" style="z-index: 10;">
        <div class="container-fluid d-flex justify-content-between align-items-center">
        <nav class="d-flex align-items-center small text-on-surface-variant fw-medium gap-2">
          <span class="cursor-pointer hover-white">Workspace</span>
          <span class="divider">/</span>
          <span class="cursor-pointer hover-white">Projects</span>
          <span class="divider">/</span>
          <span class="text-on-surface">Q4 Roadmap</span>
        </nav>

        <div class="d-flex align-items-center gap-3">
          <button class="btn btn-share d-flex align-items-center gap-2 py-1 px-3">
            <span class="material-symbols-outlined fs-7">share</span>
            <span class="fw-medium small">Share</span>
          </button>
          <div class="d-flex align-items-center gap-1">
            <button class="btn btn-icon-ghost"><span class="material-symbols-outlined">star</span></button>
            <button class="btn btn-icon-ghost"><span class="material-symbols-outlined">history</span></button>
            <button class="btn btn-icon-ghost"><span class="material-symbols-outlined">more_horiz</span></button>
          </div>
        </div>
      </div>
    </header>

    <main class="content-container w-100 mx-auto px-4 pb-5">
      <section class="mt-5 header-group">
        <div class="hero-banner position-relative rounded-4 overflow-hidden mb-4 bg-surface-container">
          <img 
            class="w-100 h-100 object-fit-cover opacity-60" 
            src="https://lh3.googleusercontent.com/aida-public/AB6AXuCHKgnShiuxNCbtthBDWD8gAFXRJpTZ6RSiyXj8grAA7xXANbnEqoJDaOm-T0F5Oly5mV5BfZ-4RF4OcGDNAenyjt6l3uXNK_-Nu8kXj0iV1KHBY2M9JtvkLYU9rmF0kxcqZgW81LnThknzajWEqhSUPF-CPF9lz0dXCvmY_MzCoDG3nGxFcHTUnGxuCUAT6MU1reEH-nJcQpGji2uFsiuw-n9B9fTbCXXmiRDM7iXHl4sL3QBtg_aJj55ILPnOUqlD-kPmqJ94Ulo" 
            alt="Hero Background"
          />
          <div class="banner-overlay position-absolute top-0 start-0 w-100 h-100"></div>
        </div>

        <div class="emoji-box d-flex align-items-center justify-content-center bg-surface-container rounded-3 mb-3 fs-2">
          🚀
        </div>

        <h1 class="display-5 fw-extrabold text-on-surface tracking-tighter mb-2">
          Q4 Strategic Roadmap
        </h1>
        <p class="text-on-surface-variant small d-flex align-items-center gap-2 fw-medium">
          Last edited 2h ago
          <span class="dot-separator"></span>
          <span class="text-primary-custom">Planning Phase</span>
        </p>
      </section>

      <section class="mt-5 space-y-block">
        <div class="block-row position-relative">
          <div class="drag-handle-box position-absolute translate-middle-y">
            <span class="material-symbols-outlined icon-add">add</span>
            <span class="material-symbols-outlined icon-drag">drag_indicator</span>
          </div>
          <p class="fs-5 fw-light text-on-surface-dim leading-relaxed mb-0">
            As we approach the final quarter, our primary focus shifts toward platform horizontal scaling and architectural hardening. The infrastructure must support the projected 400% increase in concurrent workspace sessions following the enterprise rollout.
          </p>
        </div>

        <div class="block-row position-relative pt-4">
          <div class="drag-handle-box position-absolute translate-middle-y">
            <span class="material-symbols-outlined icon-add">add</span>
            <span class="material-symbols-outlined icon-drag">drag_indicator</span>
          </div>
          <h2 class="h3 fw-bold text-on-surface tracking-tight mb-0">Core Objectives</h2>
        </div>

        <div class="checklist-container mt-4">
          <div class="block-row d-flex align-items-start gap-3 mb-3">
            <div class="check-box checked mt-1 d-flex align-items-center justify-content-center rounded border border-2 border-primary-custom bg-primary-soft">
              <span class="material-symbols-outlined fw-bold">check</span>
            </div>
            <span class="text-on-surface-variant text-decoration-line-through">Finalize multi-region database replication clusters</span>
          </div>
          <div class="block-row d-flex align-items-start gap-3 mb-3">
            <div class="check-box mt-1 rounded border border-2 border-outline transition-200"></div>
            <span class="text-on-surface">Deploy auto-scaling groups for WebSocket servers</span>
          </div>
        </div>

        <div class="block-row ps-2 mt-4">
          <ul class="list-unstyled space-y-2">
            <li class="d-flex align-items-start gap-3">
              <span class="text-primary-custom mt-1 small">●</span>
              <span>Reduce initial page load to under 800ms globally</span>
            </li>
            <li class="d-flex align-items-start gap-3">
              <span class="text-primary-custom mt-1 small">●</span>
              <span>Implementation of edge-caching for workspace metadata</span>
            </li>
          </ul>
        </div>
      </section>

      <section class="mt-5 pt-5 border-top border-outline-variant-soft">
        <div class="d-flex align-items-center justify-content-between mb-4">
          <h3 class="text-uppercase tracking-widest text-on-surface-variant fw-bold fs-7">Active Tasks</h3>
          <span class="badge rounded-pill bg-dark-container text-on-secondary-container px-3 fw-bold fs-11">3 OPEN</span>
        </div>

        <div class="task-list-wrapper rounded-3 overflow-hidden border border-outline-variant-soft">
          <div class="task-item d-flex align-items-center justify-content-between p-3 border-bottom border-outline-variant-soft">
            <div class="d-flex align-items-center gap-3">
              <span class="material-symbols-outlined text-error fill-1">error</span>
              <span class="fw-medium fs-13">Refactor global state management for mobile</span>
              <span class="badge-tag">DEV-102</span>
            </div>
            <div class="d-flex align-items-center gap-3">
              <span class="small text-on-surface-variant">Oct 24</span>
              <img src="https://lh3.googleusercontent.com/aida-public/AB6AXuAcxuZLZw4scIz8iXDPm_qDOZOsWN4wBevaLA5KYhh8ZKBSLhOHxl70uI4Xw4l-JOqt14vVs7aA8xneGujEQFGTjGN1qO4GqK2qqkVcagY9sTo9mDaNamEuqU-N-JLrVxV-gpX30lYm_gUnTJkF-mbUrAg8B5hhkRap2TxCE_6ghYPMaKWvb6USqn9OAj9PzvM9B23dIHyGD9vDiqrxpWrp1t13YlcOWF6pBKpzHMVe6Sjl94Dh1Rh-y25G5pRVE_x-HYY3PbykO2k" class="avatar-small rounded-circle grayscale">
            </div>
          </div>
          <div class="task-item d-flex align-items-center justify-content-between p-3 border-bottom border-outline-variant-soft">
            <div class="d-flex align-items-center gap-3">
              <span class="material-symbols-outlined text-primary-custom fill-1">pending</span>
              <span class="fw-medium fs-13">Design review for dark mode depth patterns</span>
              <span class="badge-tag">DES-44</span>
            </div>
            <div class="d-flex align-items-center gap-3">
              <span class="small text-on-surface-variant">Oct 26</span>
              <img src="https://lh3.googleusercontent.com/aida-public/AB6AXuCt9zWN22Z36DURyMV1dtEpVdA4HE9Df4ZLVH0duuMD70CFGkLO-CUAQnBhxScbCw2sMxakKbCzuU49AS5djl88cN1th8lsOlQmlnMvCLLmkCPzehDLx76fqnCRG-6wNBU5MhwVJvlYapSWV8hGMPPal94qra5c0eLj6UTZBzOej_aizsaJfFl8t3ubEGSZvaTmbrruEEY76v-vMMhK9QwCFTxg_yNXoyfPr1hGivT5dg5F8DncquXIhOshNE5OK_iC7Nx6n9FIzp4" class="avatar-small rounded-circle grayscale">
            </div>
          </div>
        </div>
      </section>

      <section class="mt-5">
        <h3 class="text-uppercase tracking-widest text-on-surface-variant fw-bold fs-7 mb-4">Subpages</h3>
        <div class="row g-3">
          <div class="col-md-6">
            <div class="card subpage-card p-4 h-100 border-outline-variant-soft">
              <div class="d-flex align-items-center gap-3 mb-2">
                <span class="material-symbols-outlined text-primary-custom">description</span>
                <h4 class="h6 mb-0 fw-bold">Engineering Specs</h4>
              </div>
              <p class="text-on-surface-variant small mb-0">Technical breakdown of the microservices architecture and API contracts.</p>
            </div>
          </div>
          <div class="col-md-6">
            <div class="card subpage-card p-4 h-100 border-outline-variant-soft">
              <div class="d-flex align-items-center gap-3 mb-2">
                <span class="material-symbols-outlined text-primary-custom">palette</span>
                <h4 class="h6 mb-0 fw-bold">Design Assets</h4>
              </div>
              <p class="text-on-surface-variant small mb-0">Figma files, design tokens, and high-fidelity prototypes for Q4 features.</p>
            </div>
          </div>
        </div>
      </section>
    </main>

    <button class="fab-btn position-fixed bottom-0 end-0 m-4 rounded-circle border-0 d-flex align-items-center justify-content-center shadow-lg">
      <span class="material-symbols-outlined fs-2">chat_bubble</span>
    </button>
    </div>
  </div>
</template>

<script setup>
import SidebarLeft from '../components/SidebarLeft.vue'
</script>

<style scoped>
.bg-surface-lowest { background-color: #000000; }
.bg-surface-container { background-color: #191a1a; }
.text-on-surface { color: #e7e5e4; }
.text-on-surface-dim { color: rgba(231, 229, 228, 0.9); }
.text-on-surface-variant { color: #acabaa; }
.text-primary-custom { color: #bdc2ff; }
.bg-primary-soft { background-color: rgba(189, 194, 255, 0.1); }
.border-outline-variant-soft { border-color: rgba(72, 72, 72, 0.15) !important; }
.bg-dark-container { background-color: #3b3b3f; }

.app-wrapper {
  min-height: 100vh;
  font-family: 'Inter', sans-serif;
}

.content-container {
  max-width: 800px;
}

.top-nav {
  background-color: rgba(0, 0, 0, 0.8);
  backdrop-filter: blur(20px);
  height: 56px;
}

.btn-share {
  background-color: #1f2020;
  color: #e7e5e4;
  border: none;
  transition: 0.2s;
}
.btn-share:hover { background-color: #2c2c2c; }

.btn-icon-ghost {
  color: #acabaa;
  padding: 6px;
  border: none;
}
.btn-icon-ghost:hover { background-color: #252626; color: #e7e5e4; }

.hero-banner { height: 192px; }
.banner-overlay {
  background: linear-gradient(to top, #000000, transparent);
}
.emoji-box { width: 48px; height: 48px; }

.block-row:hover .drag-handle-box { opacity: 1; }
.drag-handle-box {
  left: -45px;
  opacity: 0;
  transition: opacity 0.2s;
  display: flex;
  gap: 4px;
  color: #484848;
}
.drag-handle-box span:hover { color: #e7e5e4; cursor: pointer; }

.check-box { width: 20px; height: 20px; transition: 0.2s; }
.check-box.checked { color: #293398; }
.check-box.checked span { font-size: 14px; }
.task-item { background-color: #131313; cursor: pointer; transition: 0.2s; }
.task-item:hover { background-color: #191a1a; }
.badge-tag {
  font-size: 11px;
  padding: 2px 8px;
  background-color: #252626;
  color: #767575;
  border-radius: 4px;
}
.avatar-small { width: 24px; height: 24px; }
.grayscale { filter: grayscale(1); }
.grayscale:hover { filter: grayscale(0); }

.subpage-card {
  background-color: #131313;
  transition: 0.2s;
  cursor: pointer;
}
.subpage-card:hover {
  background-color: #191a1a;
  border-color: #bdc2ff !important;
}

.fab-btn {
  width: 56px;
  height: 56px;
  background: linear-gradient(135deg, #bdc2ff, #303b9f);
  color: #293398;
  transition: 0.2s;
}
.fab-btn:hover { transform: scale(1.05); }

.material-symbols-outlined {
  font-variation-settings: 'FILL' 0, 'wght' 300, 'GRAD' 0, 'opsz' 24;
  font-size: 18px;
}
.fill-1 { font-variation-settings: 'FILL' 1 !important; }

.cursor-pointer { cursor: pointer; }
.hover-white:hover { color: #fff; }
.tracking-tighter { letter-spacing: -0.05em; }
.tracking-widest { letter-spacing: 0.1em; }
.fs-11 { font-size: 11px; }
.fs-13 { font-size: 13px; }
.fw-extrabold { font-weight: 800; }
.dot-separator { width: 4px; height: 4px; border-radius: 50%; background-color: #484848; }
.space-y-block > div { margin-bottom: 32px; }
</style>