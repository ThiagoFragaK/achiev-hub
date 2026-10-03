<template>
    <div
        v-if="showSyncBanner"
        class="alert alert-info border-0 rounded-0 mb-0 py-2 text-center"
        role="status"
    >
        <span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>
        {{ bannerText }}
    </div>
</template>

<script>
import { getUser } from '@/lib/session'
import { getSyncStatus } from '@/services/syncService'

export default {
    name: 'GameSyncInfoComponent',
    data() {
        return {
            isUpdating: false,
            gamesSynced: 0,
            gamesTotal: 0,
            pipelineStageLabel: '',
            pollTimer: null
        }
    },
    computed: {
        showSyncBanner() {
            const user = getUser()
            return !!user && user.role !== 'guest' && this.isUpdating
        },
        bannerText() {
            const synced = this.gamesSynced
            const total = this.gamesTotal
            const progress =
                total > 0 ? `${synced}/${total}` : synced > 0 ? `${synced}/…` : null

            switch (this.pipelineStageLabel) {
                case 'RecentAchievements':
                    return progress
                        ? `Syncing achievements for recent games (${progress})…`
                        : 'Syncing achievements for recent games…'
                case 'FullLibrary':
                    return 'Importing full Steam library…'
                case 'FullAchievements':
                    return progress
                        ? `Syncing library achievements (${progress})…`
                        : 'Syncing library achievements…'
                case 'Done':
                    return 'Finishing sync…'
                default:
                    return progress
                        ? `Syncing ${progress} games…`
                        : 'Syncing your Steam library…'
            }
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
                    const sync = status?.sync
                    this.gamesSynced = sync?.gamesSynced ?? 0
                    this.gamesTotal = sync?.gamesTotal ?? 0
                    this.pipelineStageLabel = sync?.pipelineStageLabel || ''
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
