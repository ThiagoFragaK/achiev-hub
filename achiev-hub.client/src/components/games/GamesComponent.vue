<template>
    <AppShell>
        <p v-if="isLoading" class="text-secondary mb-4">Loading game details…</p>
        <p v-else-if="error" class="text-danger mb-4">{{ error }}</p>

        <div v-else class="row g-4 mb-4 align-items-start">
            <div class="col-md-3 col-lg-2">
                <ImageComponent
                    :src="game.gameImage || ''"
                    :alt="game.gameName || 'Game'"
                    ratio="16x9"
                />
            </div>

            <div class="col-md-5 col-lg-4">
                <h1 class="lastica-h3 mb-3">
                    {{ game.gameName || 'Unknown game' }}
                </h1>
                <div class="row g-3">
                    <div class="col-sm-6">
                        <p class="text-uppercase text-secondary small mb-1">Developers</p>
                        <p class="fs-6 fw-semibold mb-0">{{ game.developers || '—' }}</p>
                    </div>
                    <div class="col-sm-6">
                        <p class="text-uppercase text-secondary small mb-1">Publishers</p>
                        <p class="fs-6 fw-semibold mb-0">{{ game.publishers || '—' }}</p>
                    </div>
                </div>
            </div>

            <div class="col-md-4 col-lg-5">
                <GameAchievementsGraph
                    title="Percentage per year."
                    :labels="percentageLabels"
                    :datasets="percentageData"
                    :max="40"
                    :height="160"
                />
            </div>

            <div class="col-lg-1 text-lg-end">
                <button
                    type="button"
                    class="btn btn-outline-secondary btn-sm"
                    aria-label="Sync game data"
                    :disabled="isLoading"
                    @click="reload"
                >
                    <LucideIcon icon="RefreshCw" :size="18" />
                </button>
            </div>
        </div>

        <div class="card">
            <div class="card-body">
                <div class="d-flex align-items-center justify-content-between gap-3 mb-3">
                    <h2 class="h5 mb-0">Games Achievements</h2>
                    <button
                        type="button"
                        class="btn btn-outline-secondary btn-sm"
                        aria-label="Filter achievements"
                        aria-controls="game-achievement-filters"
                        @click="toggleFilters"
                    >
                        <LucideIcon icon="Funnel" :size="18" />
                    </button>
                </div>

                <GamesFilters
                    ref="filters"
                    v-model="filters"
                    collapse-id="game-achievement-filters"
                    @apply-filters="applyFilters"
                />

                <p v-if="syncError" class="text-danger small mb-3">{{ syncError }}</p>

                <GamesAchievementsTable
                    :gameId="gameId"
                    :filters="appliedFilters"
                    :key="tableKey"
                />
            </div>
        </div>
    </AppShell>
</template>

<script>
import { getUser } from '@/lib/session'
import { getGameDetails, syncGameAchievements } from '@/services/gamesService';
import AppShell from '@/components/layout/AppShell.vue'
import GameAchievementsGraph from '@/components/games/graphs/GameAchievementsGraph.vue'
import GamesFilters from '@/components/games/GamesFilters.vue'
import GamesAchievementsTable from '@/components/games/GamesAchievementsTable.vue'
import ImageComponent from '@/components/global/ImageComponent.vue'
import LucideIcon from '@/components/global/LucideIcon.vue'

export default {
    name: 'GamesComponent',
    components: {
        AppShell,
        GameAchievementsGraph,
        GamesFilters,
        GamesAchievementsTable,
        ImageComponent,
        LucideIcon
    },
    data() {
        return {
            gameId: null,
            gameUpdate: 0,
            tableKey: 0,
            syncError: '',
            isLoading: false,
            game: {
                gameName: '',
                gameImage: '',
                developers: '',
                publishers: ''
            },
            filters: {
                name: '',
                status: ''
            },
            appliedFilters: {
                name: '',
                status: ''
            },
            achievementColumns: [
                { key: 'icon', label: '' },
                { key: 'name', label: 'Achievement' },
                { key: 'description', label: 'Description' },
                { key: 'unlocked', label: 'Unlocked' }
            ],
            percentageLabels: ['ITEM 1', 'ITEM 2', 'ITEM 3', 'ITEM 4', 'ITEM 5'],
            percentageData: [
                {
                    label: 'Percentage',
                    data: [8, 14, 20, 28, 36],
                    color: '#0d6efd'
                }
            ]
        }
    },
    methods: {
        toggleFilters() {
            this.$refs.filters.toggle()
        },
        applyFilters(filters) {
            this.appliedFilters = {
                name: filters?.name || '',
                status: filters?.status || ''
            }
            this.tableKey++
        },
        async reload() {
            await this.syncAndReloadAchievements()
        },
        async syncAndReloadAchievements() {
            this.syncError = ''
            const user = getUser()
            if (user && user.role !== 'guest' && this.gameId) {
                try {
                    await syncGameAchievements(this.gameId)
                } catch (err) {
                    this.syncError = err?.message || 'Failed to sync achievements.'
                }
            }
            this.tableKey++
        },
        async getGameDetails() {
            if (!this.gameId) {
                this.error = 'Game id is missing.'
                return
            }

            this.isLoading = true;
            try {
                const details = await getGameDetails(this.gameId)
                this.game = {
                    gameName: details?.gameName || '',
                    gameImage: details?.gameImage || '',
                    developers: details?.developers || '',
                    publishers: details?.publishers || ''
                }
                await this.syncAndReloadAchievements()
            } catch (err) {
                this.game = {
                    gameName: '',
                    gameImage: '',
                    developers: '',
                    publishers: ''
                }
                this.error = err?.message || 'Failed to load game details.'
            } finally {
                this.isLoading = false                
            }
        },
    },
    async created() {
        this.gameId = this.$route.params.id;
        await this.getGameDetails();
    }
}
</script>
