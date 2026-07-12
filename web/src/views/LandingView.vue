<template>
  <div class="landing">

    <!-- ── Animated background ── -->
    <div class="land-bg" aria-hidden="true">
      <svg class="bg-grid" width="100%" height="100%" xmlns="http://www.w3.org/2000/svg">
        <defs>
          <pattern id="land-grid" width="48" height="48" patternUnits="userSpaceOnUse">
            <path d="M 48 0 L 0 0 0 48" fill="none" stroke="rgba(61,170,106,0.07)" stroke-width="0.5"/>
          </pattern>
        </defs>
        <rect width="100%" height="100%" fill="url(#land-grid)"/>
      </svg>
      <div class="bg-orb bg-orb-1" />
      <div class="bg-orb bg-orb-2" />
      <div class="bg-orb bg-orb-3" />
    </div>

    <!-- ── Topbar ── -->
    <nav class="land-nav">
      <div class="land-logo">
        <svg width="22" height="22" viewBox="0 0 72 72" fill="none">
          <rect width="72" height="72" rx="16" fill="#1A4A32"/>
          <line x1="24" y1="14" x2="24" y2="58" stroke="#2D7A4F" stroke-width="1.5" stroke-dasharray="2 3"/>
          <line x1="48" y1="14" x2="48" y2="58" stroke="#2D7A4F" stroke-width="1.5" stroke-dasharray="2 3"/>
          <line x1="14" y1="24" x2="58" y2="24" stroke="#2D7A4F" stroke-width="1.5" stroke-dasharray="2 3"/>
          <line x1="14" y1="48" x2="58" y2="48" stroke="#2D7A4F" stroke-width="1.5" stroke-dasharray="2 3"/>
          <circle cx="36" cy="36" r="7" fill="#E8A838"/>
          <circle cx="36" cy="36" r="3.5" fill="#0D2B1F"/>
          <line x1="24" y1="24" x2="36" y2="36" stroke="#3DAA6A" stroke-width="1.5"/>
          <line x1="48" y1="24" x2="36" y2="36" stroke="#3DAA6A" stroke-width="1.5"/>
          <line x1="24" y1="48" x2="36" y2="36" stroke="#3DAA6A" stroke-width="1.5"/>
          <line x1="48" y1="48" x2="36" y2="36" stroke="#3DAA6A" stroke-width="1.5"/>
        </svg>
        <span>Agro<span class="logo-accent">Mesh</span></span>
      </div>
      <a href="#auth" class="nav-signin-link" @click.prevent="scrollToAuth">Sign in</a>
    </nav>

    <!-- ── Main split layout ── -->
    <div class="land-main">

      <!-- Left: Hero -->
      <div class="hero-col">
        <div class="hero-badge" style="--i:0">
          <span class="badge-dot" />
          Built for Nigeria · AI-Powered
        </div>

        <h1 class="hero-h1" style="--i:1">
          The Farm OS that<br>
          <span class="hero-accent">pays for itself</span>
        </h1>

        <p class="hero-sub" style="--i:2">
          Track every naira spent on your farm. Auto-categorize bank
          transactions, log field activities, and get AI-powered scheduling
          with live 16-day weather data.
        </p>

        <div class="hero-stats" style="--i:3">
          <div class="h-stat">
            <div class="h-stat-n">₦2.3M</div>
            <div class="h-stat-l">tracked monthly</div>
          </div>
          <div class="h-stat-sep" />
          <div class="h-stat">
            <div class="h-stat-n">4 crops</div>
            <div class="h-stat-l">per section</div>
          </div>
          <div class="h-stat-sep" />
          <div class="h-stat">
            <div class="h-stat-n">16-day</div>
            <div class="h-stat-l">weather forecasts</div>
          </div>
        </div>

        <!-- Feature pills -->
        <div class="hero-pills" style="--i:4">
          <span v-for="(pill, i) in featurePills" :key="i" class="feat-pill" :style="`--pi: ${i}`">
            <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                 stroke-width="2" stroke-linecap="round" stroke-linejoin="round"
                 v-html="pill.icon" />
            {{ pill.label }}
          </span>
        </div>
      </div>

      <!-- Right: Auth card -->
      <div id="auth" class="auth-col" ref="authColRef">
        <div class="auth-card">
          <div class="auth-card-head">
            <div class="auth-greeting">{{ mode === 'login' ? 'Welcome back' : 'Get started today' }}</div>
            <div class="auth-sub">{{ mode === 'login' ? 'Sign in to your AgroMesh account' : 'Create your free AgroMesh account' }}</div>
          </div>

          <!-- Tabs -->
          <div class="auth-tabs" role="tablist">
            <button role="tab" :class="['auth-tab', { active: mode === 'login' }]"
                    :aria-selected="mode === 'login'" @click="mode = 'login'">
              Sign in
            </button>
            <button role="tab" :class="['auth-tab', { active: mode === 'register' }]"
                    :aria-selected="mode === 'register'" @click="mode = 'register'">
              Create account
            </button>
            <div class="tab-slider" :style="mode === 'login' ? 'transform:translateX(0)' : 'transform:translateX(100%)'" />
          </div>

          <!-- Error banner -->
          <Transition name="auth-err">
            <div v-if="authError" class="auth-error" role="alert">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                   stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
                <path d="M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z"/>
                <line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/>
              </svg>
              {{ authError }}
            </div>
          </Transition>

          <!-- Forms -->
          <Transition name="form-switch" mode="out-in">

            <!-- Login form -->
            <form v-if="mode === 'login'" key="login" class="auth-form" @submit.prevent="handleLogin" novalidate>
              <div class="field-group">
                <label class="field-label" for="login-email">Email address</label>
                <input id="login-email" v-model="loginForm.email" type="email" class="field-input"
                       placeholder="you@example.com" autocomplete="email" required />
              </div>
              <div class="field-group">
                <label class="field-label" for="login-password">Password</label>
                <div class="pw-wrap">
                  <input id="login-password" v-model="loginForm.password"
                         :type="showPw ? 'text' : 'password'" class="field-input"
                         placeholder="Enter your password" autocomplete="current-password" required />
                  <button type="button" class="pw-toggle" @click="showPw = !showPw" :aria-label="showPw ? 'Hide password' : 'Show password'">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                         stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
                      <path v-if="showPw" d="M17.94 17.94A10.07 10.07 0 0112 20c-7 0-11-8-11-8a18.45 18.45 0 015.06-5.94M9.9 4.24A9.12 9.12 0 0112 4c7 0 11 8 11 8a18.5 18.5 0 01-2.16 3.19m-6.72-1.07a3 3 0 11-4.24-4.24M1 1l22 22"/>
                      <path v-else d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle v-if="!showPw" cx="12" cy="12" r="3"/>
                    </svg>
                  </button>
                </div>
              </div>
              <button type="submit" class="auth-submit" :disabled="submitting">
                <span v-if="!submitting">Sign in</span>
                <span v-else class="submit-loading">
                  <span class="spinner" />
                  Signing in…
                </span>
              </button>
              <p class="auth-switch-hint">
                No account?
                <button type="button" class="link-btn" @click="mode = 'register'">Create one free</button>
              </p>
            </form>

            <!-- Register form -->
            <form v-else key="register" class="auth-form" @submit.prevent="handleRegister" novalidate>
              <div class="fields-row">
                <div class="field-group">
                  <label class="field-label" for="reg-name">Full name</label>
                  <input id="reg-name" v-model="regForm.fullName" type="text" class="field-input"
                         placeholder="Adebayo Okafor" autocomplete="name" required />
                </div>
                <div class="field-group">
                  <label class="field-label" for="reg-farm">Farm name</label>
                  <input id="reg-farm" v-model="regForm.farmName" type="text" class="field-input"
                         placeholder="Sunridge Farm" required />
                </div>
              </div>
              <div class="field-group">
                <label class="field-label" for="reg-email">Email address</label>
                <input id="reg-email" v-model="regForm.email" type="email" class="field-input"
                       placeholder="you@example.com" autocomplete="email" required />
              </div>
              <div class="hint-box">
                You'll set your farm's state/LGA and section boundaries in the setup wizard right after this.
              </div>
              <div class="field-group">
                <label class="field-label" for="reg-password">Password</label>
                <div class="pw-wrap">
                  <input id="reg-password" v-model="regForm.password"
                         :type="showPw ? 'text' : 'password'" class="field-input"
                         placeholder="Min. 8 characters" autocomplete="new-password" required />
                  <button type="button" class="pw-toggle" @click="showPw = !showPw" aria-label="Toggle password">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                         stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
                      <path v-if="showPw" d="M17.94 17.94A10.07 10.07 0 0112 20c-7 0-11-8-11-8a18.45 18.45 0 015.06-5.94M9.9 4.24A9.12 9.12 0 0112 4c7 0 11 8 11 8a18.5 18.5 0 01-2.16 3.19m-6.72-1.07a3 3 0 11-4.24-4.24M1 1l22 22"/>
                      <path v-else d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle v-if="!showPw" cx="12" cy="12" r="3"/>
                    </svg>
                  </button>
                </div>
              </div>
              <div class="pw-strength" v-if="regForm.password">
                <div class="pw-bar" :class="pwStrengthClass" />
                <span class="pw-label">{{ pwStrengthLabel }}</span>
              </div>
              <button type="submit" class="auth-submit" :disabled="submitting">
                <span v-if="!submitting">Create account</span>
                <span v-else class="submit-loading">
                  <span class="spinner" />
                  Creating account…
                </span>
              </button>
              <p class="auth-switch-hint">
                Already have an account?
                <button type="button" class="link-btn" @click="mode = 'login'">Sign in</button>
              </p>
            </form>

          </Transition>
        </div>

        <!-- Trust signals -->
        <div class="trust-row">
          <span class="trust-chip">
            <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>
            </svg>
            SSL encrypted
          </span>
          <span class="trust-chip">
            <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M20 7H4a2 2 0 00-2 2v6a2 2 0 002 2h16a2 2 0 002-2V9a2 2 0 00-2-2z"/>
              <path d="M16 21V5a2 2 0 00-2-2h-4a2 2 0 00-2 2v16"/>
            </svg>
            No credit card required
          </span>
          <span class="trust-chip">
            <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="12" cy="12" r="10"/>
              <polyline points="12 6 12 12 16 14"/>
            </svg>
            5-min setup
          </span>
        </div>
      </div>
    </div>

    <!-- ── Features section ── -->
    <section class="features-section">
      <div class="feat-section-label">WHAT YOU GET</div>
      <h2 class="feat-section-h2">Every tool your farm needs,<br>nothing it doesn't</h2>

      <div class="feat-grid">
        <div
          v-for="(feat, i) in features"
          :key="feat.id"
          class="feat-card"
          :style="`--fi: ${i}`"
          :ref="el => { if (el) featCardRefs[i] = el as Element }"
        >
          <div class="feat-card-icon" :style="{ background: feat.iconBg }">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                 stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round"
                 :style="{ color: feat.iconColor }"
                 v-html="feat.icon" />
          </div>
          <div class="feat-card-name">{{ feat.name }}</div>
          <div class="feat-card-desc">{{ feat.desc }}</div>
          <div v-if="feat.flagged" class="feat-flag-chip">
            <svg width="9" height="9" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M4 15s1-1 4-1 5 2 8 2 4-1 4-1V3s-1 1-4 1-5-2-8-2-4 1-4 1z"/>
              <line x1="4" y1="22" x2="4" y2="15"/>
            </svg>
            Feature-flagged
          </div>
        </div>
      </div>
    </section>

    <!-- ── Footer ── -->
    <footer class="land-footer">
      <div class="land-logo footer-logo">
        <svg width="16" height="16" viewBox="0 0 72 72" fill="none">
          <rect width="72" height="72" rx="16" fill="#1A4A32"/>
          <circle cx="36" cy="36" r="7" fill="#E8A838"/>
          <circle cx="36" cy="36" r="3.5" fill="#0D2B1F"/>
          <line x1="24" y1="24" x2="36" y2="36" stroke="#3DAA6A" stroke-width="1.5"/>
          <line x1="48" y1="24" x2="36" y2="36" stroke="#3DAA6A" stroke-width="1.5"/>
          <line x1="24" y1="48" x2="36" y2="36" stroke="#3DAA6A" stroke-width="1.5"/>
          <line x1="48" y1="48" x2="36" y2="36" stroke="#3DAA6A" stroke-width="1.5"/>
        </svg>
        <span>Agro<span class="logo-accent">Mesh</span></span>
      </div>
      <p class="foot-copy">© {{ new Date().getFullYear() }} AgroMesh. Purpose-built for Nigerian agriculture.</p>
    </footer>

  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAgroStore } from '@/stores/agro'
