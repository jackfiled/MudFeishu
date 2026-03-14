import { createRouter, createWebHistory } from 'vue-router'
import type { RouteRecordRaw } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'

const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/LoginView.vue'),
    meta: { public: true }
  },
  {
    path: '/',
    name: 'Home',
    component: () => import('@/views/FileManager.vue'),
    meta: { requiresAuth: true },
    children: [
      {
        path: 'folder/:folderToken?',
        name: 'Folder',
        component: () => import('@/views/FileManager.vue')
      }
    ]
  },
  {
    path: '/recycle-bin',
    name: 'RecycleBin',
    component: () => import('@/views/RecycleBin.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/shares',
    name: 'ShareList',
    component: () => import('@/views/ShareList.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/share/:shareCode',
    name: 'ShareAccess',
    component: () => import('@/views/ShareAccess.vue'),
    meta: { public: true }
  },
  {
    path: '/file/:fileToken',
    name: 'FileDetail',
    component: () => import('@/views/FileDetail.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/:pathMatch(.*)*',
    name: 'NotFound',
    component: () => import('@/views/NotFound.vue')
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

router.beforeEach((to, _from, next) => {
  const authStore = useAuthStore()

  if (to.meta.requiresAuth && !authStore.isLoggedIn) {
    next('/login')
  } else if (to.path === '/login' && authStore.isLoggedIn) {
    next('/')
  } else {
    next()
  }
})

export default router
