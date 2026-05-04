import { createApp } from 'vue'
import { createPinia } from 'pinia'
import 'bootstrap/dist/css/bootstrap.min.css'
import 'bootstrap-icons/font/bootstrap-icons.css'
import 'bootstrap'
import App from './App.vue'
import router from './router'
import api from './api/base.api'
const app = createApp(App)

app.use(createPinia())
app.use(router)

app.mount('#app')