import { extractErrorMessage } from '@/utils/http'

const router = useRouter()
const store  = useAgroStore()

// ── Auth mode ──
const mode      = ref<'login' | 'register'>('login')
const showPw    = ref(false)
const submitting = ref(false)
const authError  = ref('')
const authColRef = ref<HTMLElement | null>(null)

const loginForm = ref({ email: '', password: '' })
const regForm   = ref({
  fullName: '', email: '', password: '', farmName: '',
})

function scrollToAuth() {
  authColRef.value?.scrollIntoView({ behavior: 'smooth', block: 'center' })
}

// Password strength
const pwStrengthScore = computed(() => {
  const pw = regForm.value.password
  if (!pw) return 0
  let s = 0
  if (pw.length >= 8) s++
  if (/[A-Z]/.test(pw)) s++
  if (/[0-9]/.test(pw)) s++
  if (/[^A-Za-z0-9]/.test(pw)) s++
  return s
})
const pwStrengthClass = computed(() =>
  ['', 'pw-weak', 'pw-fair', 'pw-good', 'pw-strong'][pwStrengthScore.value] || ''
)
const pwStrengthLabel = computed(() =>
  ['', 'Weak', 'Fair', 'Good', 'Strong'][pwStrengthScore.value] || ''
)

async function handleLogin() {
  authError.value = ''
  submitting.value = true
  try {
    await store.login(loginForm.value.email, loginForm.value.password)
    await store.initDashboard()
    router.push({ name: 'dashboard' })
  } catch (e: unknown) {
    authError.value = extractErrorMessage(e, 'Invalid email or password.')
  } finally {
    submitting.value = false
  }
}

