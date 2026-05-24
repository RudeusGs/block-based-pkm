<template>
  <div class="register-page">
    <div class="container-fluid h-100">
      <div class="row g-0 min-vh-100">
        <!-- Left: Form -->
        <section class="col-12 col-md-5 col-lg-4 register-left d-flex flex-column">
          <div class="register-left-inner d-flex flex-column h-100">
            <div class="mb-4 mb-lg-5">
              <router-link to="/login" class="back-link">
                <span class="material-symbols-outlined">arrow_back</span>
                <span>Về đăng nhập</span>
              </router-link>
            </div>

            <div class="register-form-wrap my-auto mx-auto w-100">
              <div class="brand-block">
                <div class="d-flex align-items-center gap-2 mb-2">
                  <div class="brand-icon d-flex align-items-center justify-content-center">
                    <span class="material-symbols-outlined filled">widgets</span>
                  </div>
                  <span class="brand-text">Block Paged</span>
                </div>

                <h1 class="register-title mb-2">Tạo tài khoản</h1>
                <p class="register-subtitle mb-0">
                  Bắt đầu xây dựng không gian làm việc của bạn hôm nay.
                </p>
              </div>

              <div class="register-card">
                <form @submit.prevent="handleRegister" class="row g-3">
                  <div class="col-12">
                    <label for="fullName" class="form-label-custom">
                      Họ và tên
                    </label>

                    <div class="input-shell">
                      <span class="material-symbols-outlined input-icon">badge</span>

                      <input
                        v-model.trim="form.fullName"
                        type="text"
                        id="fullName"
                        class="form-control form-control-custom"
                        placeholder="Nguyễn An"
                        autocomplete="name"
                        :disabled="isSubmitting"
                        required
                      />
                    </div>
                  </div>

                  <div class="col-12">
                    <label for="userName" class="form-label-custom">
                      Tên đăng nhập
                    </label>

                    <div class="input-shell">
                      <span class="material-symbols-outlined input-icon">person</span>

                      <input
                        v-model.trim="form.userName"
                        type="text"
                        id="userName"
                        class="form-control form-control-custom"
                        placeholder="nguyen_an"
                        autocomplete="username"
                        :disabled="isSubmitting"
                        required
                      />
                    </div>
                  </div>

                  <div class="col-12">
                    <label for="email" class="form-label-custom">
                      Email
                    </label>

                    <div class="input-shell">
                      <span class="material-symbols-outlined input-icon">mail</span>

                      <input
                        v-model.trim="form.email"
                        type="email"
                        id="email"
                        class="form-control form-control-custom"
                        placeholder="an@example.com"
                        autocomplete="email"
                        :disabled="isSubmitting"
                        required
                      />
                    </div>
                  </div>

                  <div class="col-12">
                    <label for="password" class="form-label-custom">
                      Mật khẩu
                    </label>

                    <div class="input-shell">
                      <span class="material-symbols-outlined input-icon">lock</span>

                      <input
                        v-model="form.password"
                        :type="showPassword ? 'text' : 'password'"
                        id="password"
                        class="form-control form-control-custom password-input"
                        placeholder="••••••••"
                        autocomplete="new-password"
                        :disabled="isSubmitting"
                        required
                      />

                      <button
                        type="button"
                        class="password-toggle"
                        @click="togglePassword"
                        :disabled="isSubmitting"
                        aria-label="Ẩn hoặc hiện mật khẩu"
                      >
                        <span class="material-symbols-outlined">
                          {{ showPassword ? 'visibility_off' : 'visibility' }}
                        </span>
                      </button>
                    </div>
                  </div>

                  <div class="col-12 pt-2">
                    <button
                      type="submit"
                      class="btn btn-register w-100"
                      :disabled="isSubmitting || !canSubmit"
                    >
                      <span v-if="!isSubmitting">Tạo tài khoản</span>

                      <span v-else class="d-inline-flex align-items-center gap-2">
                        <span
                          class="spinner-border spinner-border-sm"
                          role="status"
                          aria-hidden="true"
                        ></span>
                        Đang tạo tài khoản...
                      </span>
                    </button>
                  </div>
                </form>

                <div class="register-footer text-center">
                  <p class="mb-0">
                    Bạn đã có tài khoản?

                    <router-link to="/login" class="signin-link ms-1">
                      Đăng nhập
                    </router-link>
                  </p>
                </div>
              </div>
            </div>

            <footer class="register-meta mt-4">
              <div class="d-flex flex-wrap align-items-center gap-3">
                <span>© 2024 Block Paged</span>
                <span class="meta-dot"></span>
                <a href="#">Quyền riêng tư</a>
                <a href="#">Điều khoản</a>
              </div>
            </footer>
          </div>
        </section>

        <!-- Right: Preview -->
        <section class="col-md-7 col-lg-8 register-right d-none d-md-flex align-items-center justify-content-center">
          <div class="preview-stage position-relative w-100">
            <div class="preview-grid"></div>

            <div class="main-canvas">
              <div class="canvas-top d-flex align-items-center justify-content-between">
                <div class="d-flex align-items-center gap-2">
                  <span class="window-dot"></span>
                  <span class="window-dot"></span>
                  <span class="window-dot"></span>
                </div>
                <div class="canvas-pill"></div>
              </div>

              <div class="canvas-content">
                <div class="canvas-title"></div>

                <div class="line-group mb-4">
                  <div class="text-line w-100"></div>
                  <div class="text-line w-75"></div>
                  <div class="text-line w-50"></div>
                </div>

                <div class="row g-3">
                  <div class="col-6">
                    <div class="media-card media-image"></div>
                  </div>

                  <div class="col-6">
                    <div class="media-card media-note">
                      <div class="mini-line short"></div>
                      <div class="mini-line"></div>
                      <div class="mini-line"></div>
                      <div class="mini-line medium"></div>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <div class="floating-sidebar">
              <div class="d-flex align-items-center gap-2 mb-3">
                <span class="material-symbols-outlined sidebar-icon">folder_open</span>
                <div class="sidebar-line sidebar-line-lg"></div>
              </div>

              <div class="sidebar-stack">
                <div class="d-flex align-items-center justify-content-between">
                  <div class="sidebar-line sidebar-line-md"></div>
                  <span class="material-symbols-outlined sidebar-more">more_horiz</span>
                </div>
                <div class="sidebar-line sidebar-line-sm muted"></div>
              </div>
            </div>

            <div class="cursor-tag cursor-top">
              <span class="material-symbols-outlined filled">near_me</span>
              <div class="cursor-label cursor-label-purple">Linh</div>
            </div>

            <div class="cursor-tag cursor-bottom">
              <span class="material-symbols-outlined filled">near_me</span>
              <div class="cursor-label cursor-label-dark">Minh</div>
            </div>

            <div class="floating-toolbar">
              <span class="material-symbols-outlined">text_fields</span>
              <span class="material-symbols-outlined">image</span>
              <span class="material-symbols-outlined">table_chart</span>
              <span class="toolbar-divider"></span>
              <span class="material-symbols-outlined">add_circle</span>
            </div>

            <div class="ambient ambient-1"></div>
            <div class="ambient ambient-2"></div>
          </div>
        </section>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useRegister } from '@/modules/auth/composables/useRegister'

const {
  form,
  showPassword,
  isSubmitting,
  canSubmit,
  handleRegister,
  togglePassword,
} = useRegister()
</script>

<style scoped>
@import './css/RegisterView.css';
</style>
