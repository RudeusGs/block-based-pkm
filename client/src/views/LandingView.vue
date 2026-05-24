<template>
  <div class="landing-page">
    <canvas ref="particleCanvas" class="particle-canvas"></canvas>
    <nav class="navbar navbar-expand-md fixed-top">
      <div class="container-xl px-4">
        <a class="navbar-brand d-flex align-items-center gap-2 fw-bold" href="#" @click.prevent="scrollToTop">
          <span class="material-symbols-outlined text-primary-accent">architecture</span>
          Block Paged
        </a>
        <button class="navbar-toggler border-0" type="button" data-bs-toggle="collapse" data-bs-target="#mainNav">
          <span class="material-symbols-outlined text-muted">menu</span>
        </button>
        <div class="collapse navbar-collapse" id="mainNav">
          <ul class="navbar-nav mx-auto gap-4">
            <li class="nav-item">
              <a class="nav-link" href="#demo" @click.prevent="scrollToSection('demo')">Bản thử</a>
            </li>
            <li class="nav-item">
              <a class="nav-link nav-active" href="#features" @click.prevent="scrollToSection('features')">Tính năng</a>
            </li>
            <li class="nav-item">
              <a class="nav-link" href="#how-it-works" @click.prevent="scrollToSection('how-it-works')">Cách hoạt động</a>
            </li>

          </ul>
          <div class="d-flex align-items-center gap-3">
            <button
              class="btn btn-primary-white px-4 py-2 rounded-3 fw-semibold"
              type="button"
              :disabled="isNavigating"
              :aria-busy="isNavigating"
              @click="goToAuthEntry"
            >
              Bắt đầu
            </button>
          </div>
        </div>
      </div>
    </nav>

    <main style="padding-top: 64px;">
      <section class="hero-section position-relative overflow-hidden pt-5 pb-5">
        <div class="hero-glow"></div>
        <div class="container-xl px-4 text-center">
          <div class="badge-pill d-inline-flex align-items-center gap-2 px-3 py-2 mb-4 rounded-pill">
            <span class="pulse-dot"></span>
            <span class="text-muted small text-uppercase fw-medium" style="letter-spacing: 0.05em; font-size: 0.6875rem;">Bản thử đồ án đã sẵn sàng</span>
          </div>
          <h1 class="hero-title fw-black mb-4">
            Quản lý tri thức <br />và cộng tác thời gian thực.
          </h1>
          <p class="text-muted fs-5 mb-5 mx-auto" style="max-width: 600px; line-height: 1.7;">
            Một không gian làm việc lấy cảm hứng từ Notion: ghi chú theo khối, quản lý công việc, cộng tác thời gian thực, kết bạn, nhắn tin và AI ưu tiên công việc trong cùng một hệ thống.
          </p>
          <div class="d-flex flex-column flex-sm-row justify-content-center gap-3">
            <button
              class="btn btn-primary-solid px-5 py-3 rounded-3 fw-bold"
              type="button"
              :disabled="isNavigating"
              :aria-busy="isNavigating"
              @click="goToAuthEntry"
            >
              Vào ứng dụng
            </button>
            <button
              class="btn btn-ghost px-5 py-3 rounded-3 fw-semibold"
              type="button"
              @click="scrollToSection('demo')"
            >
              Xem bản thử
            </button>
          </div>
          <div class="hero-stats d-flex flex-wrap justify-content-center gap-3 mt-5">
            <div class="hero-stat rounded-4 px-4 py-3">
              <strong>Thời gian thực</strong>
              <span>SignalR + Redis</span>
            </div>
            <div class="hero-stat rounded-4 px-4 py-3">
              <strong>AI</strong>
              <span>Ưu tiên công việc</span>
            </div>
            <div class="hero-stat rounded-4 px-4 py-3">
              <strong>Kết nối</strong>
              <span>Bạn bè + nhắn tin</span>
            </div>
          </div>
        </div>
      </section>

      <section id="demo" class="container-xl px-4 mb-5 pb-5">
        <div class="demo-wrapper position-relative">
          <div class="demo-glow-bg"></div>
          <div class="demo-panel position-relative rounded-4 overflow-hidden">
            <div class="demo-titlebar d-flex align-items-center px-4 py-3">
              <div class="d-flex gap-2">
                <div class="dot-gray rounded-circle"></div>
                <div class="dot-gray rounded-circle"></div>
                <div class="dot-gray rounded-circle"></div>
              </div>
              <div class="demo-url mx-auto d-flex align-items-center gap-2 px-3 py-1 rounded-2">
                <span class="material-symbols-outlined" style="font-size:14px;">lock</span>
                <span class="text-muted" style="font-size: 12px;">block-paged.app/khong-gian/do-an-tot-nghiep</span>
              </div>
            </div>
            <div class="row g-0" style="height: 540px;">
              <div class="col-3 demo-sidebar d-none d-md-block p-4">
                <div class="d-flex align-items-center gap-2 mb-4 text-primary-accent fw-semibold small">
                  <span class="material-symbols-outlined">dashboard</span> Không gian làm việc
                </div>
                <div class="d-flex flex-column gap-1">
                  <div class="d-flex align-items-center justify-content-between p-2 rounded-3" style="background: rgba(255,255,255,0.05); cursor: pointer;">
                    <div class="d-flex align-items-center gap-2 text-muted">
                      <span class="material-symbols-outlined text-primary-accent fs-6">folder_open</span>
                      <span class="small fw-medium text-white">Đồ án tốt nghiệp</span>
                    </div>
                    <span class="material-symbols-outlined fs-6 text-muted">expand_more</span>
                  </div>
                  <div class="d-flex flex-column gap-1 ms-3 ps-2 border-start border-secondary mb-2 mt-1">
                    <div class="d-flex align-items-center gap-2 p-1 px-2 rounded-2" style="background: rgba(255,255,255,0.08); cursor: pointer;">
                      <span class="material-symbols-outlined fs-6 text-primary-accent text-opacity-75" style="font-size: 14px !important;">description</span>
                      <span class="small fw-medium text-white">Kế hoạch báo cáo</span>
                    </div>
                    <div class="d-flex align-items-center gap-2 p-1 px-2 rounded-2 text-muted" style="cursor: pointer;">
                      <span class="material-symbols-outlined fs-6 text-muted" style="font-size: 14px !important;">description</span>
                      <span class="small">Tài liệu hệ thống</span>
                    </div>
                    <div class="d-flex align-items-center gap-2 p-1 px-2 rounded-2 text-muted" style="cursor: pointer;">
                      <span class="material-symbols-outlined fs-6 text-muted" style="font-size: 14px !important;">analytics</span>
                      <span class="small">Nhật ký hoạt động</span>
                    </div>
                  </div>
                  <div class="d-flex align-items-center gap-2 p-2 rounded-3 text-muted mt-2" style="cursor: pointer;">
                    <span class="material-symbols-outlined fs-6">folder</span>
                    <span class="small fw-medium">Ghi chú họp</span>
                  </div>
                  <div class="d-flex align-items-center gap-2 p-2 rounded-3 text-muted" style="cursor: pointer;">
                    <span class="material-symbols-outlined fs-6">folder</span>
                    <span class="small fw-medium">Nhật ký học tập</span>
                  </div>
                </div>
              </div>
              <div class="col-12 col-md-9 p-5">
                <div class="d-flex justify-content-between align-items-end mb-5">
                  <div>
                    <h3 class="fw-bold mb-1" style="font-size: 1.75rem; letter-spacing: -0.03em;">Không gian Block Paged</h3>
                    <p class="text-muted small mb-0">Đang cộng tác thời gian thực với thành viên</p>
                  </div>
                  <div class="d-flex" style="margin-right: -8px;">
                    <div class="avatar-circle bg-gray-700"></div>
                    <div class="avatar-circle bg-gray-600" style="margin-left: -8px;"></div>
                    <div class="avatar-circle bg-gray-500" style="margin-left: -8px;"></div>
                  </div>
                </div>
                <div class="row g-4">
                  <div class="col-md-6">
                    <div class="feature-card p-4 rounded-4 h-100">
                      <div class="d-flex align-items-center gap-2 mb-3">
                        <span class="material-symbols-outlined text-primary-accent" style="font-size: 16px;">task_alt</span>
                        <span class="text-muted text-uppercase fw-bold" style="font-size: 10px; letter-spacing: 0.1em;">Việc hôm nay</span>
                      </div>
                      <div class="d-flex flex-column gap-2">
                        <div class="d-flex align-items-start gap-3 p-2 rounded-3" style="background: rgba(255,255,255,0.03);">
                          <input type="checkbox" checked class="form-check-input mt-1 shadow-none" style="background-color: #ffffff; border-color: #ffffff;">
                          <div>
                            <div class="text-muted small fw-medium text-decoration-line-through">Hoàn thiện README và báo cáo</div>
                            <div class="text-muted" style="font-size: 10px;">Đã xong</div>
                          </div>
                        </div>
                        <div class="d-flex align-items-start gap-3 p-2 rounded-3 border-start border-2 border-white" style="background: rgba(255,255,255,0.05);">
                          <input type="checkbox" class="form-check-input mt-1 shadow-none" style="background-color: transparent; border-color: #404040;">
                          <div>
                            <div class="text-white small fw-medium">Bản thử đồng bộ và nhắn tin</div>
                            <div class="text-primary-accent" style="font-size: 10px;">Hạn chót: ngày mai</div>
                          </div>
                        </div>
                        <div class="d-flex align-items-start gap-3 p-2 rounded-3" style="background: transparent; border: 1px solid rgba(255,255,255,0.05);">
                          <input type="checkbox" class="form-check-input mt-1 shadow-none" style="background-color: transparent; border-color: #404040;">
                          <div>
                            <div class="text-muted small fw-medium">Kiểm tra Redis trước khi trình bày</div>
                            <div class="text-muted" style="font-size: 10px;">Ưu tiên cao</div>
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                  <div class="col-md-6">
                    <div class="feature-card p-4 rounded-4 h-100">
                      <div class="d-flex align-items-center gap-2 mb-3">
                        <span class="material-symbols-outlined text-muted" style="font-size: 16px;">analytics</span>
                        <span class="text-muted text-uppercase fw-bold" style="font-size: 10px; letter-spacing: 0.1em;">AI ưu tiên công việc</span>
                      </div>
                      <div class="velocity-chart rounded-3 d-flex align-items-end p-3 gap-2 pb-4">
                        <div class="bar-item position-relative" style="height: 40%;"><span class="position-absolute bottom-0 start-50 translate-middle-x text-muted" style="font-size: 9px; margin-bottom: -18px;">W1</span></div>
                        <div class="bar-item position-relative" style="height: 70%;"><span class="position-absolute bottom-0 start-50 translate-middle-x text-muted" style="font-size: 9px; margin-bottom: -18px;">W2</span></div>
                        <div class="bar-item bar-active position-relative" style="height: 90%;">
                           <div class="position-absolute top-0 start-50 translate-middle bg-white text-black rounded-pill fw-bold" style="font-size: 9px; padding: 2px 6px; margin-top: -8px;">24</div>
                           <span class="position-absolute bottom-0 start-50 translate-middle-x text-muted" style="font-size: 9px; margin-bottom: -18px;">W3</span>
                        </div>
                        <div class="bar-item position-relative" style="height: 50%;"><span class="position-absolute bottom-0 start-50 translate-middle-x text-muted" style="font-size: 9px; margin-bottom: -18px;">W4</span></div>
                      </div>
                    </div>
                  </div>
                  <!-- Block Editor Mock -->
                  <div class="col-12 mt-4 pt-2">
                     <div class="px-3">
                        <div class="d-flex align-items-center gap-2 mb-2 text-muted">
                           <span class="material-symbols-outlined" style="font-size: 14px; opacity: 0.5;">drag_indicator</span>
                           <h4 class="text-white fs-5 fw-semibold mb-0" style="letter-spacing: -0.02em;">Tổng quan dự án</h4>
                        </div>
                        <div class="d-flex align-items-start gap-2 mb-2 text-muted ms-4">
                           <p class="mb-0" style="font-size: 14px; line-height: 1.6;">Không gian này quản lý <span class="text-primary-accent bg-dark px-2 py-1 rounded-2" style="font-size: 12px; border: 1px solid #262626;">trang, công việc, thành viên, kết nối</span> và cộng tác thời gian thực bằng SignalR + Redis.</p>
                        </div>
                        <div class="d-flex align-items-center gap-2 mt-4 text-muted">
                           <span class="material-symbols-outlined" style="font-size: 14px; opacity: 0.5;">drag_indicator</span>
                           <span class="material-symbols-outlined text-primary-accent" style="font-size: 16px;">format_quote</span>
                           <p class="fst-italic border-start border-2 ps-3 mb-0 text-white-50" style="font-size: 14px; border-color: #404040 !important;">AI ưu tiên công việc tự gộp các việc gần giống nhau như ‘học bài’ và ‘học bài 1’, rồi chỉ gợi ý việc thật sự nên làm.</p>
                        </div>
                        <div class="d-flex align-items-center gap-2 mt-4 text-muted ms-4">
                           <span class="material-symbols-outlined text-primary-accent" style="font-size: 14px;">add</span>
                           <span class="text-muted" style="font-size: 14px;">Tạo trang, công việc, bình luận và theo dõi hoạt động trong một không gian</span>
                        </div>
                     </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      <section id="features" class="container-xl px-4 mb-5 pb-5">
        <div class="mb-5">
          <h2 class="fw-bold mb-2" style="font-size: 2.25rem; letter-spacing: -0.04em;">Đủ phân hệ để trình bày như một sản phẩm thật.</h2>
          <p class="text-muted">Tập trung vào trải nghiệm sạch, rõ quyền, đồng bộ thời gian thực ổn định và nghiệp vụ có chiều sâu.</p>
        </div>
        <div class="row g-4">
          <div class="col-12 col-md-8">
            <div class="bento-card p-5 rounded-4 d-flex flex-column justify-content-between" style="min-height: 380px;">
              <div>
                <span class="material-symbols-outlined text-white mb-3 fs-3">hub</span>
                <h4 class="fw-bold fs-4 mb-3 text-white">Cộng tác thời gian thực</h4>
                <p class="text-muted" style="max-width: 420px;">Nhiều người dùng có thể cùng mở không gian hoặc trang, thấy trạng thái đang hoạt động, đang nhập, thông báo và đồng bộ thay đổi nhanh qua SignalR.</p>
              </div>
              <div class="collab-bar d-flex align-items-center gap-3 rounded-3 p-3 mt-4">
                <span class="collab-chip">Bạn bè vừa online</span>
                <span class="collab-chip typing">Đang nhập...</span>
              </div>
            </div>
          </div>
          <div class="col-12 col-md-4">
            <div class="bento-card p-5 rounded-4" style="min-height: 380px;">
              <span class="material-symbols-outlined text-white mb-3 fs-3">bolt</span>
              <h4 class="fw-bold fs-4 mb-3 text-white">AI ưu tiên công việc</h4>
              <p class="text-muted">Gợi ý công việc dựa trên hạn chót, độ ưu tiên, trạng thái và khả năng gộp ý nghĩa để tránh lặp các việc giống nhau.</p>
            </div>
          </div>
          <div class="col-12 col-md-4">
            <div class="bento-card p-5 rounded-4" style="min-height: 280px;">
              <span class="material-symbols-outlined text-white mb-3 fs-3">grid_view</span>
              <h4 class="fw-bold fs-4 mb-3 text-white">Không gian & trang</h4>
              <p class="text-muted">Tổ chức tri thức bằng không gian làm việc, cây trang, trang yêu thích, trang gần đây, nhân bản và thùng rác/khôi phục theo luồng làm việc kiểu Notion.</p>
            </div>
          </div>
          <div class="col-12 col-md-8">
            <div class="bento-card p-5 rounded-4 d-flex justify-content-between align-items-start" style="min-height: 280px;">
              <div style="max-width: 280px;">
                <span class="material-symbols-outlined text-white mb-3 fs-3">terminal</span>
                <h4 class="fw-bold fs-4 mb-3 text-white">Kết nối & nhắn tin</h4>
                <p class="text-muted">Kết bạn, xem hồ sơ, nhắn tin thời gian thực, gửi ảnh và chia sẻ không gian làm việc cho bạn bè ngay trong hệ thống.</p>
              </div>
              <div class="command-panel p-3 rounded-3 d-none d-sm-block">
                <div class="d-flex flex-column gap-2">
                  <div class="px-2 py-1 rounded-2 text-white fw-bold text-uppercase" style="font-size: 10px; letter-spacing: 0.1em; background: rgba(255,255,255,0.1);">Chia sẻ không gian</div>
                  <div class="text-muted font-monospace" style="font-size: 13px;">&gt; Gửi không gian cho bạn bè</div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      <section id="how-it-works" class="how-section py-5 mb-5">
        <div class="container-xl px-4">
          <div class="text-center mb-5 pb-3">
            <h2 class="fw-bold mb-2 text-white" style="font-size: 2.25rem; letter-spacing: -0.04em;">Ba bước để bắt đầu làm việc.</h2>
            <p class="text-muted">Từ tạo không gian làm việc đến cộng tác và chốt công việc, mọi thứ nằm trong một luồng liền mạch.</p>
          </div>
          <div class="row g-5 position-relative">
            <div class="how-line d-none d-md-block"></div>
            <div class="col-12 col-md-4 text-center position-relative z-1">
              <div class="step-badge mx-auto mb-4">
                <span class="fw-bold text-white fs-5">01</span>
              </div>
              <h4 class="fw-bold mb-3 text-white">Tạo không gian</h4>
              <p class="text-muted small">Tạo không gian, trang, công việc và mời thành viên theo vai trò chủ sở hữu, quản lý, thành viên hoặc người xem.</p>
            </div>
            <div class="col-12 col-md-4 text-center position-relative z-1">
              <div class="step-badge mx-auto mb-4">
                <span class="fw-bold text-white fs-5">02</span>
              </div>
              <h4 class="fw-bold mb-3 text-white">Cộng tác</h4>
              <p class="text-muted small">Chỉnh sửa nội dung, bình luận công việc, theo dõi nhật ký hoạt động, nhận thông báo và nhắn tin thời gian thực.</p>
            </div>
            <div class="col-12 col-md-4 text-center position-relative z-1">
              <div class="step-badge mx-auto mb-4">
                <span class="fw-bold text-white fs-5">03</span>
              </div>
              <h4 class="fw-bold mb-3 text-white">Ưu tiên</h4>
              <p class="text-muted small">Dùng AI ưu tiên công việc để lọc việc quan trọng, tránh trùng lặp và tập trung vào việc nên làm trước.</p>
            </div>
          </div>
        </div>
      </section>

      <section class="container-xl px-4 mb-5 pb-5">
        <div class="row g-5 align-items-center">
          <div class="col-12 col-md-6 d-flex flex-column gap-4">
            <div class="testimonial-card p-5 rounded-4 position-relative">
              <span class="material-symbols-outlined quote-icon">format_quote</span>
              <p class="fs-5 fw-medium fst-italic mb-4 lh-lg text-white-50">
                "Một đồ án full-stack gom đủ các phần khó: xác thực, phân quyền, thời gian thực, Redis, công việc, kết nối xã hội, nhắn tin và AI gợi ý việc."
              </p>
              <div class="d-flex align-items-center gap-3">
                <div class="avatar-initial text-white fw-bold">MK</div>
                <div>
                  <h6 class="fw-bold mb-0 text-white">Phân hệ full-stack</h6>
                  <p class="text-muted mb-0 text-uppercase" style="font-size: 10px; letter-spacing: 0.1em;">Vue 3 + ASP.NET Core + PostgreSQL</p>
                </div>
              </div>
            </div>
            <div class="testimonial-card p-5 rounded-4 position-relative">
              <span class="material-symbols-outlined quote-icon">format_quote</span>
              <p class="fs-5 fw-medium fst-italic mb-4 lh-lg text-white-50">
                "Điểm mạnh của hệ thống là không chỉ ghi chú, mà còn có luồng cộng tác và mạng xã hội nội bộ để trình bày thực tế."
              </p>
              <div class="d-flex align-items-center gap-3">
                <div class="avatar-initial text-white fw-bold">SL</div>
                <div>
                  <h6 class="fw-bold mb-0 text-white">Luồng thời gian thực</h6>
                  <p class="text-muted mb-0 text-uppercase" style="font-size: 10px; letter-spacing: 0.1em;">SignalR + Redis + thông báo</p>
                </div>
              </div>
            </div>
          </div>
          <div class="col-12 col-md-6 ps-md-5">
            <h2 class="fw-black mb-4 text-white" style="font-size: 3rem; letter-spacing: -0.04em; line-height: 1.1;">Xây dựng để trình bày như một sản phẩm hoàn chỉnh.</h2>
            <p class="text-muted fs-5 mb-4 lh-lg">Block Paged được thiết kế như một hệ thống quản lý tri thức và cộng tác dành cho nhóm nhỏ, sinh viên, lập trình viên và các dự án cần quản lý nội dung lẫn công việc.</p>
            <div class="d-flex gap-4 opacity-50">
              <span class="fw-black fs-4 text-white" style="letter-spacing: -0.05em;">KIỂU NOTION</span>
              <span class="fw-black fs-4 text-white" style="letter-spacing: -0.05em;">THỜI GIAN THỰC</span>
              <span class="fw-black fs-4 text-white" style="letter-spacing: -0.05em;">AI CÔNG VIỆC</span>
            </div>
          </div>
        </div>
      </section>



      <section class="container-xl px-4 mb-5 pb-5">
        <div class="cta-section position-relative p-5 p-md-5 rounded-5 text-center overflow-hidden">
          <div class="cta-blob cta-blob-1"></div>
          <div class="cta-blob cta-blob-2"></div>
          <div class="position-relative" style="z-index: 1;">
            <h2 class="fw-bold mb-4 text-white" style="font-size: clamp(2rem, 5vw, 3.5rem); letter-spacing: -0.04em;">Sẵn sàng vào không gian làm việc?</h2>
            <p class="text-muted fs-5 mb-5 mx-auto" style="max-width: 500px;">Đăng nhập để tạo trang, quản lý công việc, nhắn tin và trình bày thời gian thực ngay.</p>
            <button
              class="btn btn-cta px-5 py-3 rounded-4 fw-black fs-5"
              type="button"
              :disabled="isNavigating"
              :aria-busy="isNavigating"
              @click="goToAuthEntry"
            >
              Bắt đầu ngay
            </button>
          </div>
        </div>
      </section>
    </main>

    <footer class="site-footer">
      <div class="container-xl px-4 py-5 d-flex flex-column flex-md-row justify-content-between align-items-center gap-4">
        <span class="text-muted" style="font-size: 0.6875rem; text-transform: uppercase; letter-spacing: 0.05em;">© 2026 Block Paged - Ngô Trần Nguyên Quân.</span>
        <div class="d-flex gap-4 align-items-center">
          <a href="#" class="footer-link">Tài liệu</a>
          <a href="#" class="footer-link">GitHub</a>
          <a href="#" class="footer-link">Liên hệ</a>
          <a href="#" class="footer-link">Twitter</a>
          <a href="#" class="footer-link">LinkedIn</a>
        </div>
      </div>
    </footer>
  </div>
