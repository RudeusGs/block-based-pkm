<template>
  <div class="auth-wrapper vh-100 overflow-hidden d-flex align-items-center justify-content-center">
    <router-link to="/" class="btn-back animate-fade-down">
      <span class="material-symbols-outlined">arrow_back</span>
      <span class="ms-2 fw-semibold small">Back to home</span>
    </router-link>

    <div class="bg-decorations">
      <div class="blob blob-1"></div>
      <div class="blob blob-2"></div>
      <div class="floating-shape d-none d-lg-block"></div>
    </div>

    <main class="login-container w-100 z-1 px-3">
      
      <div class="text-center mb-4 animate-fade-down">
        <div class="brand-logo mx-auto mb-3 d-flex align-items-center justify-content-center">
          <span class="material-symbols-outlined fs-2">architecture</span>
        </div>
        <h1 class="brand-name fw-black mb-0">Block Paged</h1>
      </div>

      <div class="card border-0 shadow-premium animate-fade-up">
        <div class="card-body p-4 p-md-5">
          <div class="mb-4 text-center">
            <h4 class="fw-bold mb-1">Welcome back</h4>
            <p class="text-muted small">Access your workspace</p>
          </div>

          <form @submit.prevent="handleLogin">
            <div class="mb-3">
              <label for="email" class="form-label-custom">User Name</label>
              <div class="input-group-custom">
                <span class="material-symbols-outlined icon">person</span>
                <input v-model="form.userName" type="text" id="email" class="form-control-custom" placeholder="Your user name" required />
              </div>
            </div>

            <div class="mb-3">
              <div class="d-flex justify-content-between">
                <label for="password" class="form-label-custom">Password</label>
                <a href="#" class="text-link small">Forgot?</a>
              </div>
              <div class="input-group-custom">
                <span class="material-symbols-outlined icon">lock</span>
                <input 
                  v-model="form.password" 
                  :type="showPassword ? 'text' : 'password'" 
                  id="password" class="form-control-custom" placeholder="••••••••" required 
                />
                <button type="button" class="btn-toggle-pass" @click="togglePassword">
                  <span class="material-symbols-outlined">{{ showPassword ? 'visibility_off' : 'visibility' }}</span>
                </button>
              </div>
            </div>
            <button
              type="submit"
              class="btn btn-black w-100 py-3 fw-bold rounded-3 mb-4"
              :disabled="isSubmitting"
            >
              <span v-if="!isSubmitting">Sign In</span>
              <span v-else class="spinner-border spinner-border-sm"></span>
            </button>

            <!-- Error message -->
            <div v-if="errorMessage" class="error-alert d-flex align-items-center gap-2 mb-2">
              <span class="material-symbols-outlined" style="font-size:18px">error</span>
              <span>{{ errorMessage }}</span>
            </div>
          </form>

          <div class="divider mb-4"><span>Or</span></div>

          <div class="row g-2">
            <div class="col-6">
              <button class="btn btn-outline-custom w-100 d-flex align-items-center justify-content-center gap-2 py-2">
                <img src="https://www.google.com/favicon.ico" width="14" height="14" />
                <span class="fw-semibold small">Google</span>
              </button>
            </div>
            <div class="col-6">
              <button class="btn btn-outline-custom w-100 d-flex align-items-center justify-content-center gap-2 py-2">
                <span class="material-symbols-outlined fs-5">terminal</span>
                <span class="fw-semibold small">GitHub</span>
              </button>
            </div>
          </div>
        </div>
        
        <div class="card-footer border-0 bg-light-soft py-3 text-center">
          <p class="mb-0 small text-muted">
            New here? <a href="/register" class="text-black fw-bold text-decoration-none border-bottom border-dark ms-1">Create account</a>
          </p>
        </div>
      </div>
    </main>
  </div>
</template>

<script setup lang="ts">
import { useLogin } from './composables/useLogin';

const { form, showPassword, isSubmitting, errorMessage, handleLogin, togglePassword } = useLogin();
</script>

<style scoped>
@import './css/LoginView.css';
</style>