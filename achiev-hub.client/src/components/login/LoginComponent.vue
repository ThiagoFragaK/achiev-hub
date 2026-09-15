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
                        :disabled="loading"
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
                        :class="{ 'is-invalid': !!loginError }"
                        :disabled="loading"
                    />
                </div>

                <div v-if="loginError" class="alert alert-danger py-2" role="alert">
                    {{ loginError }}
                </div>

                <div class="d-grid mb-3">
                    <button type="submit" class="btn btn-primary btn-lg" :disabled="loading">
                        {{ loading ? 'Logging in…' : 'Login' }}
                    </button>
                    <button type="button" class="btn btn-secondary btn-lg mt-4" :disabled="loading" @click="onRegister">
                        Register
                    </button>
                </div>
            </form>

            <div class="text-center mb-3">
                <button
                    type="button"
                    class="btn btn-link"
                    :disabled="loading"
                    @click="onContinueAsGuest"
                >
                    Continue without login.
                </button>
            </div>

            <!-- <div class="text-center mb-2">
                <RouterLink to="/register" class="small"> Create an account </RouterLink>
            </div> -->

            <div class="text-center">
                <RouterLink to="/style-guide" class="small text-secondary">
                    Style Guide
                </RouterLink>
            </div>
        </div>
    </div>
</template>

<script>
import { continueAsGuest, login } from '@/services/authService'

export default {
    name: 'LoginComponent',
    data() {
        return {
            steamId: '',
            password: '',
            steamIdError: false,
            loginError: '',
            loading: false
        }
    },
    methods: {
        onRegister() {
            this.$router.push('/register')
        },
        redirectAfterLogin() {
            const redirect =
                typeof this.$route.query.redirect === 'string' ? this.$route.query.redirect : '/'
            this.$router.push(redirect)
        },
        async onLogin() {
            this.steamIdError = !this.steamId.trim()
            this.loginError = ''
            if (this.steamIdError) return

            this.loading = true
            try {
                await login(this.steamId.trim(), this.password)
                this.redirectAfterLogin()
            } catch (error) {
                this.loginError = error.body?.message || error.message || 'Login failed'
            } finally {
                this.loading = false
            }
        },
        async onContinueAsGuest() {
            this.steamIdError = !this.steamId.trim()
            this.loginError = ''
            if (this.steamIdError) return

            this.loading = true
            try {
                await continueAsGuest(this.steamId.trim())
                this.$router.push({ name: 'home' })
            } catch (error) {
                this.loginError = error.body?.message || error.message || 'Guest session failed'
            } finally {
                this.loading = false
            }
        }
    }
}
</script>
