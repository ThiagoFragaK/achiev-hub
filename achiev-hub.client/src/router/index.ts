import { createRouter, createWebHistory } from 'vue-router'
import { isAuthenticated } from '@/lib/session'
import GameDetailView from '@/views/GameDetailView.vue'
import HomeView from '@/views/HomeView.vue'
import LoginView from '@/views/LoginView.vue'
import MyGamesView from '@/views/MyGamesView.vue'
import StatsView from '@/views/StatsView.vue'
import SteamView from '@/views/SteamView.vue'
import StyleGuideView from '@/views/StyleGuideView.vue'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/login',
      name: 'login',
      component: LoginView,
      meta: { public: true },
    },
    {
      path: '/',
      name: 'home',
      component: HomeView,
    },
    {
      path: '/games',
      name: 'games',
      component: MyGamesView,
    },
    {
      path: '/games/:id',
      name: 'game-detail',
      component: GameDetailView,
    },
    {
      path: '/stats',
      name: 'stats',
      component: StatsView,
    },
    {
      path: '/steam',
      name: 'steam',
      component: SteamView,
    },
    {
      path: '/style-guide',
      name: 'style-guide',
      component: StyleGuideView,
      meta: { public: true },
    },
  ],
})

router.beforeEach((to) => {
  if (to.meta.public) return true
  if (!isAuthenticated()) {
    return { name: 'login', query: { redirect: to.fullPath } }
  }
  return true
})

export default router
