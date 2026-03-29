<template>
  <div class="landing-page">
    <div class="bg-grid"></div>
    <div class="bg-strip"></div>

    <!-- HEADER -->
    <header class="landing-header">
      <div class="brand">
        <img :src="logo" alt="logo" class="brand-logo-img" />
        <span class="brand-name">CollabBoard</span>
      </div>

      <nav class="landing-nav">
        <button class="nav-link" @click="scrollToFeatures">
          <i class="bi bi-grid-3x3-gap me-1"></i> Tính năng
        </button>
        <button class="nav-link nav-link-primary" @click="goLogin">
          <i class="bi bi-box-arrow-in-right me-1"></i> Đăng nhập
        </button>
      </nav>
    </header>

    <!-- MAIN -->
    <main class="landing-main">
      <!-- LEFT -->
      <section class="hero-left">
        <div class="tag-row">
          <span class="tag-pill">
            <i class="bi bi-lightning-charge me-1"></i>
            block-based workspace
          </span>
        </div>

        <h1 class="hero-title">
          Xây dựng
          <span class="hero-highlight">tri thức của bạn</span>
          như Notion
        </h1>

        <p class="hero-desc">
          Ghi chú, quản lý công việc, lưu trữ ý tưởng và cộng tác realtime
          trong một workspace duy nhất. Mọi thứ đều là block — linh hoạt, mạnh mẽ, gọn gàng.
        </p>

        <div class="hero-chips">
          <span class="chip"><i class="bi bi-journal-text me-1"></i> Notes</span>
          <span class="chip"><i class="bi bi-check2-square me-1"></i> Tasks</span>
          <span class="chip"><i class="bi bi-code-slash me-1"></i> Code blocks</span>
        </div>

        <div class="hero-actions">
          <button class="btn-primary" @click="goLogin">
            <i class="bi bi-play-fill me-1"></i>
            Bắt đầu ngay
          </button>
        </div>

        <div class="hero-meta">
          <div class="meta-item">
            <span class="meta-dot"></span>
            <span>Realtime collaboration</span>
          </div>
          <div class="meta-item">
            <span class="meta-dot"></span>
            <span>Block-based editing như Notion</span>
          </div>
        </div>
      </section>

      <!-- RIGHT (PREVIEW) -->
      <section class="hero-right">
        <div class="preview-shell">
          <div class="preview-sidebar">
            <div class="preview-dot active"></div>
            <div class="preview-dot"></div>
            <div class="preview-dot"></div>
          </div>

          <div class="preview-main">
            <div class="preview-topbar">
              <span>📄 Project Plan</span>
            </div>

            <div class="preview-body">
              <p>👉 Xây dựng hệ thống PKM</p>
              <p>✔️ Block editor</p>
              <p>✔️ Realtime sync</p>
              <p class="code-block">const idea = "Build Notion clone 🚀"</p>
            </div>
          </div>
        </div>
      </section>
    </main>

    <!-- FEATURES -->
    <section ref="features" class="landing-features">
      <div class="feature-card">
        <div class="feature-icon">
          <i class="bi bi-blockquote-left"></i>
        </div>
        <h3>Block-based editor</h3>
        <p>Mỗi đoạn nội dung là một block riêng, dễ dàng kéo thả và chỉnh sửa.</p>
      </div>

      <div class="feature-card">
        <div class="feature-icon">
          <i class="bi bi-people-fill"></i>
        </div>
        <h3>Realtime collaboration</h3>
        <p>Cùng team chỉnh sửa nội dung ngay lập tức, không cần refresh.</p>
      </div>

      <div class="feature-card">
        <div class="feature-icon">
          <i class="bi bi-kanban-fill"></i>
        </div>
        <h3>Task & Kanban</h3>
        <p>Quản lý công việc trực quan với board và checklist.</p>
      </div>

      <div class="feature-card">
        <div class="feature-icon">
          <i class="bi bi-moon-stars-fill"></i>
        </div>
        <h3>Dark mode</h3>
        <p>Thiết kế tối ưu cho dev làm đêm 😏</p>
      </div>
    </section>

    <!-- FOOTER -->
    <footer class="landing-footer">
      <span>© {{ new Date().getFullYear() }} CollabBoard</span>
      <span class="footer-right">
        <i class="bi bi-github me-1"></i> v1.0
      </span>
    </footer>
  </div>
</template>

<script>
import logo from '@/assets/logo/logo_Convox.png'

export default {
  name: 'LandingView',
  data() {
    return {
      logo,
    }
  },
  methods: {
    goLogin() {
      this.$router.push('/login')
    },
    scrollToFeatures() {
      this.$refs.features.scrollIntoView({ behavior: 'smooth' })
    },
  },
}
</script>

<style scoped>
.landing-page {
  min-height: 100vh;
  background: #020617;
  color: #e5e7eb;
  display: flex;
  flex-direction: column;
}

.bg-grid {
  position: absolute;
  inset: 0;
  background-size: 40px 40px;
  opacity: 0.3;
}

.landing-header {
  display: flex;
  justify-content: space-between;
  padding: 20px 40px;
}

.brand {
  display: flex;
  align-items: center;
  gap: 10px;
}

.brand-logo-img {
  width: 36px;
}

.landing-main {
  display: grid;
  grid-template-columns: 1fr 1fr;
  padding: 40px;
}

.hero-title {
  font-size: 36px;
  font-weight: bold;
}

.hero-highlight {
  color: #6366f1;
}

.preview-shell {
  background: #111827;
  border-radius: 12px;
  display: flex;
}

.preview-sidebar {
  width: 60px;
  background: #020617;
}

.preview-main {
  padding: 10px;
}

.code-block {
  background: #000;
  padding: 6px;
  border-radius: 6px;
  color: #22c55e;
}

.feature-card {
  background: #111827;
  padding: 16px;
  border-radius: 10px;
}

.landing-features {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 16px;
  padding: 20px 40px;
}

.landing-footer {
  padding: 20px;
  display: flex;
  justify-content: space-between;
}
</style>