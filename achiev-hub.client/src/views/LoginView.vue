<template>
    <div class="min-vh-100 d-flex align-items-center justify-content-center px-3 py-5">
        <div class="w-100" style="max-width: 28rem">
            <div class="text-center mb-4">
                <img
                    src="/assets/achievHub-logo2.png"
                    alt="Achievements Hub"
                    class="img-fluid"
                    style="max-height: 4rem"
                />
            </div>

            <hr class="mb-4 text-primary opacity-100" />

            <form @submit.prevent="onLogin">
                <div class="mb-3">
                    <label class="form-label fw-semibold" for="steamId"> Steam ID </label>
                    <input
                        id="steamId"
                        v-model="steamId"
                        type="text"
                        name="steamId"
                        autocomplete="username"
                        class="form-control"
                        :class="{ 'is-invalid': steamIdError }"
                        :aria-invalid="steamIdError"
                    />
                    <div
                        v-if="steamIdError"
                        class="invalid-feedback d-block text-center"
                        role="alert"
                    >
                        Required
                    </div>
                </div>

                <div class="mb-4">
                    <label class="form-label fw-semibold" for="password"> Password </label>
                    <input
                        id="password"
                        v-model="password"
                        type="password"
                        name="password"
                        autocomplete="current-password"
                        class="form-control"
                    />
                </div>

                <div class="d-grid mb-3">
                    <button type="submit" class="btn btn-primary btn-lg">Login</button>
                </div>
            </form>

            <div class="text-center mb-3">
                <button type="button" class="btn btn-link" @click="continueAsGuest">
                    Continue without login.
                </button>
            </div>

            <div class="text-center">
                <RouterLink to="/style-guide" class="small text-secondary">
                    Style Guide
                </RouterLink>
            </div>
        </div>
    </div>
</template>

<script>
import { setAuthenticated } from '@/lib/session'

export default {
    name: 'LoginView',
    data() {
        return {
            steamId: '',
            password: '',
            steamIdError: false
        }
    },
    methods: {
        enterApp() {
            setAuthenticated(true)
            const redirect =
                typeof this.$route.query.redirect === 'string' ? this.$route.query.redirect : '/'
            this.$router.push(redirect)
        },
        onLogin() {
            this.steamIdError = !this.steamId.trim()
            if (this.steamIdError) return
            this.enterApp()
        },
        continueAsGuest() {
            this.enterApp()
        }
    }
}
</script>