async function handleRegister() {
  authError.value = ''
  if (regForm.value.password.length < 8) {
    authError.value = 'Password must be at least 8 characters.'
    return
  }
  submitting.value = true
  try {
    await store.register({
      fullName: regForm.value.fullName,
      email:    regForm.value.email,
      password: regForm.value.password,
      farmName: regForm.value.farmName,
    })
    // Spec: land on the dashboard with a "Complete farm setup" banner/CTA — never block
    // the dashboard behind setup completion. The farmer can explore immediately.
    await store.initDashboard()
    router.push({ name: 'dashboard' })
  } catch (e: unknown) {
    authError.value = extractErrorMessage(e, 'Registration failed. Please try again.')
  } finally {
    submitting.value = false
  }
}

// ── Feature pills (hero) ──
const featurePills = [
  { label: 'AI cost tracking',       icon: '<circle cx="12" cy="12" r="10"/><path d="M12 8v4l3 3"/>' },
  { label: 'Field activity logs',    icon: '<path d="M9 11l3 3L22 4"/><path d="M21 12v7a2 2 0 01-2 2H5a2 2 0 01-2-2V5a2 2 0 012-2h11"/>' },
  { label: 'Smart scheduling',       icon: '<rect x="3" y="4" width="18" height="18" rx="2" ry="2"/><line x1="16" y1="2" x2="16" y2="6"/><line x1="8" y1="2" x2="8" y2="6"/><line x1="3" y1="10" x2="21" y2="10"/>' },
  { label: 'Inventory auto-deduct',  icon: '<path d="M21 16V8a2 2 0 00-1-1.73l-7-4a2 2 0 00-2 0l-7 4A2 2 0 003 8v8a2 2 0 001 1.73l7 4a2 2 0 002 0l7-4A2 2 0 0021 16z"/>' },
  { label: 'Weather-aware AI',       icon: '<path d="M18 10h-1.26A8 8 0 109 20h9a5 5 0 000-10z"/>' },
  { label: 'Worker payroll',         icon: '<path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2"/><circle cx="9" cy="7" r="4"/>' },
]

