import { createRouter, createWebHistory } from 'vue-router'
import DashboardView  from '@/views/DashboardView.vue'
import ActivitiesView from '@/views/ActivitiesView.vue'
import SectionsView   from '@/views/SectionsView.vue'
import FinanceView    from '@/views/FinanceView.vue'
import WorkersView    from '@/views/WorkersView.vue'
import ReportsView    from '@/views/ReportsView.vue'

const routes = [
  { path: '/',           name: 'dashboard',  component: DashboardView,  meta: { title: 'Farm Overview',        sub: 'KABIRU FARMS LTD · JUNE 2025' } },
  { path: '/activities', name: 'activities', component: ActivitiesView, meta: { title: 'Activities',           sub: 'ALL ACTIVITY LOGS' } },
  { path: '/sections',   name: 'sections',   component: SectionsView,   meta: { title: 'Sections',             sub: 'OYO STATE · 6 SECTIONS · 100 ACRES' } },
  { path: '/finance',    name: 'finance',    component: FinanceView,    meta: { title: 'Finance',              sub: 'MONO SYNC · LAST UPDATED 02:00 AM' } },
  { path: '/workers',    name: 'workers',    component: WorkersView,    meta: { title: 'Workers',              sub: 'WORKFORCE MANAGEMENT' } },
  { path: '/reports',    name: 'reports',    component: ReportsView,    meta: { title: 'Reports',              sub: 'OPERATIONAL REPORTS' } },
]

export default createRouter({
  history: createWebHistory(),
  routes
})
