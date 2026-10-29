<template>
    <AppShell>
        <GameHeader
            :gameId="gameId"
            @reload="syncAndReloadAchievements"
            @completion-change="onCompletionChange"
        />

        <div v-if="activeGoal" class="alert alert-primary d-flex flex-wrap align-items-center gap-2 mt-4 mb-0" role="status">
            <div class="me-auto">
                <strong>Active goal:</strong>
                reach {{ formatPercentage(activeGoal.percentageGoal) }}
                <span class="text-secondary">
                    · {{ activeGoal.achievementsMissing }}
                    <template v-if="activeGoal.achievementsTotal > 0">
                        / {{ activeGoal.achievementsTotal }}
                    </template>
                    missing
                </span>
            </div>
            <RouterLink :to="{ name: 'goals' }" class="btn btn-sm btn-outline-primary">
                View goals
            </RouterLink>
        </div>
        <div v-else-if="goalChecked && !isFullyCompleted" class="alert alert-secondary d-flex flex-wrap align-items-center gap-2 mt-4 mb-0" role="status">
            <span class="me-auto">No active goal for this game.</span>
            <RouterLink
                :to="{ name: 'goals-form', query: { appId: gameId } }"
                class="btn btn-sm btn-primary"
            >
                Create a goal
            </RouterLink>
        </div>

        <div class="card mt-4">
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
                        <button
                            v-if="activeGoal && canAttachSelection"
                            type="button"
                            class="btn btn-sm btn-primary"
                            :disabled="attaching || selectedRows.length === 0"
                            @click="attachSelected"
                        >
                            {{ attaching ? 'Adding…' : `Add to goal (${selectedRows.length})` }}
                        </button>
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
                <p v-if="attachError" class="text-danger small">{{ attachError }}</p>

                <GamesAchievementsTable
                    ref="achievementsTable"
                    :gameId="gameId"
                    :filters="appliedFilters"
                    :hasSelection="canAttachSelection"
                    :excludeIds="goalAchievementIds"
                    :key="tableKey"
                    @selectedRows="onSelectedRows"
                />
            </div>
        </div>
    </AppShell>
</template>

<script>
import { getUser } from '@/lib/session'
import { syncGameAchievements } from '@/services/gamesService'
import { attachGoalAchievements, getActiveGoalByGame } from '@/services/goalsService'
import { formatPercentage } from '@/utils/NumberFormat'
import AppShell from '@/components/layout/AppShell.vue'
import GamesFilters from '@/components/games/GamesFilters.vue'
import GamesAchievementsTable from '@/components/games/GamesAchievementsTable.vue'
import LucideIcon from '@/components/global/LucideIcon.vue'
import GameHeader from '@/components/games/GameHeader.vue'
import { HttpError } from '@/services/http'

export default {
    name: 'GamesComponent',
    components: {
        AppShell,
        GamesFilters,
        GamesAchievementsTable,
        LucideIcon,
        GameHeader
    },
    data() {
        return {
            gameId: null,
            tableKey: 0,
            syncError: '',
            attachError: '',
            attaching: false,
            isFullyCompleted: false,
            activeGoal: null,
            goalChecked: false,
            selectedRows: [],
            filters: {
                name: '',
                status: 'locked',
                sort: 'unlockTime',
                global: 'all'
            },
            appliedFilters: {
                name: '',
                status: 'locked',
                sort: 'unlockTime',
                global: 'all'
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
            ]
        }
    },
    computed: {
        goalAchievementIds() {
            return this.activeGoal?.achievementIds ?? []
        },
        canAttachSelection() {
            return !!this.activeGoal && this.appliedFilters.status === 'locked'
        }
    },
    methods: {
        formatPercentage,
        toggleFilters() {
            this.$refs.filters.toggle()
        },
        applyFilters(filters) {
            this.appliedFilters = {
                name: filters?.name || '',
                status: filters?.status || 'locked',
                sort: filters?.sort || 'unlockTime',
                global: filters?.global || 'all'
            }
            this.filters = { ...this.appliedFilters }
            this.selectedRows = []
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
        onSelectedRows(rows) {
            this.selectedRows = rows || []
        },
        async loadActiveGoal() {
            this.goalChecked = false
            this.activeGoal = null
            if (!this.gameId) {
                this.goalChecked = true
                return
            }
            try {
                this.activeGoal = await getActiveGoalByGame(this.gameId)
            } catch (err) {
                if (!(err instanceof HttpError && err.status === 404)) {
                    this.syncError = err?.message || 'Failed to load active goal.'
                }
                this.activeGoal = null
            } finally {
                this.goalChecked = true
            }
        },
        async attachSelected() {
            if (!this.activeGoal || this.selectedRows.length === 0) {
                return
            }
            const achievementIds = this.selectedRows
                .map((row) => Number(row.id))
                .filter((id) => Number.isFinite(id) && id > 0)
            if (achievementIds.length === 0) {
                this.attachError = 'Selected achievements are missing ids. Sync the game and try again.'
                return
            }

            this.attaching = true
            this.attachError = ''
            try {
                this.activeGoal = await attachGoalAchievements(this.activeGoal.id, achievementIds)
                this.selectedRows = []
                this.$refs.achievementsTable?.cleanSelection?.()
                this.tableKey++
            } catch (err) {
                this.attachError = err?.message || 'Failed to add achievements to the goal.'
            } finally {
                this.attaching = false
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
            await this.loadActiveGoal()
            this.tableKey++
        }
    },

    async created() {
        this.gameId = this.$route.params.id
        await this.loadActiveGoal()
    }
}
</script>
