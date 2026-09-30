<template>
    <div
        v-if="showPrivateBanner"
        class="alert alert-warning border-0 rounded-0 mb-0 py-2 text-center"
        role="status"
    >
        Your Steam profile is private, so we can’t sync your library.
        Make game details public on Steam, then sync or log in again.
    </div>
    <div
        v-else-if="showSyncBanner"
        class="alert alert-info border-0 rounded-0 mb-0 py-2 text-center"
        role="status"
    >
        <span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>
        <template v-if="isUpdating">
            Syncing {{ gamesSynced }}/{{ gamesTotal || '…' }} games
            <span v-if="coverageText" class="small ms-1 text-secondary">{{ coverageText }}</span>
        </template>
        <template v-else-if="isPartial">
            Library partially imported — {{ gamesSynced }}/{{ gamesTotal || '…' }} games so far.
            Stats may show an incomplete badge until the full sync finishes.
        </template>
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
            isPartial: false,
            steamLibraryPublic: true,
            coverageText: '',
            gamesSynced: 0,
            gamesTotal: 0,
            pollTimer: null
        }
    },
    computed: {
        showPrivateBanner() {
            const user = getUser()
            return !!user && user.role !== 'guest' && this.steamLibraryPublic === false
        },
        showSyncBanner() {
            const user = getUser()
            return (
                !!user &&
                user.role !== 'guest' &&
                this.steamLibraryPublic !== false &&
                (this.isUpdating || this.isPartial)
            )
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
                    this.steamLibraryPublic = status?.steamLibraryPublic !== false
                    this.isUpdating = !!status?.isUpdating
                    this.isPartial = !!status?.isPartial || status?.sync?.status === 'partial'
                    const sync = status?.sync
                    this.gamesSynced =
                        sync?.gamesSynced ?? status?.syncedWithStats ?? 0
                    this.gamesTotal =
                        sync?.gamesTotal ?? status?.ownedWithStats ?? 0
                    const synced = status?.syncedWithStats ?? this.gamesSynced
                    const owned = status?.ownedWithStats ?? this.gamesTotal
                    this.coverageText =
                        owned > 0 ? `(${synced}/${owned} with achievements)` : ''
                } catch {
                    this.isUpdating = false
                    this.isPartial = false
                }
            }

            poll()
            this.pollTimer = setInterval(poll, 5000)
        }
    }
}
</script>
