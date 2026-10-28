<template>
    <AppShell>
        <div class="d-flex flex-wrap align-items-center gap-2 mb-4">
            <h1 class="lastica-h3 mb-0">Create a Goal</h1>
            <div class="d-flex flex-wrap gap-2 ms-auto">
                <RouterLink :to="{ name: 'goals' }" class="btn btn-outline-secondary btn-sm">
                    Back to Goals
                </RouterLink>
                <button
                    type="button"
                    class="btn btn-primary btn-sm"
                    :disabled="!canSubmit"
                    @click="submit"
                >
                    {{ submitting ? 'Creating…' : 'Create goal' }}
                </button>
            </div>
        </div>

        <p v-if="submitError" class="alert alert-danger" role="alert">{{ submitError }}</p>

        <div class="card mb-4">
            <div class="card-body">
                <div class="row g-3">
                    <div class="col-12 col-md-8">
                        <GameSearchComponent
                            v-model="appId"
                            :disabled="submitting"
                            @select="onGameSelect"
                        />
                    </div>
                </div>
            </div>
        </div>

        <div v-if="appId" class="row g-4 mb-4">
            <div class="col-12 col-lg-7">
                <div class="card h-100">
                    <div class="card-body">
                        <h2 class="h6 text-uppercase text-secondary">Game info</h2>
                        <p v-if="infoLoading" class="text-secondary small mb-0">Loading…</p>
                        <p v-else-if="infoError" class="text-danger small mb-0">{{ infoError }}</p>
                        <div v-else class="row g-3">
                            <div class="col-6">
                                <p class="text-secondary small mb-1">Total achievements</p>
                                <p class="fs-5 fw-semibold mb-0">{{ gameInfo.total }}</p>
                            </div>
                            <div class="col-6">
                                <p class="text-secondary small mb-1">Missing (locked)</p>
                                <p class="fs-5 fw-semibold mb-0">{{ gameInfo.missing }}</p>
                            </div>
                            <div class="col-12">
                                <p class="text-secondary small mb-1">Current percentage</p>
                                <p class="fs-5 fw-semibold mb-0">{{ formatPercentage(gameInfo.percentage) }}</p>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
            <div class="col-12 col-lg-5">
                <div class="card h-100">
                    <div class="card-body">
                        <label class="form-label" for="percentageGoal">Goal %</label>
                        <input
                            id="percentageGoal"
                            v-model.number="percentageGoal"
                            type="number"
                            class="form-control"
                            :class="{ 'is-invalid': percentageInvalid }"
                            min="0"
                            max="100"
                            step="0.1"
                            :disabled="submitting || infoLoading || hasSelection"
                            :readonly="hasSelection"
                        />
                        <div class="form-text">
                            <template v-if="hasSelection">
                                Auto-set from {{ selectedAchievements.length }} selected achievement(s)
                                ({{ gameInfo.unlocked }} unlocked + selection) / {{ gameInfo.total }}.
                            </template>
                            <template v-else>
                                Select achievements to auto-calculate, or set a % manually
                                (must be greater than {{ formatPercentage(gameInfo.percentage) }}).
                            </template>
                        </div>
                        <div v-if="percentageInvalid" class="invalid-feedback d-block">
                            Goal % must be above current progress and at most 100.
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <div v-if="appId" class="card mb-4">
            <div class="card-body">
                <h2 class="h6 mb-3">Locked achievements (optional)</h2>
                <p class="text-secondary small">
                    Select achievements to track for this goal. End goal % updates automatically from your selection.
                </p>
                <GoalsAchievementsTable
                    :key="appId"
                    :game-id="appId"
                    @selectedRows="onSelectedRows"
                />
            </div>
        </div>

    </AppShell>
</template>

<script>
import AppShell from '@/components/layout/AppShell.vue'
import GameSearchComponent from '@/components/goals/form/GameSearchComponent.vue'
import GoalsAchievementsTable from '@/components/goals/tables/GoalsAchievementsTable.vue'
import { createGoal } from '@/services/goalsService'
import { getGameProgress } from '@/services/statsService'
import { formatPercentage } from '@/utils/NumberFormat'

