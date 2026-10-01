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

            <div v-if="preparing" class="text-center">
                <div class="alert alert-info" role="status">
                    <div class="fw-semibold mb-1">{{ prepareHeadline }}</div>
                    <div class="small">
                        We're importing your recently played games and achievements.
                        You'll be able to log in when that finishes — your full library keeps syncing afterward.
                    </div>
                    <div v-if="prepareProgress" class="small mt-2 text-secondary">
                        {{ prepareProgress }}
                    </div>
                    <div v-if="prepareError" class="small mt-2 text-danger">
                        {{ prepareError }}
                    </div>
                </div>
                <div v-if="!prepareError" class="spinner-border text-primary" role="status">
                    <span class="visually-hidden">Loading…</span>
                </div>
                <button
                    v-if="prepareError"
                    type="button"
                    class="btn btn-outline-primary mt-3"
                    :disabled="loading"
                    @click="startProvisioningPoll"
                >
                    Retry status check
                </button>
            </div>

            <form v-else @submit.prevent>
                <div class="mb-3">
                    <label class="form-label fw-semibold" for="regSteamId">Steam ID</label>
                    <input
                        id="regSteamId"
                        v-model="steamId"
                        type="text"
                        class="form-control"
                        :class="{ 'is-invalid': fieldErrors.steamId }"
                        :disabled="loading || fieldsLocked || steamValidated"
                        autocomplete="username"
                    />
                    <div v-if="fieldErrors.steamId" class="invalid-feedback d-block">Required</div>
                    <div v-if="steamPersona" class="form-text text-success">
                        Steam profile: {{ steamPersona }}
                    </div>
                </div>

                <div class="mb-3">
                    <label class="form-label fw-semibold" for="regEmail">Email</label>
                    <input
                        id="regEmail"
                        v-model="email"
                        type="email"
                        class="form-control"
                        :class="{ 'is-invalid': fieldErrors.email }"
                        :disabled="loading || fieldsLocked || !steamValidated"
                        autocomplete="email"
                    />
                    <div v-if="fieldErrors.email" class="invalid-feedback d-block">Required</div>
                </div>

                <div class="mb-3">
                    <label class="form-label fw-semibold" for="regPassword">Password</label>
                    <div class="position-relative">
                        <input
                            id="regPassword"
                            v-model="password"
                            :type="showPassword ? 'text' : 'password'"
                            class="form-control pe-5"
                            :class="{ 'is-invalid': fieldErrors.password }"
                            :disabled="loading || fieldsLocked || !steamValidated"
                            autocomplete="new-password"
                        />
                        <button
                            type="button"
                            class="btn btn-link text-secondary position-absolute top-50 end-0 translate-middle-y px-3 py-0 border-0"
                            :disabled="loading || fieldsLocked || !steamValidated"
                            :aria-label="showPassword ? 'Hide password' : 'Show password'"
                            :aria-pressed="showPassword"
                            @click="showPassword = !showPassword"
                        >
                            <LucideIcon :icon="showPassword ? 'EyeOff' : 'Eye'" :size="18" />
                        </button>
                    </div>
                    <div class="form-text">
                        At least 12 characters, with uppercase, number, and special character.
                    </div>
                    <div v-if="fieldErrors.password" class="invalid-feedback d-block">
                        {{ fieldErrors.password }}
                    </div>
                </div>

                <div class="mb-3">
                    <label class="form-label fw-semibold" for="regConfirmPassword">
                        Confirm password
                    </label>
                    <div class="position-relative">
                        <input
                            id="regConfirmPassword"
                            v-model="confirmPassword"
                            :type="showConfirmPassword ? 'text' : 'password'"
                            class="form-control pe-5"
                            :class="{ 'is-invalid': fieldErrors.confirmPassword }"
                            :disabled="loading || fieldsLocked || !steamValidated"
                            autocomplete="new-password"
                        />
                        <button
                            type="button"
                            class="btn btn-link text-secondary position-absolute top-50 end-0 translate-middle-y px-3 py-0 border-0"
                            :disabled="loading || fieldsLocked || !steamValidated"
                            :aria-label="
                                showConfirmPassword
                                    ? 'Hide confirm password'
                                    : 'Show confirm password'
                            "
                            :aria-pressed="showConfirmPassword"
                            @click="showConfirmPassword = !showConfirmPassword"
                        >
                            <LucideIcon
                                :icon="showConfirmPassword ? 'EyeOff' : 'Eye'"
                                :size="18"
                            />
                        </button>
                    </div>
                    <div v-if="fieldErrors.confirmPassword" class="invalid-feedback d-block">
                        {{ fieldErrors.confirmPassword }}
                    </div>
                </div>

                <div v-if="!bypassEmailVerification && codeSent" class="mb-3">
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

                <div v-if="bypassEmailVerification" class="alert alert-warning py-2" role="alert">
                    Development mode: email verification is skipped.
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
                        v-if="!steamValidated"
                        type="button"
                        class="btn btn-primary"
                        :disabled="loading"
                        @click="onValidateSteam"
                    >
                        {{ loading ? 'Checking Steam…' : 'Validate Steam ID' }}
                    </button>

                    <button
                        v-if="steamValidated && !bypassEmailVerification"
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
                        v-if="steamValidated"
                        type="button"
                        class="btn btn-success"
                        :disabled="loading || (!bypassEmailVerification && !emailVerified)"
                        @click="onRegister"
                    >
                        {{ loading ? 'Registering…' : 'Register' }}
                    </button>
                </div>
            </form>
        </div>
    </div>
