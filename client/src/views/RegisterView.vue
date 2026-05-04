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
                <span>Back to login</span>
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

                <h1 class="register-title mb-2">Create an account</h1>
                <p class="register-subtitle mb-0">
                  Start curating your digital workspace today.
                </p>
              </div>

              <div class="register-card">
                <form @submit.prevent="handleRegister" class="row g-3">
                  <div class="col-12">
                    <label class="form-label-custom">Full Name</label>
                    <div class="input-shell">
                      <span class="material-symbols-outlined input-icon">badge</span>
                      <input
                        v-model="form.fullName"
                        type="text"
                        class="form-control form-control-custom"
                        placeholder="Jan van Eyck"
                        required
                      />
                    </div>
                  </div>

                  <div class="col-12">
                    <label class="form-label-custom">Username</label>
                    <div class="input-shell">
                      <span class="material-symbols-outlined input-icon">person</span>
                      <input
                        v-model="form.userName"
                        type="text"
                        class="form-control form-control-custom"
                        placeholder="jan_eyck"
                        required
                      />
                    </div>
                  </div>

                  <div class="col-12">
                    <label class="form-label-custom">Email</label>
                    <div class="input-shell">
                      <span class="material-symbols-outlined input-icon">mail</span>
                      <input
                        v-model="form.email"
                        type="email"
                        class="form-control form-control-custom"
                        placeholder="jan@atheneum.io"
                        required
                      />
                    </div>
                  </div>

                  <div class="col-12">
                    <label class="form-label-custom">Password</label>
                    <div class="input-shell">
                      <span class="material-symbols-outlined input-icon">lock</span>
                      <input
                        v-model="form.password"
                        :type="showPassword ? 'text' : 'password'"
                        class="form-control form-control-custom password-input"
                        placeholder="••••••••"
                        required
                      />
                      <button
                        type="button"
                        class="password-toggle"
                        @click="showPassword = !showPassword"
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
                      :disabled="isSubmitting"
                    >
                      <span v-if="!isSubmitting">Create Account</span>
                      <span
                        v-else
                        class="spinner-border spinner-border-sm"
                        role="status"
                        aria-hidden="true"
                      ></span>
                    </button>
                  </div>

                  <div v-if="errorMessage" class="col-12">
                    <div class="error-alert d-flex align-items-center gap-2">
                      <span class="material-symbols-outlined error-icon">error</span>
                      <span>{{ errorMessage }}</span>
                    </div>
                  </div>
                </form>

                <div class="register-footer text-center">
                  <p class="mb-0">
                    Already have an account?
                    <router-link to="/login" class="signin-link ms-1">
                      Sign in
                    </router-link>
                  </p>
                </div>
              </div>
            </div>

            <footer class="register-meta mt-4">
              <div class="d-flex flex-wrap align-items-center gap-3">
                <span>© 2026 Block Paged</span>
                <span class="meta-dot"></span>
                <a href="#">Privacy</a>
                <a href="#">Terms</a>
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
              <div class="cursor-label cursor-label-purple">Elena</div>
            </div>

            <div class="cursor-tag cursor-bottom">
              <span class="material-symbols-outlined filled">near_me</span>
              <div class="cursor-label cursor-label-dark">Marcus</div>
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
import { reactive, ref } from 'vue';
import { useRouter } from 'vue-router';
import { AuthenticateAPI } from '@/api/authenticate.api';

const router = useRouter();

const form = reactive({
  fullName: '',
  userName: '',
  email: '',
  password: ''
});

const showPassword = ref(false);
const isSubmitting = ref(false);
const errorMessage = ref('');

const handleRegister = async () => {
  try {
    isSubmitting.value = true;
    errorMessage.value = '';

    const response: any = await AuthenticateAPI.register(form);
    
    if (response.isSuccess) {
      router.push('/login');
    } else {
      errorMessage.value = response.message || 'Đăng ký thất bại.';
    }
  } catch (error: any) {
    console.error('Register Error:', error);
    errorMessage.value = error?.message || 'Đăng ký thất bại. Vui lòng kiểm tra lại thông tin.';
  } finally {
    isSubmitting.value = false;
  }
};
</script>

<style scoped>

</style>