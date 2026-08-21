import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '@/views/HomeView.vue'
import StyleGuideView from '@/views/StyleGuideView.vue'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      name: 'home',
      component: HomeView,
    },
    {
      path: '/style-guide',
      name: 'style-guide',
      component: StyleGuideView,
    },
  ],
})

export default router
