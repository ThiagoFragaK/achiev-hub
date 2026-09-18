<template>
    <AppShell>
        <p v-if="loadingDetails" class="text-secondary mb-4">Loading game details…</p>
        <p v-else-if="detailsError" class="text-danger mb-4">{{ detailsError }}</p>

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
                    :disabled="loadingDetails || loadingAchievements"
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
                />

                <p v-if="loadingAchievements" class="text-secondary mb-0">Loading achievements…</p>
                <p v-else-if="achievementsError" class="text-danger mb-0">{{ achievementsError }}</p>
                <GamesTable
                    v-else
                    :achievements="achievements"
                    :columns="achievementColumns"
                    :current-page="currentPage"
                    :total-pages="totalPages"
                    :per-page="perPage"
                    :total-items="totalCount"
                    @change-page="onPageChange"
                />
            </div>
        </div>
    </AppShell>
</template>

<script>
import AppShell from '@/components/layout/AppShell.vue'
import GameAchievementsGraph from '@/components/games/GameAchievementsGraph.vue'
import GamesFilters from '@/components/games/GamesFilters.vue'
import GamesTable from '@/components/games/GamesTable.vue'
import ImageComponent from '@/components/global/ImageComponent.vue'
import LucideIcon from '@/components/global/LucideIcon.vue'
import { getSessionSteamId } from '@/lib/steam'
import { getAchievements, getGameDetails } from '@/services/gamesService'

export default {
    name: 'GamesComponent',
    components: {
        AppShell,
        GameAchievementsGraph,
        GamesFilters,
        GamesTable,
        ImageComponent,
        LucideIcon
    },
    data() {
        return {
            game: {
                gameName: '',
                gameImage: '',
                developers: '',
                publishers: ''
            },
            achievements: [],
            loadingDetails: false,
            loadingAchievements: false,
            detailsError: '',
            achievementsError: '',
            currentPage: 1,
            perPage: 25,
            totalPages: 1,
            totalCount: 0,
            filters: {
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
    computed: {
        appId() {
            return this.$route.params.id
        }
    },
    watch: {
        appId: {
            immediate: true,
            handler() {
                this.currentPage = 1
                this.reload()
            }
        }
    },
    methods: {
        toggleFilters() {
            this.$refs.filters.toggle()
        },
        async onPageChange(page) {
            this.currentPage = page
            await this.loadAchievements()
        },
        async reload() {
            await Promise.all([this.loadDetails(), this.loadAchievements()])
        },
        async loadDetails() {
            const appId = this.appId
            if (!appId) {
                this.detailsError = 'Game id is missing.'
                return
            }

            this.loadingDetails = true
            this.detailsError = ''
            try {
                const details = await getGameDetails(appId)
                this.game = {
                    gameName: details?.gameName || '',
                    gameImage: details?.gameImage || '',
                    developers: details?.developers || '',
                    publishers: details?.publishers || ''
                }
                await this.loadAchievements();
            } catch (err) {
                this.game = {
                    gameName: '',
                    gameImage: '',
                    developers: '',
                    publishers: ''
                }
                this.detailsError = err?.message || 'Failed to load game details.'
            } finally {
                this.loadingDetails = false                
            }
        },
        async loadAchievements() {
            const steamId = getSessionSteamId()
            const appId = this.appId
            if (!steamId) {
                this.achievementsError = 'Steam ID is missing from your session.'
                this.achievements = []
                this.totalCount = 0
                this.totalPages = 1
                return
            }
            if (!appId) {
                this.achievementsError = 'Game id is missing.'
                return
            }

            this.loadingAchievements = true
            this.achievementsError = ''
            try {
                const result = await getAchievements(steamId, appId, this.currentPage, this.perPage)
                this.achievements = result?.data ?? []
                this.currentPage = result?.currentPage ?? this.currentPage
                this.totalPages = Math.max(1, result?.lastPage ?? 1)
                this.perPage = result?.perPage ?? this.perPage
                this.totalCount = result?.totalCount ?? this.achievements.length
            } catch (err) {
                this.achievements = []
                this.totalCount = 0
                this.totalPages = 1
                this.achievementsError = err?.message || 'Failed to load achievements.'
            } finally {
                this.loadingAchievements = false
            }
        }
    }
}
</script>
