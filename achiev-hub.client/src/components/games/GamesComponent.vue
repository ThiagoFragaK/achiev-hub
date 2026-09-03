<template>
    <AppShell>
        <div class="row g-4 mb-4 align-items-start">
            <div class="col-md-3 col-lg-2">
                <ImageComponent :alt="game.name" />
            </div>

            <div class="col-md-5 col-lg-4">
                <h1 class="lastica-h3 mb-3">
                    {{ game.name }}
                </h1>
                <div class="row g-3">
                    <div class="col-sm-6">
                        <p class="text-uppercase text-secondary small mb-1">Total game hours</p>
                        <p class="fs-4 fw-semibold mb-0">{{ game.hours }}h</p>
                    </div>
                    <div class="col-sm-6">
                        <p class="text-uppercase text-secondary small mb-1">Percentage</p>
                        <p class="fs-4 fw-semibold mb-0">{{ game.percentage }}%</p>
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

                <GamesTable
                    :achievements="pagedAchievements"
                    :columns="achievementColumns"
                    :current-page="currentPage"
                    :total-pages="totalPages"
                    :per-page="perPage"
                    :total-items="demoAchievements.length"
                    @change-page="currentPage = $event"
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
import { demoAchievements, demoGames } from '@/data/demo'

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
            demoAchievements,
            currentPage: 1,
            perPage: 10,
            filters: {
                name: '',
                status: ''
            },
            achievementColumns: [
                { key: 'name', label: 'Achievement' },
                { key: 'status', label: 'Status' }
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
        game() {
            const id = String(this.$route.params.id)
            return demoGames.find((g) => g.id === id) ?? demoGames[0]
        },
        totalPages() {
            return Math.max(1, Math.ceil(this.demoAchievements.length / this.perPage))
        },
        pagedAchievements() {
            const start = (this.currentPage - 1) * this.perPage
            return this.demoAchievements.slice(start, start + this.perPage)
        }
    },
    methods: {
        toggleFilters() {
            this.$refs.filters.toggle()
        }
    }
}
</script>
