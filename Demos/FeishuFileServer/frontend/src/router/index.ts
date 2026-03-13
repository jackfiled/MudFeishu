import { createRouter, createWebHistory } from 'vue-router'
import type { RouteRecordRaw } from 'vue-router'

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    name: 'Home',
    component: () => import('@/views/FileManager.vue'),
    children: [
      {
        path: 'folder/:folderToken?',
        name: 'Folder',
        component: () => import('@/views/FileManager.vue')
      }
    ]
  },
  {
    path: '/file/:fileToken',
    name: 'FileDetail',
    component: () => import('@/views/FileDetail.vue')
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

export default router
