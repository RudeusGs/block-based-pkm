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
              <label for="userName" class="form-label-custom">User Name</label>
              <div class="input-group-custom">
                <span class="material-symbols-outlined icon">person</span>
                <input v-model="form.userName" type="text" id="userName" class="form-control-custom" placeholder="Your user name" required />
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
                <button type="button" class="btn-toggle-pass" @click="showPassword = !showPassword">
                  <span class="material-symbols-outlined">{{ showPassword ? 'visibility_off' : 'visibility' }}</span>
                </button>
              </div>
            </div>

            <button type="submit" class="btn btn-black w-100 py-3 fw-bold rounded-3 mb-4" :disabled="isSubmitting">
              <span v-if="!isSubmitting">Sign In</span>
              <span v-else class="spinner-border spinner-border-sm"></span>
            </button>
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
            New here? <router-link to="/register" class="text-black fw-bold text-decoration-none border-bottom border-dark ms-1">Create account</router-link>
          </p>
        </div>
      </div>
    </main>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue';
const showPassword = ref(false);
const isSubmitting = ref(false);
const form = reactive({ userName: '', password: '' });

const handleLogin = async () => {
  isSubmitting.value = true;
  await new Promise(resolve => setTimeout(resolve, 1000));
  isSubmitting.value = false;
};
</script>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700;800;900&display=swap');
@import url('https://fonts.googleapis.com/css2?family=Material+Symbols+Outlined:wght,FILL@100..700,0..1&display=swap');
.btn-back {
  position: absolute;
  top: 2rem;
  left: 2rem;
  z-index: 10;
  display: flex;
  align-items: center;
  text-decoration: none;
  color: #666;
  padding: 8px 16px;
  border-radius: 50px;
  background: rgba(255, 255, 255, 0.5);
  backdrop-filter: blur(10px);
  border: 1px solid rgba(0, 0, 0, 0.05);
  transition: all 0.3s ease;
}

.btn-back:hover {
  color: black;
  background: white;
  transform: translateX(-5px);
  box-shadow: 0 10px 20px rgba(0, 0, 0, 0.05);
}
.auth-wrapper {
  background-color: #f8f9fb;
  font-family: 'Inter', sans-serif;
  position: relative;
}

.login-container {
  max-width: 500px;
}
.bg-decorations {
  position: absolute;
  inset: 0;
  z-index: 0;
  pointer-events: none;
}
.blob { position: absolute; border-radius: 50%; filter: blur(100px); opacity: 0.25; }
.blob-1 { width: 500px; height: 500px; background: #ddd; top: -150px; right: -150px; }
.blob-2 { width: 400px; height: 400px; background: #ccc; bottom: -100px; left: -100px; }
.floating-shape {
  position: absolute; width: 280px; height: 280px;
  background: white; border-radius: 30px; right: 10%; top: 15%; 
  transform: rotate(12deg); box-shadow: 0 40px 80px rgba(0,0,0,0.03);
}
.brand-logo { width: 52px; height: 52px; background: black; color: white; border-radius: 12px; }
.brand-name { font-size: 1.5rem; letter-spacing: -0.05em; font-weight: 900; }

.card { 
  border-radius: 24px; 
  background: rgba(255, 255, 255, 0.8); 
  backdrop-filter: blur(20px);
}
.shadow-premium { 
  box-shadow: 0 40px 100px -20px rgba(0, 0, 0, 0.05), 0 20px 40px -15px rgba(0,0,0,0.03); 
}

/* Form Elements */
.form-label-custom { font-size: 0.65rem; font-weight: 800; text-transform: uppercase; letter-spacing: 0.1em; color: #666; margin-bottom: 6px; display: block; }
.input-group-custom { position: relative; }
.input-group-custom .icon { position: absolute; left: 14px; top: 50%; transform: translateY(-50%); color: #bbb; font-size: 18px; }
.form-control-custom {
  width: 100%; padding: 12px 14px 12px 42px;
  background: #f1f2f4; border: 1px solid transparent; border-radius: 12px;
  font-size: 0.85rem; transition: all 0.2s;
}
.form-control-custom:focus { outline: none; background: white; border-color: black; }
.btn-toggle-pass { position: absolute; right: 10px; top: 50%; transform: translateY(-50%); background: none; border: none; color: #bbb; }

/* Buttons */
.btn-black { background: black; color: white; border: none; transition: transform 0.2s; }
.btn-black:hover { background: #222; transform: scale(1.01); color: white; }
.btn-outline-custom { background: white; border: 1px solid #eee; border-radius: 10px; }

/* Divider */
.divider { display: flex; align-items: center; color: #ccc; font-size: 0.65rem; text-transform: uppercase; font-weight: 700; }
.divider::before, .divider::after { content: ''; flex: 1; border-bottom: 1px solid #eee; }
.divider span { padding: 0 12px; }

.bg-light-soft { background-color: #fafafa; }
.text-link { color: black; text-decoration: none; font-weight: 700; }

/* Animations */
.animate-fade-up { animation: fadeUp 0.7s ease-out forwards; }
.animate-fade-down { animation: fadeDown 0.7s ease-out forwards; }
@keyframes fadeUp { from { opacity: 0; transform: translateY(30px); } to { opacity: 1; transform: translateY(0); } }
@keyframes fadeDown { from { opacity: 0; transform: translateY(-30px); } to { opacity: 1; transform: translateY(0); } }
@media (max-width: 576px) {
  .btn-back {
    top: 1.5rem;
    left: 1.5rem;
    padding: 8px;
  }
  .btn-back span:not(.material-symbols-outlined) {
    display: none;
  }
}
</style>