</template>

<script setup lang="ts">
import { onMounted, onUnmounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { isAuthenticated } from '@/modules/auth/utils/auth-token.util'

const router = useRouter()
const particleCanvas = ref<HTMLCanvasElement | null>(null)
const isNavigating = ref(false)

function closeMobileNavbar() {
  const nav = document.getElementById('mainNav')

  if (!nav) return

  nav.classList.remove('show')

  const toggler = document.querySelector<HTMLButtonElement>(
    '[data-bs-target="#mainNav"]'
  )

  toggler?.setAttribute('aria-expanded', 'false')
}

function scrollToTop() {
  closeMobileNavbar()

  window.scrollTo({
    top: 0,
    behavior: 'smooth',
  })
}

function scrollToSection(sectionId: string) {
  closeMobileNavbar()

  const section = document.getElementById(sectionId)

  section?.scrollIntoView({
    behavior: 'smooth',
    block: 'start',
  })
}

async function goToAuthEntry() {
  if (isNavigating.value) return

  isNavigating.value = true
  closeMobileNavbar()

  try {
    if (isAuthenticated()) {
      await router.replace({ name: 'app' })
      return
    }

    await router.push({
      name: 'login',
      query: {
        redirect: '/app',
      },
    })
  } catch (error) {
    console.error('Landing navigation failed:', error)
  } finally {
    isNavigating.value = false
  }
}

onMounted(() => {
  const canvas = particleCanvas.value
  if (!canvas) return

  const canvasContext = canvas.getContext('2d')
  if (!canvasContext) return

  const ctx: CanvasRenderingContext2D = canvasContext

  class Particle {
    x: number
    y: number
    size: number
    speedX: number
    speedY: number
    life: number

    constructor(x: number, y: number) {
      this.x = x
      this.y = y
      this.size = Math.random() * 3 + 1
      this.speedX = (Math.random() - 0.5) * 1.5
      this.speedY = (Math.random() - 0.5) * 1.5
      this.life = 1
    }

    update() {
      this.x += this.speedX
      this.y += this.speedY

      if (this.life > 0) {
        this.life -= 0.02
      }
    }

    draw() {
      ctx.fillStyle = `rgba(255, 255, 255, ${this.life})`
      ctx.beginPath()
      ctx.arc(this.x, this.y, this.size, 0, Math.PI * 2)
      ctx.fill()
    }
  }

  let particles: Particle[] = []
  let animationFrame = 0

  const resize = () => {
    canvas.width = window.innerWidth
    canvas.height = window.innerHeight
  }

  const handleMouseMove = (event: MouseEvent) => {
    for (let i = 0; i < 2; i += 1) {
      particles.push(new Particle(event.clientX, event.clientY))
    }
  }

  const animate = () => {
    ctx.clearRect(0, 0, canvas.width, canvas.height)

    for (let i = 0; i < particles.length; i += 1) {
      const particle = particles[i]

      if (!particle) continue

      particle.update()
      particle.draw()

      if (particle.life <= 0) {
        particles.splice(i, 1)
        i -= 1
      }
    }

    animationFrame = requestAnimationFrame(animate)
  }

  window.addEventListener('resize', resize)
  window.addEventListener('mousemove', handleMouseMove)

  resize()
  animate()

  onUnmounted(() => {
    window.removeEventListener('resize', resize)
    window.removeEventListener('mousemove', handleMouseMove)
    cancelAnimationFrame(animationFrame)
    particles = []
  })
})
</script>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700;800;900&display=swap');
@import url('https://fonts.googleapis.com/css2?family=Material+Symbols+Outlined:wght,FILL@100..700,0..1&display=swap');
.particle-canvas {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  pointer-events: none; /* Quan trọng: Để không chặn click vào link/nút */
  z-index: 9999; /* Nằm trên cùng để thấy hiệu ứng rõ nhất */
}
/* ─── Monochrome Variable Theme ─── */
:root {
  --color-bg: #000000;
  --color-surface: #0a0a0a;
  --color-surface-2: #111111;
  --color-surface-3: #1a1a1a;
  --color-surface-4: #222222;
  --color-primary: #ffffff; /* White as primary accent */
  --color-primary-dim: #cccccc;
  --color-primary-container: #333333;
  --color-outline: #262626;
  --color-muted: #a8a8a8; /* Tăng độ sáng để mọi thứ rõ ràng hơn */
  --color-text: #ffffff;
}

/* ─── Global ─── */
.landing-page {
  background-color: #000000;
  color: #ffffff;
  font-family: 'Inter', sans-serif;
  min-height: 100vh;
}

.material-symbols-outlined {
  font-variation-settings: 'FILL' 0, 'wght' 400, 'GRAD' 0, 'opsz' 24;
}

/* ─── Colors ─── */
.text-primary-accent { color: #ffffff !important; }
.text-muted { color: #a8a8a8 !important; } /* Tăng độ sáng muted text cho dễ đọc */

/* Thêm rule cho text-white-50 để quote rõ ràng hơn */
.text-white-50 {
  color: rgba(255, 255, 255, 0.85) !important;
}

/* ─── Navbar ─── */
.navbar {
  background: rgba(0, 0, 0, 0.8) !important;
  backdrop-filter: blur(24px);
  border-bottom: 1px solid #1a1a1a;
}

.navbar-brand {
  color: #ffffff !important;
  font-size: 1.125rem;
  letter-spacing: -0.04em;
}

.nav-link {
  color: #737373 !important;
  font-size: 0.9rem;
  font-weight: 500;
  transition: color 0.2s;
}

.nav-link:hover, .nav-link.nav-active {
  color: #ffffff !important;
}

.btn-primary-white {
  background: #ffffff;
  color: #000000 !important;
  border: none;
  font-size: 0.875rem;
  transition: opacity 0.2s;
}

.btn-primary-white:hover { opacity: 0.9; }

.btn-primary-white:disabled,
.btn-primary-solid:disabled,
.btn-cta:disabled {
  cursor: not-allowed;
  opacity: 0.72;
}

.btn-ghost:disabled {
  cursor: not-allowed;
  opacity: 0.5;
}

/* ─── Hero ─── */
.hero-glow {
  position: absolute;
  top: 0;
  left: 50%;
  transform: translateX(-50%);
  width: 100%;
  height: 600px;
  background: radial-gradient(circle at center, rgba(255, 255, 255, 0.03) 0%, transparent 70%);
  filter: blur(40px);
  z-index: 0;
}

.badge-pill {
  background: #111111;
  border: 1px solid #262626;
}

.pulse-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #ffffff;
  animation: pulse 2s infinite;
}

@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.4; }
}

