import { createRouter, createWebHistory } from 'vue-router'
import { isAuthenticated } from '@/lib/session'
import Games from '@/pages/Games.vue'
import Home from '@/pages/Home.vue'
import Login from '@/pages/Login.vue'
import MyGames from '@/pages/MyGames.vue'
import Stats from '@/pages/Stats.vue'
import Steam from '@/pages/Steam.vue'
import StyleGuide from '@/pages/StyleGuide.vue'

const router = createRouter({
    history: createWebHistory(),
    routes: [
        {
            path: '/login',
            name: 'login',
            component: Login,
            meta: { public: true }
        },
        {
            path: '/',
            name: 'home',
            component: Home
        },
        {
            path: '/games',
            name: 'games',
            component: MyGames
        },
        {
            path: '/games/:id',
            name: 'game-detail',
            component: Games
        },
        {
            path: '/stats',
            name: 'stats',
            component: Stats
        },
        {
            path: '/steam',
            name: 'steam',
            component: Steam
        },
        {
            path: '/style-guide',
            name: 'style-guide',
            component: StyleGuide,
            meta: { public: true }
        }
    ]
})

router.beforeEach((to) => {
    if (to.meta.public) return true
    if (!isAuthenticated()) {
        return { name: 'login', query: { redirect: to.fullPath } }
    }
    return true
})

export default router
