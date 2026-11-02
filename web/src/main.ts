import { createApp } from 'vue'
import { createPinia } from 'pinia'
import router from './router'
import App from './App.vue'
import '@fontsource-variable/plus-jakarta-sans'
import './assets/main.css'
import PageLoader from './components/ui/PageLoader.vue'

const app = createApp(App)

// Used on every list page while data loads; registered once so views don't each import it.
app.component('PageLoader', PageLoader)

app.use(createPinia())
app.use(router)

// Wait for the initial navigation to resolve before mounting — otherwise App.vue's
// `route.meta.public` check can briefly evaluate against the router's unresolved initial
// state, flash-mounting AppShell/Sidebar (which unconditionally fetches farms) even on a
// public, unauthenticated route. That 401s and triggers the API interceptor's full-page
// reload, which restarts the same race — an infinite reload loop on first load.
router.isReady().then(() => {
  app.mount('#app')
})
