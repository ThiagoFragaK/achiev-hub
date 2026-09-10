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

            <form @submit.prevent>
                <div class="mb-3">
                    <label class="form-label fw-semibold" for="regSteamId">Steam ID</label>
                    <input
                        id="regSteamId"
                        v-model="steamId"
                        type="text"
                        class="form-control"
                        :class="{ 'is-invalid': fieldErrors.steamId }"
                        :disabled="loading || emailVerified"
                        autocomplete="username"
                    />
                    <div v-if="fieldErrors.steamId" class="invalid-feedback d-block">Required</div>
                </div>

                <div class="mb-3">
                    <label class="form-label fw-semibold" for="regEmail">Email</label>
                    <input
                        id="regEmail"
                        v-model="email"
                        type="email"
                        class="form-control"
                        :class="{ 'is-invalid': fieldErrors.email }"
                        :disabled="loading || emailVerified"
                        autocomplete="email"
                    />
                    <div v-if="fieldErrors.email" class="invalid-feedback d-block">Required</div>
                </div>

                <div class="mb-3">
                    <label class="form-label fw-semibold" for="regPassword">Password</label>
                    <input
                        id="regPassword"
                        v-model="password"
                        type="password"
                        class="form-control"
                        :class="{ 'is-invalid': fieldErrors.password }"
                        :disabled="loading || emailVerified"
                        autocomplete="new-password"
                    />
                    <div class="form-text">
                        At least 12 characters, with uppercase, number, and special character.
                    </div>
                    <div v-if="fieldErrors.password" class="invalid-feedback d-block">
                        {{ fieldErrors.password }}
                    </div>
                </div>

                <div v-if="codeSent" class="mb-3">
                    <label class="form-label fw-semibold" for="regCode">Verification code</label>
                    <input
                        id="regCode"
                        v-model="code"
                        type="text"
                        inputmode="numeric"
                        maxlength="6"
                        class="form-control"
                        :disabled="loading || emailVerified"
                        autocomplete="one-time-code"
                    />
                    <div class="form-text">Enter the 6-digit code sent to your email.</div>
                </div>

                <div v-if="formError" class="alert alert-danger py-2" role="alert">
                    {{ formError }}
                </div>

                <div v-if="formSuccess" class="alert alert-success py-2" role="alert">
                    {{ formSuccess }}
                </div>

                <div class="d-grid gap-2">
                    <button
                        type="button"
                        class="btn btn-outline-secondary"
                        :disabled="loading"
                        @click="onCancel"
                    >
                        Cancel
                    </button>

                    <button
                        type="button"
                        class="btn btn-primary"
                        :disabled="
                            loading ||
                            emailVerified ||
                            (cooldownSeconds > 0 && !(codeSent && code.trim().length >= 6))
                        "
                        @click="onVerifyEmail"
                    >
                        <template v-if="emailVerified">Email verified</template>
                        <template v-else-if="codeSent && code.trim().length >= 6">
                            Confirm code
                        </template>
                        <template v-else-if="cooldownSeconds > 0">
                            Resend in {{ cooldownSeconds }}s
                        </template>
                        <template v-else-if="codeSent">Resend code</template>
                        <template v-else>Verify Email</template>
                    </button>

                    <button
                        type="button"
                        class="btn btn-success"
                        :disabled="loading || !emailVerified"
                        @click="onRegister"
                    >
                        {{ loading && emailVerified ? 'Registering…' : 'Register' }}
                    </button>
                </div>
            </form>

            <div class="text-center mt-3">
                <RouterLink to="/login" class="small"> Back to login </RouterLink>
            </div>
        </div>
    </div>
</template>

<script>
import { confirmCode, register, sendVerification } from '@/services/authService';
import { validatePassword } from '@/utils/PasswordHelper';

export default {
    name: 'RegisterComponent',
    data() {
        return {
            steamId: '',
            email: '',
            password: '',
            code: '',
            codeSent: false,
            emailVerified: false,
            emailVerifiedToken: '',
            cooldownSeconds: 0,
            cooldownTimer: null,
            loading: false,
            formError: '',
            formSuccess: '',
            fieldErrors: {
                steamId: false,
                email: false,
                password: ''
            }
        }
    },
    beforeUnmount() {
        this.clearCooldown()
    },
    methods: {
        clearCooldown() {
            if (this.cooldownTimer) {
                clearInterval(this.cooldownTimer)
                this.cooldownTimer = null
            }
        },
        startCooldown(seconds = 60) {
            this.clearCooldown()
            this.cooldownSeconds = seconds
            this.cooldownTimer = setInterval(() => {
                if (this.cooldownSeconds <= 1) {
                    this.cooldownSeconds = 0
                    this.clearCooldown()
                    return
                }
                this.cooldownSeconds -= 1
            }, 1000)
        },
        validateBaseFields() {
            this.fieldErrors.steamId = !this.steamId.trim()
            this.fieldErrors.email = !this.email.trim()
            this.fieldErrors.password = validatePassword(this.password)
            return !this.fieldErrors.steamId && !this.fieldErrors.email && !this.fieldErrors.password
        },
        onCancel() {
            this.$router.push({ name: 'login' })
        },
        async onVerifyEmail() {
            this.formError = ''
            this.formSuccess = ''

            if (this.emailVerified) return

            if (this.codeSent && this.code.trim().length >= 6) {
                await this.confirmVerificationCode()
                return
            }

            if (this.cooldownSeconds > 0) return
            if (!this.validateBaseFields()) return

            this.loading = true
            try {
                await sendVerification(this.email.trim())
                this.codeSent = true
                this.formSuccess = 'Verification code sent. Check your email, then enter the code and click Verify Email again.'
                this.startCooldown(60)
            } catch (error) {
                this.formError = error.body?.message || error.message || 'Failed to send verification'
                if (error.status === 429) {
                    this.startCooldown(60)
                }
            } finally {
                this.loading = false
            }
        },
        async confirmVerificationCode() {
            if (!this.code.trim()) {
                this.formError = 'Enter the verification code'
                return
            }

            this.loading = true
            this.formError = ''
            try {
                const response = await confirmCode(this.email.trim(), this.code.trim())
                const token = response?.data?.emailVerifiedToken
                if (!token) {
                    throw new Error(response?.message || 'Verification failed')
                }

                this.emailVerifiedToken = token
                this.emailVerified = true
                this.clearCooldown()
                this.cooldownSeconds = 0
                this.formSuccess = 'Email verified. You can register now.'
            } catch (error) {
                this.formError = error.body?.message || error.message || 'Invalid verification code'
            } finally {
                this.loading = false
            }
        },
        async onRegister() {
            if (!this.emailVerified || !this.emailVerifiedToken) return

            this.formError = ''
            this.formSuccess = ''
            this.loading = true
            try {
                await register({
                    steamId: this.steamId.trim(),
                    email: this.email.trim(),
                    password: this.password,
                    emailVerifiedToken: this.emailVerifiedToken
                })
                this.$router.push({ name: 'login' })
            } catch (error) {
                this.formError = error.body?.message || error.message || 'Registration failed'
            } finally {
                this.loading = false
            }
        }
    }
}
</script>