</template>

<script>
import {
    confirmCode,
    register,
    sendVerification,
    validateSteam
} from '@/services/authService'
import { getProvisioningStatus } from '@/services/syncService'
import LucideIcon from '@/components/global/LucideIcon.vue'
import { validatePassword } from '@/utils/PasswordHelper'

export default {
    name: 'RegisterComponent',
    components: {
        LucideIcon
    },
    data() {
        return {
            bypassEmailVerification: import.meta.env.DEV,
            steamId: '',
            email: '',
            password: '',
            confirmPassword: '',
            showPassword: false,
            showConfirmPassword: false,
            code: '',
            codeSent: false,
            emailVerified: false,
            emailVerifiedToken: '',
            steamValidated: false,
            steamPersona: '',
            preparing: false,
            prepareHeadline: 'Preparing your account…',
            prepareProgress: '',
            prepareError: '',
            prepareTimer: null,
            cooldownSeconds: 0,
            cooldownTimer: null,
            loading: false,
            formError: '',
            formSuccess: '',
            fieldErrors: {
                steamId: false,
                email: false,
                password: '',
                confirmPassword: ''
            }
        }
    },
    computed: {
        fieldsLocked() {
            return !this.bypassEmailVerification && this.emailVerified
        }
    },
    beforeUnmount() {
        this.clearCooldown()
        this.clearPrepareTimer()
    },
    methods: {
        clearPrepareTimer() {
            if (this.prepareTimer) {
                clearInterval(this.prepareTimer)
                this.prepareTimer = null
            }
        },
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
        pipelineCopy(sync, statusLabel) {
            const synced = sync?.gamesSynced ?? 0
            const total = sync?.gamesTotal ?? 0
            const stage = sync?.pipelineStageLabel || ''
            const progress = total > 0 ? ` (${synced}/${total})` : ''

            if (statusLabel === 'FirstSync' || stage === 'None' || !stage) {
                return {
                    headline: 'Importing recently played games…',
                    detail: 'Importing your recently played games…'
                }
            }

            switch (stage) {
                case 'RecentAchievements':
                    return {
                        headline: 'Syncing recent achievements…',
                        detail: `Syncing achievements for recent games${progress}…`
                    }
                case 'FullLibrary':
                    return {
                        headline: 'Importing full Steam library…',
                        detail: 'Importing your full Steam library…'
                    }
                case 'FullAchievements':
                    return {
                        headline: 'Syncing library achievements…',
                        detail: `Syncing library achievements${progress}…`
                    }
                case 'Done':
                    return {
                        headline: 'Almost ready…',
                        detail: 'Finishing up…'
                    }
                default:
                    return {
                        headline: 'Preparing your account…',
                        detail: total > 0 ? `Syncing: ${synced} of ${total}` : 'Syncing your Steam data…'
                    }
            }
        },
        startProvisioningPoll() {
            this.prepareError = ''
            this.clearPrepareTimer()
            const poll = async () => {
                try {
                    const status = await getProvisioningStatus(this.steamId.trim())
                    const sync = status?.sync
                    const syncStatus = sync?.status
                    const copy = this.pipelineCopy(sync, status?.statusLabel)

                    if (syncStatus === 'Failed') {
                        this.prepareError =
                            sync?.lastError || 'Sync failed. You can retry the status check.'
                        this.prepareHeadline = 'Sync failed'
                        this.clearPrepareTimer()
                        return
                    }

                    this.prepareHeadline = copy.headline
                    this.prepareProgress = copy.detail
                    if (sync?.lastError) {
                        this.prepareProgress = `${copy.detail} (${sync.lastError})`
                    }

                    if (status?.statusLabel === 'Active' || status?.isReady) {
                        this.clearPrepareTimer()
                        this.$router.push({
                            name: 'login',
                            query: { steamId: this.steamId.trim(), ready: '1' }
                        })
                    }
                } catch {
                    // Keep polling; worker may still be starting.
                }
            }
            poll()
            this.prepareTimer = setInterval(poll, 3000)
        },
        validateBaseFields() {
            this.fieldErrors.steamId = !this.steamId.trim()
            this.fieldErrors.email = !this.email.trim()
            this.fieldErrors.password = validatePassword(this.password)

            if (!this.confirmPassword) {
                this.fieldErrors.confirmPassword = 'Required'
            } else if (this.confirmPassword !== this.password) {
                this.fieldErrors.confirmPassword = 'Passwords do not match.'
            } else {
                this.fieldErrors.confirmPassword = ''
            }

            return (
                !this.fieldErrors.steamId &&
                !this.fieldErrors.email &&
                !this.fieldErrors.password &&
                !this.fieldErrors.confirmPassword
            )
        },
        onCancel() {
            this.$router.push({ name: 'login' })
        },
        async onValidateSteam() {
            this.formError = ''
            this.formSuccess = ''
            this.fieldErrors.steamId = !this.steamId.trim()
            if (this.fieldErrors.steamId) return

            this.loading = true
            try {
                const response = await validateSteam(this.steamId.trim())
                this.steamValidated = true
                this.steamPersona = response?.data?.personaName || ''
                const isPublic = response?.data?.isLibraryPublic !== false
                this.formSuccess = isPublic
                    ? 'Steam ID looks good. Continue with email and password.'
                    : 'Steam ID is valid, but the profile is private. You can register; library sync stays off until game details are public.'
            } catch (error) {
                this.steamValidated = false
                this.steamPersona = ''
                this.formError = error.body?.message || error.message || 'Steam validation failed'
            } finally {
                this.loading = false
            }
        },
        async onVerifyEmail() {
            this.formError = ''
            this.formSuccess = ''

            if (this.bypassEmailVerification || this.emailVerified) return

            if (this.codeSent && this.code.trim().length >= 6) {
                await this.confirmVerificationCode()
                return
            }

            if (this.cooldownSeconds > 0) return
            if (!this.steamValidated) {
                this.formError = 'Validate your Steam ID first.'
                return
            }
            if (!this.validateBaseFields()) return

            this.loading = true
            try {
                await sendVerification(this.email.trim(), this.steamId.trim())
                this.codeSent = true
                this.formSuccess =
                    'Verification code sent. Check your email, then enter the code and confirm.'
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
            if (!this.steamValidated) {
                this.formError = 'Validate your Steam ID first.'
                return
            }
            if (!this.bypassEmailVerification && (!this.emailVerified || !this.emailVerifiedToken)) {
                return
            }
            if (!this.validateBaseFields()) return

            this.formError = ''
            this.formSuccess = ''
            this.loading = true
            try {
                const response = await register({
                    steamId: this.steamId.trim(),
                    email: this.email.trim(),
                    password: this.password,
                    emailVerifiedToken: this.bypassEmailVerification
                        ? ''
                        : this.emailVerifiedToken
                })
                const data = response?.data ?? response
                if (data?.sync?.enqueued === false) {
                    this.preparing = true
                    this.prepareError =
                        data.sync.lastError ||
                        'Failed to enqueue first sync. You can still try logging in shortly.'
                    return
                }

                this.preparing = true
                this.startProvisioningPoll()
            } catch (error) {
                this.formError = error.body?.message || error.message || 'Registration failed'
            } finally {
                this.loading = false
            }
        }
    }
}
</script>
