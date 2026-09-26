<template>
    <div class="min-vh-100">
        <AppHeader />
        <div
            v-if="showSyncBanner"
            class="alert alert-info border-0 rounded-0 mb-0 py-2 text-center"
            role="status"
        >
            <span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>
            Updating your library…
            <span v-if="coverageText" class="small ms-1 text-secondary">{{ coverageText }}</span>
        </div>
        <main class="container py-4">
            <slot />
        </main>
    </div>
</template>

<script>
import AppHeader from './AppHeader.vue'
import { getUser } from '@/lib/session'
import { getSyncStatus } from '@/services/syncService'

export default {
    name: 'AppShell',
    components: {
        AppHeader
    },
    data() {
        return {
            isUpdating: false,
            coverageText: '',
            pollTimer: null
        }
    },
    computed: {
        showSyncBanner() {
            const user = getUser()
            return !!user && user.role !== 'guest' && this.isUpdating
        }
    },
    mounted() {
        this.startSyncPolling()
    },
    beforeUnmount() {
        this.clearPoll()
    },
    methods: {
        clearPoll() {
            if (this.pollTimer) {
                clearInterval(this.pollTimer)
                this.pollTimer = null
            }
        },
        startSyncPolling() {
            const user = getUser()
            if (!user || user.role === 'guest') {
                return
            }

            const poll = async () => {
                try {
                    const status = await getSyncStatus()
                    this.isUpdating = !!status?.isUpdating
                    const synced = status?.syncedWithStats ?? 0
                    const owned = status?.ownedWithStats ?? 0
                    this.coverageText =
                        owned > 0 ? `(${synced}/${owned} games with achievements)` : ''
                } catch {
                    this.isUpdating = false
                }
            }

            poll()
            this.pollTimer = setInterval(poll, 5000)
        }
    }
}
</script>