// ── Feature cards ──
const features = [
  {
    id: 'finance',
    name: 'AI Financial Intelligence',
    desc: 'Connect your bank via Mono. AI auto-categorizes every transaction — Labour, Chemicals, Fuel — with crop allocation for P&L by section.',
    icon: '<line x1="12" y1="1" x2="12" y2="23"/><path d="M17 5H9.5a3.5 3.5 0 000 7h5a3.5 3.5 0 010 7H6"/>',
    iconBg: 'rgba(61,170,106,0.12)',
    iconColor: '#3DAA6A',
    flagged: true,
  },
  {
    id: 'activities',
    name: 'Field Activity Logging',
    desc: 'Log weeding, spraying, fertilizer application and more. Capture the worked area on a map, inputs used, workers present, and labour cost in one tap.',
    icon: '<path d="M9 11l3 3L22 4"/><path d="M21 12v7a2 2 0 01-2 2H5a2 2 0 01-2-2V5a2 2 0 012-2h11"/>',
    iconBg: 'rgba(45,122,79,0.12)',
    iconColor: '#2D7A4F',
    flagged: false,
  },
  {
    id: 'scheduling',
    name: 'AI Scheduling & Weather',
    desc: 'AI predicts next weeding and spray dates using your activity history, crop mix, Nigerian seasons, and a live 16-day Open-Meteo weather forecast.',
    icon: '<path d="M18 10h-1.26A8 8 0 109 20h9a5 5 0 000-10z"/>',
    iconBg: 'rgba(59,130,246,0.12)',
    iconColor: '#3B82F6',
    flagged: true,
  },
  {
    id: 'inventory',
    name: 'Inventory Tracking',
    desc: 'Stock auto-deducts when you log an activity. Get low-stock alerts before you run out of Glyphosate mid-season. Link receipts to purchase transactions.',
    icon: '<path d="M21 16V8a2 2 0 00-1-1.73l-7-4a2 2 0 00-2 0l-7 4A2 2 0 003 8v8a2 2 0 001 1.73l7 4a2 2 0 002 0l7-4A2 2 0 0021 16z"/><polyline points="3.27 6.96 12 12.01 20.73 6.96"/>',
    iconBg: 'rgba(232,168,56,0.12)',
    iconColor: '#B8780A',
    flagged: true,
  },
  {
    id: 'workers',
    name: 'Worker Management',
    desc: 'Define roles with daily rates (Field Supervisor ₦3,500, Farm Worker ₦2,200). Rates snapshot into activity logs so historical cost stays accurate when rates change.',
    icon: '<path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 00-3-3.87"/><path d="M16 3.13a4 4 0 010 7.75"/>',
    iconBg: 'rgba(160,120,60,0.12)',
    iconColor: '#9A7040',
    flagged: true,
  },
  {
    id: 'sections',
    name: 'Multi-Crop Sections',
    desc: 'Map your farm into sections on a live Leaflet map. Each section supports multiple crops with percentage allocation. Hectares or acres — your preference.',
    icon: '<path d="M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2z"/><polyline points="9 22 9 12 15 12 15 22"/>',
    iconBg: 'rgba(0,200,150,0.10)',
    iconColor: 'var(--data)',
    flagged: false,
  },
]

// ── Scroll-triggered feature card animation ──
const featCardRefs = ref<(Element | null)[]>([])

onMounted(() => {
  const observer = new IntersectionObserver(
    entries => {
      entries.forEach(entry => {
        if (entry.isIntersecting) {
          entry.target.classList.add('feat-visible')
          observer.unobserve(entry.target)
        }
      })
    },
    { threshold: 0.15 }
  )
  featCardRefs.value.forEach(el => el && observer.observe(el))
})
</script>

<style scoped>
/* ── Root ── */
.landing {
  min-height: 100dvh;
  background: #0D2B1F;
  color: #fff;
  position: relative;
  overflow-x: hidden;
}