.hero-title {
  font-size: clamp(3rem, 8vw, 5.5rem);
  letter-spacing: -0.04em;
  line-height: 1.1;
  background: linear-gradient(180deg, #ffffff 40%, #525252 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
}

.btn-primary-solid {
  background: #ffffff;
  color: #000000;
  border: none;
}

.btn-primary-solid:hover { background: #e5e5e5; color: #000; }

.btn-ghost {
  background: transparent;
  color: #ffffff;
  border: 1px solid #262626;
}

.btn-ghost:hover { background: #111111; }

.hero-stats { position: relative; z-index: 1; }
.hero-stat {
  min-width: 150px;
  background: rgba(255,255,255,0.035);
  border: 1px solid #1f1f1f;
  text-align: left;
}
.hero-stat strong {
  display: block;
  color: #fff;
  font-size: 0.82rem;
  letter-spacing: -0.02em;
}
.hero-stat span {
  display: block;
  color: #8f8f8f;
  font-size: 0.7rem;
  text-transform: uppercase;
  letter-spacing: 0.06em;
}

/* ─── Demo Panel ─── */
.demo-wrapper { padding: 2px; }
.demo-glow-bg {
  position: absolute;
  inset: -4px;
  background: linear-gradient(135deg, rgba(255,255,255,0.1), transparent);
  border-radius: 2rem;
  filter: blur(20px);
  opacity: 0.3;
}

.demo-panel {
  background: #0a0a0a;
  border: 1px solid #1a1a1a;
}

.demo-titlebar { background: #111111; border-bottom: 1px solid #1a1a1a; }
.demo-url { background: #000000; color: #737373; }
.dot-gray { width: 12px; height: 12px; background: #262626; margin-right: 4px; }

.demo-sidebar { border-right: 1px solid #1a1a1a; }
.skeleton-bar { height: 8px; background: #1a1a1a; border-radius: 4px; }
.skeleton-row { height: 48px; background: #111111; } /* Tăng độ sáng để rõ hơn */

.avatar-circle { width: 32px; height: 32px; border-radius: 50%; border: 2px solid #0a0a0a; }
.bg-gray-700 { background: #404040 !important; }
.bg-gray-600 { background: #525252 !important; }
.bg-gray-500 { background: #737373 !important; }

.feature-card { background: #0d0d0d; border: 1px solid #1a1a1a; }
.velocity-chart { height: 112px; background: #111111; } /* Tăng độ sáng */
.bar-item { flex: 1; background: #1a1a1a; border-radius: 4px 4px 0 0; }
.bar-item.bar-active { background: #ffffff; }

/* ─── Bento Grid ─── */
.bento-card {
  background: #0a0a0a;
  border: 1px solid #1a1a1a;
  transition: border-color 0.3s;
}

.bento-card:hover { border-color: #404040; }
.collab-bar { background: #000000; border: 1px solid #1a1a1a; }
.collab-chip { padding: 0.375rem 0.75rem; border-radius: 0.5rem; font-size: 12px; background: #1a1a1a; color: #737373; }
.collab-chip.typing { background: rgba(255, 255, 255, 0.1); color: #ffffff; }

.command-panel {
  background: #111111;
  border: 1px solid #262626;
  box-shadow: 0 20px 60px rgba(0,0,0,0.8);
}
.how-section { 
  background: #0a0a0a;
  border-top: 1px solid #1a1a1a; 
  border-bottom: 1px solid #1a1a1a; 
}
.how-line { 
  position: absolute; 
  top: 40px; 
  left: 0%;
  right: 25%; 
  height: 1px; 
  background: #262626;
}
.step-badge {
  width: 64px; height: 64px; border-radius: 1rem;
  background: #0a0a0a; border: 1px solid #262626;
  display: flex; align-items: center; justify-content: center;
}

/* ─── Testimonials ─── */
.testimonial-card { background: #0a0a0a; border: 1px solid #1a1a1a; }
.quote-icon { font-size: 2.5rem; opacity: 0.05; position: absolute; top: 1.5rem; right: 2rem; color: #ffffff; }
.avatar-initial { width: 48px; height: 48px; border-radius: 50%; background: #1a1a1a; display: flex; align-items: center; justify-content: center; }


/* ─── CTA ─── */
.cta-section {
  background: #0a0a0a;
  border: 1px solid #1a1a1a;
  padding: 6rem 4rem !important;
}

.cta-blob-1 { width: 400px; height: 400px; top: -100px; right: -100px; background: rgba(255, 255, 255, 0.03); filter: blur(80px); position: absolute; }
.cta-blob-2 { width: 400px; height: 400px; bottom: -100px; left: -100px; background: rgba(255, 255, 255, 0.02); filter: blur(80px); position: absolute; }

.btn-cta { background: #ffffff; color: #000000; border: none; transition: transform 0.2s; }
.btn-cta:hover { transform: scale(1.04); background: #f2f2f2; }

/* ─── Footer ─── */
.site-footer { background: #000000; border-top: 1px solid #1a1a1a; }
.footer-link { color: #525252; text-decoration: none; transition: color 0.2s; font-size: 0.75rem; text-transform: uppercase; }
.footer-link:hover { color: #ffffff; }
@media (max-width: 768px) {
  .hero-title { font-size: 3rem; }
}
</style>