function roundPercent(value) {
    return Math.round(value * 100) / 100
}

export default {
    name: 'GoalFormComponent',
    components: {
        AppShell,
        GameSearchComponent,
        GoalsAchievementsTable
    },
    data() {
        return {
            appId: null,
            percentageGoal: null,
            selectedAchievements: [],
            submitting: false,
            submitError: '',
            infoLoading: false,
            infoError: '',
            gameInfo: {
                total: 0,
                unlocked: 0,
                missing: 0,
                percentage: 0
            }
        }
    },
    computed: {
        hasSelection() {
            return this.selectedAchievements.length > 0
        },
        percentageInvalid() {
            if (this.percentageGoal == null || this.percentageGoal === '') {
                return false
            }
            const value = Number(this.percentageGoal)
            return !Number.isFinite(value)
                || value <= this.gameInfo.percentage
                || value > 100
        },
        canSubmit() {
            return !!this.appId
                && !this.submitting
                && !this.infoLoading
                && this.percentageGoal != null
                && this.percentageGoal !== ''
                && !this.percentageInvalid
        }
    },
    created() {
        const queryAppId = Number(this.$route.query.appId)
        if (Number.isFinite(queryAppId) && queryAppId > 0) {
            this.appId = queryAppId
            this.loadGameInfo(queryAppId)
        }
    },
    methods: {
        formatPercentage,
        suggestedPercentage(current) {
            const suggested = Math.min(100, roundPercent(current + 0.01))
            if (suggested > current) {
                return suggested
            }
            return current < 100 ? 100 : null
        },
        percentageFromSelection() {
            const total = Number(this.gameInfo.total) || 0
            const unlocked = Number(this.gameInfo.unlocked) || 0
            const selected = this.selectedAchievements.length
            if (total <= 0 || selected <= 0) {
                return null
            }
            return Math.min(100, roundPercent(((unlocked + selected) / total) * 100))
        },
        syncPercentageGoal() {
            if (this.hasSelection) {
                const derived = this.percentageFromSelection()
                this.percentageGoal = derived
                return
            }
            if (this.percentageGoal == null || this.percentageGoal <= this.gameInfo.percentage) {
                this.percentageGoal = this.suggestedPercentage(this.gameInfo.percentage)
            }
        },
        onGameSelect(game) {
            this.selectedAchievements = []
            this.submitError = ''
            if (!game) {
                this.appId = null
                this.percentageGoal = null
                this.gameInfo = { total: 0, unlocked: 0, missing: 0, percentage: 0 }
                return
            }
            const appId = game.appId ?? this.appId
            this.appId = appId
            const current = Number(game.achievementsPercentage) || 0
            this.gameInfo.percentage = current
            this.percentageGoal = this.suggestedPercentage(current)
            this.loadGameInfo(appId)
        },
        async loadGameInfo(appId) {
            this.infoLoading = true
            this.infoError = ''
            try {
                const progress = await getGameProgress(appId)
                const total = Number(progress?.totalCount) || 0
                const unlocked = Number(progress?.unlockedCount) || 0
                const percentage = total > 0 ? roundPercent((unlocked / total) * 100) : 0
                this.gameInfo = {
                    total,
                    unlocked,
                    missing: Math.max(0, total - unlocked),
                    percentage
                }
                this.syncPercentageGoal()
            } catch (err) {
                this.infoError = err?.message || 'Failed to load game info.'
            } finally {
                this.infoLoading = false
            }
        },
        onSelectedRows(rows) {
            this.selectedAchievements = rows || []
            this.syncPercentageGoal()
        },
        async submit() {
            if (!this.canSubmit) {
                return
            }
            this.submitting = true
            this.submitError = ''
            try {
                const achievementIds = this.selectedAchievements
                    .map((row) => Number(row.id))
                    .filter((id) => Number.isFinite(id) && id > 0)
                await createGoal({
                    appId: Number(this.appId),
                    percentageGoal: Number(this.percentageGoal),
                    achievementIds
                })
                await this.$router.push({ name: 'goals' })
            } catch (err) {
                this.submitError = err?.message || 'Failed to create goal.'
            } finally {
                this.submitting = false
            }
        }
    }
}
</script>
