<template>
    <AppShell>
        <GameHeader 
            :gameId="gameId" 
            @reload="syncAndReloadAchievements"
            @completion-change="onCompletionChange"
        />

        <div class="card">
            <div class="card-body">
                <div class="d-flex flex-wrap align-items-center gap-2 mb-2">
                    <button
                        type="button"
                        class="btn btn-outline-secondary btn-sm"
                        aria-label="Filter achievements"
                        aria-controls="game-achievement-filters"
                        @click="toggleFilters"
                    >
                        <LucideIcon icon="Funnel" :size="18" />
                    </button>
                    <h2 class="h5 mb-0">Games Achievements</h2>
                    <div class="d-flex flex-wrap gap-2 ms-auto">
                        <div class="btn-group" role="group" aria-label="Achievement status">
                            <button
                                v-for="option in statusOptions"
                                :key="option.value"
                                type="button"
                                class="btn btn-sm"
                                :class="appliedFilters.status === option.value ? 'btn-primary' : 'btn-outline-secondary'"
                                :disabled="isFullyCompleted"
                                @click="setStatus(option.value)"
                            >
                                {{ option.label }}
                            </button>
                        </div>
                        <div class="btn-group" role="group" aria-label="Sort achievements">
                            <button
                                v-for="option in sortOptions"
                                :key="option.value"
                                type="button"
                                class="btn btn-sm"
                                :class="appliedFilters.sort === option.value ? 'btn-primary' : 'btn-outline-secondary'"
                                @click="setSort(option.value)"
                            >
                                {{ option.label }}
                            </button>
                        </div>
                    </div>
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
import { syncGameAchievements } from '@/services/gamesService'
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
            isFullyCompleted: false,
            filters: {
                name: '',
                status: 'locked',
                sort: 'unlockTime'
            },
            appliedFilters: {
                name: '',
                status: 'locked',
                sort: 'unlockTime'
            },
            statusOptions: [
                { value: 'locked', label: 'Locked' },
                { value: 'unlocked', label: 'Unlocked' },
                { value: 'all', label: 'All' }
            ],
            sortOptions: [
                { value: 'unlockTime', label: 'Unlock time' },
                { value: 'name', label: 'Name' },
                { value: 'global', label: 'Global %' }
            ],
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
                status: filters?.status || 'locked',
                sort: filters?.sort || 'unlockTime'
            }
            this.filters = { ...this.appliedFilters }
            this.tableKey++
        },
        setStatus(status) {
            if (this.isFullyCompleted || this.appliedFilters.status === status) {
                return
            }
            this.applyFilters({
                ...this.appliedFilters,
                status
            })
        },
        setSort(sort) {
            if (this.appliedFilters.sort === sort) {
                return
            }
            this.applyFilters({
                ...this.appliedFilters,
                sort
            })
        },
        onCompletionChange(isCompleted) {
            this.isFullyCompleted = !!isCompleted
            if (this.isFullyCompleted && this.appliedFilters.status !== 'all') {
                this.applyFilters({
                    ...this.appliedFilters,
                    status: 'all'
                })
            }
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
