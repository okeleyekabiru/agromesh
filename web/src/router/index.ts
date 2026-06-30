import { createRouter, createWebHistory } from 'vue-router'
import LandingView     from '@/views/LandingView.vue'
import DashboardView   from '@/views/DashboardView.vue'
import ActivitiesView  from '@/views/ActivitiesView.vue'
import SectionsView    from '@/views/SectionsView.vue'
import FinanceView     from '@/views/FinanceView.vue'
import WorkersView     from '@/views/WorkersView.vue'
import ReportsView     from '@/views/ReportsView.vue'
import InventoryView   from '@/views/InventoryView.vue'
import OnboardingView    from '@/views/OnboardingView.vue'
import SectionsSetupView from '@/views/SectionsSetupView.vue'

const routes = [
  // Public
  { path: '/',              name: 'landing',        component: LandingView,      meta: { public: true } },
  { path: '/onboarding',    name: 'onboarding',     component: OnboardingView,   meta: { public: true } },
  { path: '/sections-setup', name: 'sections-setup', component: SectionsSetupView, meta: { public: true } },

  // App (require auth)
  { path: '/dashboard',  name: 'dashboard',  component: DashboardView,  meta: { title: 'Farm Overview',  sub: 'JUNE 2025' } },
  { path: '/activities', name: 'activities', component: ActivitiesView, meta: { title: 'Activities',     sub: 'ALL ACTIVITY LOGS' } },
  { path: '/sections',   name: 'sections',   component: SectionsView,   meta: { title: 'Sections',       sub: 'OYO STATE' } },
  { path: '/finance',    name: 'finance',    component: FinanceView,    meta: { title: 'Finance',        sub: 'MONO SYNC',      feature: 'finance' } },
  { path: '/workers',    name: 'workers',    component: WorkersView,    meta: { title: 'Workers',        sub: 'WORKFORCE',      feature: 'workers' } },
  { path: '/reports',    name: 'reports',    component: ReportsView,    meta: { title: 'Reports',        sub: 'OPERATIONAL',    feature: 'reports' } },
  { path: '/inventory',  name: 'inventory',  component: InventoryView,  meta: { title: 'Inventory',      sub: 'STOCK & SUPPLIES', feature: 'inventory' } },
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

router.beforeEach(async (to) => {
  const { useAgroStore } = await import('@/stores/agro')
  const store = useAgroStore()

  // Rehydrate user from localStorage if needed
  if (!store.currentUser) store.restoreSession()

  const authed = store.isAuthenticated

  // Landing: redirect authenticated users straight to the app
  if (to.name === 'landing' && authed) return { name: 'dashboard' }

  // Onboarding is always accessible (just needs to be logged in or just registered)
  if (to.name === 'onboarding') return true

  // All non-public routes require auth
  if (!to.meta.public && !authed) return { name: 'landing' }

  // Feature-gated routes: redirect to dashboard if flag is off for this farm
  if (to.meta.feature && store.enabledFeatures.length > 0) {
    if (!store.enabledFeatures.includes(to.meta.feature as string)) {
      return { name: 'dashboard' }
    }
  }

  // Onboarding check: new farms without sections
  if (!to.meta.public && store.needsOnboarding) return { name: 'onboarding' }

  return true
})

export default router
