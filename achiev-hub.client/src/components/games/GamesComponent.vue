<template>
    <AppShell>
        <GameHeader 
            :gameId="gameId" 
            @reload="syncAndReloadAchievements"
        />

        <div class="card">
            <div class="card-body">
                <div class="d-flex gap-3 mb-2">
                    <button
                        type="button"
                        class="btn btn-outline-secondary btn-sm"
                        aria-label="Filter achievements"
                        aria-controls="game-achievement-filters"
                        @click="toggleFilters"
                    >
                        <LucideIcon icon="Funnel" :size="18" />
                    </button>
                    <h2 class="h5">Games Achievements</h2>
                </div>

                <GamesFilters
                    ref="filters"
                    v-model="filters"
                    collapse-id="game-achievement-filters"
                    @apply-filters="applyFilters"
                />

                <p v-if="syncError" class="text-danger small">{{ syncError }}</p>

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
import { syncGameAchievements } from '@/services/gamesService';
import AppShell from '@/components/layout/AppShell.vue'
import GameAchievementsGraph from '@/components/games/graphs/GameAchievementsGraph.vue'
import GamesFilters from '@/components/games/GamesFilters.vue'
import GamesAchievementsTable from '@/components/games/GamesAchievementsTable.vue'
import ImageComponent from '@/components/global/ImageComponent.vue'
import LucideIcon from '@/components/global/LucideIcon.vue'
import GameHeader from '@/components/games/GameHeader.vue'

export default {
    name: 'GamesComponent',
    components: {
        AppShell,
        GameAchievementsGraph,
        GamesFilters,
        GamesAchievementsTable,
        ImageComponent,
        LucideIcon,
        GameHeader
    },
    data() {
        return {
            gameId: null,
            gameUpdate: 0,
            tableKey: 0,
            syncError: '',
            isLoading: false,
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
        }
    },
    computed: {
        progressData() {
            return [
                {
                    label: 'Unlocked',
                    data: this.progressPercentages,
                    color: '#0d6efd',
                    fill: true
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
    },
    async created() {
        this.gameId = this.$route.params.id;
    }
}
</script>