/* ── Animated background ── */
.land-bg {
  position: fixed;
  inset: 0;
  pointer-events: none;
  z-index: 0;
}
.bg-grid {
  position: absolute;
  inset: 0;
}
.bg-orb {
  position: absolute;
  border-radius: 50%;
  filter: blur(80px);
  opacity: 0.35;
}
.bg-orb-1 {
  width: 600px;
  height: 600px;
  top: -160px;
  right: -100px;
  background: radial-gradient(circle, rgba(61,170,106,0.5) 0%, transparent 70%);
  animation: orb-drift-1 14s ease-in-out infinite;
}
.bg-orb-2 {
  width: 500px;
  height: 500px;
  bottom: -100px;
  left: -120px;
  background: radial-gradient(circle, rgba(45,122,79,0.4) 0%, transparent 70%);
  animation: orb-drift-2 18s ease-in-out infinite;
}
.bg-orb-3 {
  width: 300px;
  height: 300px;
  top: 40%;
  left: 30%;
  background: radial-gradient(circle, rgba(232,168,56,0.18) 0%, transparent 70%);
  animation: orb-drift-3 22s ease-in-out infinite;
}
@keyframes orb-drift-1 {
  0%, 100% { transform: translate(0, 0) scale(1); }
  33%       { transform: translate(40px, -30px) scale(1.06); }
  66%       { transform: translate(-20px, 20px) scale(0.94); }
}
@keyframes orb-drift-2 {
  0%, 100% { transform: translate(0, 0) scale(1); }
  33%       { transform: translate(-50px, 40px) scale(0.94); }
  66%       { transform: translate(30px, -20px) scale(1.06); }
}
@keyframes orb-drift-3 {
  0%, 100% { transform: translate(0, 0); }
  50%       { transform: translate(60px, -40px); }
}
@media (prefers-reduced-motion: reduce) {
  .bg-orb { animation: none !important; }
}

/* ── Nav ── */
.land-nav {
  position: relative;
  z-index: 10;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 20px 48px;
}
.land-logo {
  display: flex;
  align-items: center;
  gap: 8px;
  font-family: 'Syne', sans-serif;
  font-weight: 800;
  font-size: 20px;
  color: #fff;
  letter-spacing: -0.04em;
}
.logo-accent { color: #3DAA6A; }
.nav-signin-link {
  font-size: 13px;
  font-weight: 500;
  color: rgba(255,255,255,0.65);
  text-decoration: none;
  padding: 7px 16px;
  border-radius: 8px;
  border: 1px solid rgba(255,255,255,0.12);
  transition: background 150ms, color 150ms, border-color 150ms;
}
.nav-signin-link:hover {
  background: rgba(255,255,255,0.07);
  color: #fff;
  border-color: rgba(255,255,255,0.22);
}

/* ── Main split ── */
.land-main {
  position: relative;
  z-index: 1;
  display: flex;
  align-items: flex-start;
  gap: 64px;
  padding: 60px 48px 80px;
  max-width: 1280px;
  margin: 0 auto;
}

/* ── Hero col ── */
.hero-col {
  flex: 1;
  min-width: 0;
  padding-top: 16px;
}

.hero-badge {
  display: inline-flex;
  align-items: center;
  gap: 7px;
  padding: 5px 12px;
  border-radius: 20px;
  background: rgba(61,170,106,0.1);
  border: 1px solid rgba(61,170,106,0.25);
  font-family: 'JetBrains Mono', monospace;
  font-size: 10px;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: #3DAA6A;
  margin-bottom: 28px;
  animation: fade-up 500ms var(--ease-out, cubic-bezier(.16,1,.3,1)) both;
  animation-delay: calc(var(--i, 0) * 80ms + 100ms);
}
.badge-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: #3DAA6A;
  box-shadow: 0 0 0 3px rgba(61,170,106,0.2);
  animation: pulse-dot 2s ease-in-out infinite;
}
@keyframes pulse-dot {
  0%, 100% { box-shadow: 0 0 0 3px rgba(61,170,106,0.2); }
  50%       { box-shadow: 0 0 0 6px rgba(61,170,106,0.08); }
}

