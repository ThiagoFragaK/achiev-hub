<template>
    <nav class="navbar navbar-expand-lg navbar-dark bg-dark border-bottom border-primary border-2">
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

                <div class="dropdown">
                    <button
                        class="btn btn-link text-decoration-none text-white dropdown-toggle d-flex align-items-center gap-2"
                        type="button"
                        data-bs-toggle="dropdown"
                        aria-expanded="false"
                        aria-label="User menu"
                    >
                        <span class="small fw-medium">T.K.</span>
                        <span
                            class="d-inline-block rounded border bg-secondary"
                            style="width: 2.25rem; height: 2.25rem"
                            aria-hidden="true"
                        />
                    </button>
                    <ul class="dropdown-menu dropdown-menu-end">
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
import { setAuthenticated } from '@/lib/session'

export default {
    name: 'AppHeader',
    data() {
        return {
            links: [
                { to: '/', label: 'Home', name: 'home' },
                { to: '/games', label: 'My games', name: 'games' },
                { to: '/stats', label: 'Stats', name: 'stats' },
                { to: '/steam', label: 'Steam', name: 'steam' }
            ]
        }
    },
    methods: {
        isActive(name) {
            if (name === 'games') {
                return this.$route.name === 'games' || this.$route.name === 'game-detail'
            }
            return this.$route.name === name
        },
        logout() {
            setAuthenticated(false)
            this.$router.push({ name: 'login' })
        }
    }
}
</script>
