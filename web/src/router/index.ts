import { createRouter, createWebHistory } from 'vue-router'
import DashboardView    from '@/views/DashboardView.vue'
import ActivitiesView   from '@/views/ActivitiesView.vue'
import SectionsView     from '@/views/SectionsView.vue'
import FinanceView      from '@/views/FinanceView.vue'
import WorkersView      from '@/views/WorkersView.vue'
import ReportsView      from '@/views/ReportsView.vue'
import InventoryView    from '@/views/InventoryView.vue'
import OnboardingView   from '@/views/OnboardingView.vue'

const routes = [
  { path: '/onboarding', name: 'onboarding', component: OnboardingView, meta: { public: true } },
  { path: '/',           name: 'dashboard',  component: DashboardView,  meta: { title: 'Farm Overview',    sub: 'JUNE 2025' } },
  { path: '/activities', name: 'activities', component: ActivitiesView, meta: { title: 'Activities',       sub: 'ALL ACTIVITY LOGS' } },
  { path: '/sections',   name: 'sections',   component: SectionsView,   meta: { title: 'Sections',         sub: 'OYO STATE' } },
  { path: '/finance',    name: 'finance',    component: FinanceView,    meta: { title: 'Finance',          sub: 'MONO SYNC' } },
  { path: '/workers',    name: 'workers',    component: WorkersView,    meta: { title: 'Workers',          sub: 'WORKFORCE' } },
  { path: '/reports',    name: 'reports',    component: ReportsView,    meta: { title: 'Reports',          sub: 'OPERATIONAL' } },
  { path: '/inventory',  name: 'inventory',  component: InventoryView,  meta: { title: 'Inventory',       sub: 'STOCK & SUPPLIES' } },
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

// Guard: redirect to onboarding if the API has no farms yet
router.beforeEach(async (to) => {
  if (to.meta.public) return true
  const { useAgroStore } = await import('@/stores/agro')
  const store = useAgroStore()
  if (store.needsOnboarding) {
    return { name: 'onboarding' }
  }
})

export default router