.hero-h1 {
  font-family: 'Syne', sans-serif;
  font-size: clamp(36px, 5vw, 60px);
  font-weight: 800;
  line-height: 1.1;
  letter-spacing: -0.04em;
  color: #fff;
  margin: 0 0 20px;
  animation: fade-up 500ms var(--ease-out, cubic-bezier(.16,1,.3,1)) both;
  animation-delay: calc(var(--i, 0) * 80ms + 100ms);
}
.hero-accent {
  background: linear-gradient(135deg, #3DAA6A 0%, #E8A838 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.hero-sub {
  font-size: 16px;
  line-height: 1.65;
  color: rgba(255,255,255,0.6);
  max-width: 500px;
  margin: 0 0 32px;
  animation: fade-up 500ms var(--ease-out, cubic-bezier(.16,1,.3,1)) both;
  animation-delay: calc(var(--i, 0) * 80ms + 100ms);
}

.hero-stats {
  display: flex;
  align-items: center;
  gap: 24px;
  margin-bottom: 32px;
  animation: fade-up 500ms var(--ease-out, cubic-bezier(.16,1,.3,1)) both;
  animation-delay: calc(var(--i, 0) * 80ms + 100ms);
}
.h-stat-sep {
  width: 1px;
  height: 32px;
  background: rgba(255,255,255,0.1);
}
.h-stat-n {
  font-family: 'Syne', sans-serif;
  font-weight: 800;
  font-size: 22px;
  color: #fff;
  letter-spacing: -0.03em;
}
.h-stat-l {
  font-family: 'JetBrains Mono', monospace;
  font-size: 10px;
  color: rgba(255,255,255,0.4);
  letter-spacing: 0.04em;
  text-transform: uppercase;
  margin-top: 2px;
}

/* Feature pills */
.hero-pills {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  animation: fade-up 500ms var(--ease-out, cubic-bezier(.16,1,.3,1)) both;
  animation-delay: calc(var(--i, 0) * 80ms + 100ms);
}
.feat-pill {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 5px 10px;
  border-radius: 6px;
  background: rgba(255,255,255,0.05);
  border: 1px solid rgba(255,255,255,0.08);
  font-size: 11px;
  color: rgba(255,255,255,0.55);
  animation: fade-up 400ms var(--ease-out, cubic-bezier(.16,1,.3,1)) both;
  animation-delay: calc(var(--pi, 0) * 50ms + 500ms);
  transition: background 150ms, color 150ms;
}
.feat-pill:hover {
  background: rgba(61,170,106,0.08);
  color: rgba(255,255,255,0.8);
  border-color: rgba(61,170,106,0.2);
}

/* ── Auth col ── */
.auth-col {
  width: 420px;
  flex-shrink: 0;
  position: sticky;
  top: 32px;
  animation: scale-in 500ms var(--ease-out, cubic-bezier(.16,1,.3,1)) both;
  animation-delay: 200ms;
}

.auth-card {
  background: rgba(255,255,255,0.04);
  border: 1px solid rgba(255,255,255,0.09);
  border-radius: 20px;
  padding: 28px;
  backdrop-filter: blur(16px);
  -webkit-backdrop-filter: blur(16px);
  box-shadow: 0 24px 64px rgba(0,0,0,0.4), 0 8px 16px rgba(0,0,0,0.2);
}

.auth-card-head { margin-bottom: 20px; }
.auth-greeting {
  font-family: 'Syne', sans-serif;
  font-size: 20px;
  font-weight: 700;
  color: #fff;
  letter-spacing: -0.03em;
}
.auth-sub {
  font-size: 12px;
  color: rgba(255,255,255,0.45);
  margin-top: 3px;
}

/* Tabs */
.auth-tabs {
  display: flex;
  position: relative;
  background: rgba(255,255,255,0.05);
  border-radius: 10px;
  padding: 3px;
  margin-bottom: 20px;
}
.auth-tab {
  flex: 1;
  padding: 8px 12px;
  border-radius: 7px;
  border: none;
  background: transparent;
  font-size: 12px;
  font-weight: 500;
  color: rgba(255,255,255,0.45);
  cursor: pointer;
  transition: color 200ms;
  position: relative;
  z-index: 1;
}
.auth-tab.active { color: #fff; }
.tab-slider {
  position: absolute;
  top: 3px;
  left: 3px;
  width: calc(50% - 3px);
  height: calc(100% - 6px);
  background: rgba(61,170,106,0.18);
  border: 1px solid rgba(61,170,106,0.25);
  border-radius: 7px;
  transition: transform 200ms cubic-bezier(0.34, 1.56, 0.64, 1);
}

/* Error */
.auth-error {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 12px;
  border-radius: 8px;
  background: rgba(220,60,60,0.1);
  border: 1px solid rgba(220,60,60,0.2);
  font-size: 12px;
  color: #F87171;
  margin-bottom: 16px;
}
.auth-err-enter-active { animation: slide-down 200ms ease both; }
.auth-err-leave-active { animation: slide-down 150ms ease reverse both; }

/* Form */
.auth-form { display: flex; flex-direction: column; gap: 14px; }
.fields-row { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }

.field-group { display: flex; flex-direction: column; gap: 5px; }
.field-label {
  font-family: 'JetBrains Mono', monospace;
  font-size: 10px;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: rgba(255,255,255,0.45);
}
.field-input {
  background: rgba(255,255,255,0.06);
  border: 1px solid rgba(255,255,255,0.1);
  border-radius: 9px;
  padding: 10px 12px;
  font-size: 13px;
  color: #fff;
  outline: none;
  transition: border-color 150ms, background 150ms;
  min-height: 44px;
  width: 100%;
  box-sizing: border-box;
}
.field-input::placeholder { color: rgba(255,255,255,0.25); }
.field-input:focus {
  border-color: rgba(61,170,106,0.5);
  background: rgba(255,255,255,0.08);
}

.hint-box {
  background: rgba(61,170,106,0.07);
  border: 1px solid rgba(61,170,106,0.18);
  border-radius: 8px;
  padding: 9px 12px;
  font-size: 11px;
  color: rgba(255,255,255,0.55);
  line-height: 1.5;
}

/* Select */
.select-wrap { position: relative; }
.field-select {
  appearance: none;
  -webkit-appearance: none;
  padding-right: 32px;
  cursor: pointer;
}
.field-select option { background: #0D2B1F; color: #fff; }
.select-chevron {
  position: absolute;
  right: 10px;
  top: 50%;
  transform: translateY(-50%);
  pointer-events: none;
  color: rgba(255,255,255,0.35);
}

/* GPS location */
.label-opt {
  font-size: 9px;
  font-family: 'JetBrains Mono', monospace;
  color: rgba(255,255,255,0.35);
  font-weight: 400;
  margin-left: 6px;
  text-transform: uppercase;
  letter-spacing: 0.06em;
}
.gps-btn {
  display: flex;
  align-items: center;
  gap: 7px;
  width: 100%;
  padding: 9px 14px;
  background: rgba(61,170,106,0.08);
  border: 1px solid rgba(61,170,106,0.22);
  border-radius: 10px;
  color: #3DAA6A;
  font-size: 12px;
  font-family: 'JetBrains Mono', monospace;
  cursor: pointer;
  transition: background 0.15s, border-color 0.15s;
  margin-bottom: 8px;
}
.gps-btn:hover:not(:disabled) { background: rgba(61,170,106,0.14); border-color: rgba(61,170,106,0.4); }
.gps-btn:disabled { opacity: 0.6; cursor: not-allowed; }
.gps-spinner {
  width: 11px; height: 11px;
  border: 1.5px solid rgba(61,170,106,0.3);
  border-top-color: #3DAA6A;
  border-radius: 50%;
  animation: spin 0.7s linear infinite;
  flex-shrink: 0;
}
.gps-result {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 6px 10px;
  background: rgba(61,170,106,0.1);
  border: 1px solid rgba(61,170,106,0.25);
  border-radius: 8px;
  font-size: 10px;
  font-family: 'JetBrains Mono', monospace;
  color: #3DAA6A;
  margin-bottom: 8px;
}
.gps-clear {
  margin-left: auto;
  background: none;
  border: none;
  color: rgba(255,255,255,0.4);
  cursor: pointer;
  font-size: 13px;
  line-height: 1;
  padding: 0 2px;
}
.gps-clear:hover { color: rgba(255,255,255,0.7); }
.gps-err-msg {
  font-size: 10px;
  color: #EF4444;
  margin-bottom: 6px;
  padding: 0 2px;
}
.gps-manual-row {
  display: flex;
  gap: 8px;
}
.gps-manual-row .field-input { font-size: 11px; }
.gps-fade-enter-active, .gps-fade-leave-active { transition: opacity 0.2s, transform 0.2s; }
.gps-fade-enter-from, .gps-fade-leave-to { opacity: 0; transform: translateY(-4px); }

.pw-wrap { position: relative; }
.pw-wrap .field-input { padding-right: 40px; }
.pw-toggle {
  position: absolute;
  right: 10px;
  top: 50%;
  transform: translateY(-50%);
  background: none;
  border: none;
  color: rgba(255,255,255,0.35);
  cursor: pointer;
  padding: 4px;
  transition: color 150ms;
}
.pw-toggle:hover { color: rgba(255,255,255,0.7); }

.pw-strength { display: flex; align-items: center; gap: 8px; margin-top: -6px; }
.pw-bar {
  height: 3px;
  border-radius: 2px;
  flex: 1;
  background: rgba(255,255,255,0.08);
  position: relative;
  overflow: hidden;
}
.pw-bar::after {
  content: '';
  position: absolute;
  inset: 0;
  border-radius: 2px;
  transition: width 300ms, background 300ms;
}
.pw-weak::after    { width: 25%;  background: #EF4444; }
.pw-fair::after    { width: 50%;  background: #F97316; }
.pw-good::after    { width: 75%;  background: #EAB308; }
.pw-strong::after  { width: 100%; background: #22C55E; }
.pw-label {
  font-family: 'JetBrains Mono', monospace;
  font-size: 9px;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: rgba(255,255,255,0.35);
  min-width: 40px;
}

.auth-submit {
  width: 100%;
  min-height: 44px;
  padding: 11px 16px;
  border-radius: 10px;
  border: none;
  background: #3DAA6A;
  color: #fff;
  font-family: 'Syne', sans-serif;
  font-size: 13px;
  font-weight: 700;
  letter-spacing: 0.01em;
  cursor: pointer;
  transition: background 150ms, transform 150ms, box-shadow 150ms;
  box-shadow: 0 4px 16px rgba(61,170,106,0.35);
}
.auth-submit:hover:not(:disabled) {
  background: #2D7A4F;
  box-shadow: 0 6px 24px rgba(61,170,106,0.45);
  transform: translateY(-1px);
}
.auth-submit:active:not(:disabled) { transform: scale(0.97); }
.auth-submit:disabled { opacity: 0.55; cursor: not-allowed; }

.submit-loading {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
}
.spinner {
  width: 14px;
  height: 14px;
  border: 2px solid rgba(255,255,255,0.3);
  border-top-color: #fff;
  border-radius: 50%;
  animation: spin 600ms linear infinite;
}
@keyframes spin { to { transform: rotate(360deg); } }

.auth-switch-hint {
  text-align: center;
  font-size: 12px;
  color: rgba(255,255,255,0.35);
  margin: 0;
}
.link-btn {
  background: none;
  border: none;
  color: #3DAA6A;
  font-size: 12px;
  cursor: pointer;
  padding: 0;
  text-decoration: underline;
  text-underline-offset: 2px;
}
.link-btn:hover { color: #5EC986; }

/* Form transition */
.form-switch-enter-active { animation: fade-up 220ms var(--ease-out, cubic-bezier(.16,1,.3,1)) both; }
.form-switch-leave-active { animation: fade-up 140ms var(--ease-out, cubic-bezier(.16,1,.3,1)) reverse both; }

/* Trust row */
.trust-row {
  display: flex;
  justify-content: center;
  gap: 12px;
  margin-top: 14px;
  flex-wrap: wrap;
}
.trust-chip {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  font-family: 'JetBrains Mono', monospace;
  font-size: 9px;
  letter-spacing: 0.05em;
  color: rgba(255,255,255,0.3);
  text-transform: uppercase;
}

/* ── Features section ── */
.features-section {
  position: relative;
  z-index: 1;
  padding: 80px 48px;
  max-width: 1280px;
  margin: 0 auto;
  border-top: 1px solid rgba(255,255,255,0.05);
}
.feat-section-label {
  font-family: 'JetBrains Mono', monospace;
  font-size: 9px;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  color: #3DAA6A;
  margin-bottom: 12px;
}
.feat-section-h2 {
  font-family: 'Syne', sans-serif;
  font-size: clamp(24px, 3.5vw, 40px);
  font-weight: 800;
  line-height: 1.15;
  letter-spacing: -0.03em;
  color: #fff;
  margin: 0 0 48px;
}
.feat-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 20px;
}

/* Feature card: hidden until IntersectionObserver fires */
.feat-card {
  background: rgba(255,255,255,0.03);
  border: 1px solid rgba(255,255,255,0.07);
  border-radius: 16px;
  padding: 24px;
  opacity: 0;
  transform: translateY(16px);
  transition:
    opacity 400ms ease both,
    transform 400ms cubic-bezier(.16,1,.3,1) both,
    background 150ms,
    border-color 150ms;
  transition-delay: calc(var(--fi, 0) * 60ms);
}
.feat-card.feat-visible {
  opacity: 1;
  transform: translateY(0);
}
.feat-card:hover {
  background: rgba(255,255,255,0.055);
  border-color: rgba(61,170,106,0.2);
}

.feat-card-icon {
  width: 40px;
  height: 40px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 14px;
}
.feat-card-name {
  font-family: 'Syne', sans-serif;
  font-size: 15px;
  font-weight: 700;
  color: #fff;
  letter-spacing: -0.02em;
  margin-bottom: 8px;
}
.feat-card-desc {
  font-size: 13px;
  line-height: 1.6;
  color: rgba(255,255,255,0.45);
}
.feat-flag-chip {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  margin-top: 14px;
  padding: 3px 8px;
  border-radius: 5px;
  background: rgba(232,168,56,0.08);
  border: 1px solid rgba(232,168,56,0.18);
  font-family: 'JetBrains Mono', monospace;
  font-size: 9px;
  letter-spacing: 0.05em;
  text-transform: uppercase;
  color: #E8A838;
}

/* ── Footer ── */
.land-footer {
  position: relative;
  z-index: 1;
  padding: 28px 48px;
  border-top: 1px solid rgba(255,255,255,0.05);
  display: flex;
  align-items: center;
  justify-content: space-between;
  max-width: 1280px;
  margin: 0 auto;
}
.footer-logo { font-size: 15px; }
.foot-copy {
  font-family: 'JetBrains Mono', monospace;
  font-size: 10px;
  color: rgba(255,255,255,0.25);
  margin: 0;
}

/* ── Responsive ── */
@media (max-width: 900px) {
  .land-main {
    flex-direction: column;
    padding: 40px 24px 60px;
    gap: 48px;
  }
  .auth-col {
    width: 100%;
    position: static;
  }
  .land-nav { padding: 16px 24px; }
  .features-section { padding: 60px 24px; }
  .feat-grid { grid-template-columns: 1fr 1fr; }
  .land-footer { padding: 24px; flex-direction: column; gap: 8px; text-align: center; }
}
@media (max-width: 560px) {
  .feat-grid { grid-template-columns: 1fr; }
  .fields-row { grid-template-columns: 1fr; }
  .hero-h1 { font-size: 32px; }
}

/* ── Global keyframe reuse ── */
@keyframes fade-up {
  from { opacity: 0; transform: translateY(12px); }
  to   { opacity: 1; transform: translateY(0); }
}
@keyframes scale-in {
  from { opacity: 0; transform: scale(0.96) translateY(10px); }
  to   { opacity: 1; transform: scale(1) translateY(0); }
}
@keyframes slide-down {
  from { opacity: 0; transform: translateY(-8px); }
  to   { opacity: 1; transform: translateY(0); }
}
</style>
