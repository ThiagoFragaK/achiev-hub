<template>
    <nav
        class="app-navbar navbar navbar-expand-lg navbar-dark bg-dark border-bottom border-primary border-2"
    >
        <div class="container">
            <RouterLink to="/" class="navbar-brand py-0">
                <img src="/assets/achievHub-logo2.png" alt="Achievements Hub" height="40" />
            </RouterLink>

            <button
                class="navbar-toggler"
                type="button"
                data-bs-toggle="collapse"
                data-bs-target="#appNavbar"
                aria-controls="appNavbar"
                aria-expanded="false"
                aria-label="Toggle navigation"
            >
                <span class="navbar-toggler-icon" />
            </button>

            <div id="appNavbar" class="collapse navbar-collapse">
                <ul class="navbar-nav mx-auto mb-2 mb-lg-0 text-uppercase small fw-medium">
                    <li v-for="link in links" :key="link.to" class="nav-item">
                        <RouterLink
                            :to="link.to"
                            class="nav-link"
                            :class="{ active: isActive(link.name) }"
                        >
                            {{ link.label }}
                        </RouterLink>
                    </li>
                </ul>

                <div ref="userMenu" class="navbar-user dropdown">
                    <button
                        class="btn btn-link text-decoration-none text-white dropdown-toggle d-flex align-items-center gap-2"
                        :class="{ show: isUserMenuOpen }"
                        type="button"
                        :aria-expanded="isUserMenuOpen"
                        aria-label="User menu"
                        @click="toggleUserMenu"
                    >
                        <span class="small fw-medium">{{ displayName }}</span>
                        <span
                            class="d-inline-block rounded border bg-secondary overflow-hidden"
                            style="width: 2.25rem; height: 2.25rem"
                            aria-hidden="true"
                        >
                            <img
                                v-if="avatar"
                                :src="avatar"
                                alt=""
                                class="w-100 h-100 object-fit-cover"
                            />
                        </span>
                    </button>
                    <!-- data-bs-popper enables Bootstrap's CSS-only placement, used for navbar dropdowns -->
                    <ul
                        class="dropdown-menu dropdown-menu-end"
                        :class="{ show: isUserMenuOpen }"
                        data-bs-popper="static"
                    >
                        <li>
                            <button type="button" class="dropdown-item text-danger" @click="logout">
                                Logout
                            </button>
                        </li>
                    </ul>
                </div>
            </div>
        </div>
    </nav>
</template>

<script>
import { logout } from '@/services/authService'
import { getUser, mergeUser } from '@/lib/session'
import { getPlayer } from '@/services/playersService'

export default {
    name: 'AppHeader',
    data() {
        return {
            isUserMenuOpen: false,
            user: getUser(),
            links: [
                { to: '/', label: 'Home', name: 'home' },
                { to: '/games', label: 'My games', name: 'games' },
                { to: '/stats', label: 'Stats', name: 'stats' },
                { to: '/steam', label: 'Steam', name: 'steam' }
            ]
        }
    },
    computed: {
        displayName() {
            if (!this.user) return 'Guest'
            if (this.user.personaName) return this.user.personaName
            if (this.user.steamId) return this.user.steamId.slice(-4)
            return this.user.email?.split('@')[0] || 'User'
        },
        avatar() {
            return this.user?.avatar || ''
        }
    },
    watch: {
        $route() {
            this.isUserMenuOpen = false
        }
    },
    mounted() {
        document.addEventListener('click', this.onDocumentClick)
        document.addEventListener('keydown', this.onKeydown)
        this.loadSteamProfileIfNeeded()
    },
    beforeUnmount() {
        document.removeEventListener('click', this.onDocumentClick)
        document.removeEventListener('keydown', this.onKeydown)
    },
    methods: {
        isActive(name) {
            if (name === 'games') {
                return this.$route.name === 'games' || this.$route.name === 'game-detail'
            }
            return this.$route.name === name
        },
        toggleUserMenu() {
            this.isUserMenuOpen = !this.isUserMenuOpen
        },
        onDocumentClick(event) {
            if (this.isUserMenuOpen && !this.$refs.userMenu?.contains(event.target)) {
                this.isUserMenuOpen = false
            }
        },
        onKeydown(event) {
            if (event.key === 'Escape') {
                this.isUserMenuOpen = false
            }
        },
        async loadSteamProfileIfNeeded() {
            const steamId = this.user?.steamId?.trim()
            if (!steamId || (this.user.personaName && this.user.avatar)) {
                return
            }

            try {
                const player = await getPlayer(steamId)
                this.user = mergeUser({
                    personaName: player?.personaName || this.user.personaName,
                    avatar: player?.avatar || this.user.avatar
                })
            } catch {
                // Keep session fallbacks if Steam profile is unavailable.
            }
        },
        async logout() {
            this.isUserMenuOpen = false
            await logout()
            this.$router.push({ name: 'login' })
        }
    }
}
</script>
