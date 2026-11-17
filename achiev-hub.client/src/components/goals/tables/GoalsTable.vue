<template>
    <div>
        <TabsComponent v-model="statusFilter" :tabs="statusTabs">
            <template #append>
                <button
                    type="button"
                    class="btn btn-outline-light btn-sm"
                    aria-label="Filter goals"
                    aria-controls="goals-filters"
                    @click="toggleFilters"
                >
                    <LucideIcon icon="Funnel" :size="18" />
                </button>
            </template>
            <GoalsFilters
                ref="GoalsFilters"
                :filters="filters"
                :games="games"
                @apply-filters="applyFilters"
            />
            <div v-if="error" class="alert alert-danger" role="alert">{{ error }}</div>
            <TableComponent
                :data="table.data"
                :columns="table.columns"
                :isLoading="table.isLoading"
                :pagination="table.pagination"
                @change-page="onPageChange"
            >
                <template #cell-game="{ data }">
                    <div class="d-flex align-items-center gap-2">
                        <img
                            v-if="data.row.gameIcon"
                            :src="data.row.gameIcon"
                            :alt="data.row.gameName"
                            width="32"
                            height="32"
                            class="rounded"
                        />
                        <RouterLink
                            :to="{ name: 'game-detail', params: { id: data.row.appId } }"
                            class="fw-medium"
                        >
                            {{ data.row.gameName }}
                        </RouterLink>
                    </div>
                </template>
                <template #cell-achievements="{ data }">
                    <div class="mt-2 me-2 d-inline-block">
                        {{ data.row.achievementsMissing }}
                        <span v-if="data.row.achievementsTotal > 0" class="text-secondary">
                            / {{ data.row.achievementsTotal }}
                        </span>
                    </div>
                    <button
                        type="button"
                        class="btn btn-sm btn-outline-warning"
                        aria-label="Show goal achievements"
                        @click="openAchievementsModal(data.row)"
                    >
                        Show
                    </button>
                </template>
                <template #cell-status="{ data }">
                    <span class="badge" :class="statusBadgeClass(data.row.status)">
                        {{ statusLabel(data.row.status) }}
                    </span>
                </template>
                <template #cell-completion="{ data }">
                    {{ formatPercentage(completionOf(data.row)) }}
                </template>
                <template #cell-endGoal="{ data }">
                    {{ formatPercentage(data.row.percentageGoal) }}
                </template>
                <template #cell-actions="{ data }">
                    <div v-if="isActive(data.row.status)" class="d-flex justify-content-center gap-1">
                        <button
                            type="button"
                            class="btn btn-sm btn-outline-warning"
                            :disabled="actionLoadingId === data.row.id"
                            @click="onAbandon(data.row)"
                        >
                            Abandon
                        </button>
                    </div>
                    <div v-else-if="isAbandoned(data.row.status)" class="d-flex justify-content-center gap-1">
                        <button
                            type="button"
                            class="btn btn-sm btn-outline-info"
                            :disabled="actionLoadingId === data.row.id"
                            @click="onReopen(data.row)"
                        >
                            Reopen
                        </button>
                    </div>
                    <span v-else class="text-secondary">—</span>
                </template>
            </TableComponent>
        </TabsComponent>

        <GoalAchievementsModal
            ref="achievementsModal"
            @saved="onAchievementsSaved"
        />

        <ConfirmModal
            id="abandon-goal-confirm"
            ref="abandonModal"
            title="Abandon goal"
            :message="abandonMessage"
            confirm-text="Abandon"
            cancel-text="Cancel"
            confirm-variant="warning"
            :isLoading="actionKind === 'abandon' && !!actionLoadingId"
            @confirm="confirmAbandon"
            @cancel="pendingAbandonRow = null"
        />

        <ConfirmModal
            id="reopen-goal-confirm"
            ref="reopenModal"
            title="Reopen goal"
            :message="reopenMessage"
            confirm-text="Reopen"
            cancel-text="Cancel"
            confirm-variant="primary"
            :isLoading="actionKind === 'reopen' && !!actionLoadingId"
            @confirm="confirmReopen"
            @cancel="pendingReopenRow = null"
        />
    </div>
</template>

<script>
import TableComponent from '@/components/global/TableComponent.vue'
import TabsComponent from '@/components/global/TabsComponent.vue'
import ConfirmModal from '@/components/global/ConfirmModal.vue'
import LucideIcon from '@/components/global/LucideIcon.vue'
import GoalsFilters from '@/components/goals/GoalsFilters.vue'
import GoalAchievementsModal from '@/components/goals/modals/GoalAchievementsModal.vue'
import { abandonGoal, getGoalFilterGames, getGoals, reopenGoal } from '@/services/goalsService'
import { formatPercentage } from '@/utils/NumberFormat'
import {
    applyPagedResult,
    createPagination,
    resetPagination,
    setPage
} from '@/utils/PaginationHelper'

const STATUS = {
    Active: 0,
    Completed: 1,
    Abandoned: 2
}

const STATUS_BY_FILTER = {
    active: STATUS.Active,
    completed: STATUS.Completed,
    abandoned: STATUS.Abandoned
}

function completionOf(row) {
    const total = Number(row?.achievementsTotal) || 0
    if (total <= 0) {
        return 0
    }
    const missing = Number(row?.achievementsMissing) || 0
    const unlocked = Math.max(0, total - missing)
    return (unlocked / total) * 100
}

export default {
    name: 'GoalsTable',
    components: {
        TableComponent,
        TabsComponent,
        ConfirmModal,
        GoalAchievementsModal,
        GoalsFilters,
        LucideIcon
    },
    data() {
        return {
            error: '',
            statusFilter: 'active',
            statusTabs: [
                { name: 'active', label: 'Active' },
                { name: 'completed', label: 'Completed' },
                { name: 'abandoned', label: 'Abandoned' }
            ],
            filters: {
                appId: '',
                minCompletion: '',
                minEndGoal: ''
            },
            games: [],
            actionLoadingId: null,
            actionKind: null,
            pendingAbandonRow: null,
            pendingReopenRow: null,
            table: {
                isLoading: true,
                data: [],
                columns: [
                    { key: 'game', label: 'Game' },
                    { key: 'achievements', label: 'Achievements' },
                    { key: 'status', label: 'Status' },
                    { key: 'completion', label: 'Completion percentage' },
                    { key: 'endGoal', label: 'End goal' },
                    { key: 'actions', label: '' }
                ],
                pagination: createPagination({ perPage: 15 })
            }
        }
    },
    computed: {
        abandonMessage() {
            const name = this.pendingAbandonRow?.gameName
            return name
                ? `Abandon the active goal for ${name}? You can reopen it later.`
                : 'Abandon this active goal? You can reopen it later.'
        },
        reopenMessage() {
            const name = this.pendingReopenRow?.gameName
            return name
                ? `Reopen the abandoned goal for ${name}?`
                : 'Reopen this abandoned goal?'
        }
    },
    watch: {
        statusFilter() {
            this.table.pagination.currentPage = 1
            this.loadFilterGames()
            this.load()
        }
    },
    async created() {
        await Promise.all([this.loadFilterGames(), this.load()])
    },
    methods: {
        formatPercentage,
        completionOf,
        toggleFilters() {
            this.$refs.GoalsFilters?.toggleFilters()
        },
        applyFilters(filters) {
            this.filters = { ...filters }
            this.table.pagination.currentPage = 1
            this.load()
        },
        async onPageChange(page) {
            if (!setPage(this.table.pagination, page)) {
                return
            }
            await this.load()
        },
        isActive(status) {
            return status === STATUS.Active || status === 'Active'
        },
        isAbandoned(status) {
            return status === STATUS.Abandoned || status === 'Abandoned'
        },
        statusLabel(status) {
            if (status === STATUS.Completed || status === 'Completed') return 'Completed'
            if (status === STATUS.Abandoned || status === 'Abandoned') return 'Abandoned'
            return 'Active'
        },
        statusBadgeClass(status) {
            if (status === STATUS.Completed || status === 'Completed') return 'text-bg-success'
            if (status === STATUS.Abandoned || status === 'Abandoned') return 'text-bg-secondary'
            return 'text-bg-primary'
        },
        async loadFilterGames() {
            try {
                const status = STATUS_BY_FILTER[this.statusFilter]
                this.games = await getGoalFilterGames(status) || []
            } catch {
                this.games = []
            }
        },
        async load() {
            this.table.isLoading = true
            this.error = ''
            try {
                const status = STATUS_BY_FILTER[this.statusFilter]
                const result = await getGoals(
                    status,
                    this.table.pagination.currentPage,
                    this.table.pagination.perPage,
                    this.filters
                )
                this.table.data = applyPagedResult(this.table.pagination, result)
            } catch (err) {
                this.table.data = []
                resetPagination(this.table.pagination)
                this.error = err?.message || 'Failed to load goals.'
            } finally {
                this.table.isLoading = false
            }
        },
        openAchievementsModal(row) {
            this.$refs.achievementsModal?.open(row)
        },
        async onAchievementsSaved() {
            await Promise.all([this.loadFilterGames(), this.load()])
        },
        onAbandon(row) {
            if (!row?.id) {
                return
            }
            this.pendingAbandonRow = row
            this.$refs.abandonModal?.open()
        },
        onReopen(row) {
            if (!row?.id) {
                return
            }
            this.pendingReopenRow = row
            this.$refs.reopenModal?.open()
        },
        async confirmAbandon() {
            const row = this.pendingAbandonRow
            if (!row?.id) {
                this.$refs.abandonModal?.close()
                return
            }
            this.actionKind = 'abandon'
            this.actionLoadingId = row.id
            this.error = ''
            try {
                await abandonGoal(row.id)
                this.pendingAbandonRow = null
                this.$refs.abandonModal?.close()
                await Promise.all([this.loadFilterGames(), this.load()])
            } catch (err) {
                this.error = err?.message || 'Failed to abandon goal.'
                this.$refs.abandonModal?.close()
            } finally {
                this.actionLoadingId = null
                this.actionKind = null
            }
        },
        async confirmReopen() {
            const row = this.pendingReopenRow
            if (!row?.id) {
                this.$refs.reopenModal?.close()
                return
            }
            this.actionKind = 'reopen'
            this.actionLoadingId = row.id
            this.error = ''
            try {
                await reopenGoal(row.id)
                this.pendingReopenRow = null
                this.$refs.reopenModal?.close()
                await Promise.all([this.loadFilterGames(), this.load()])
            } catch (err) {
                this.error = err?.message || 'Failed to reopen goal.'
                this.$refs.reopenModal?.close()
            } finally {
                this.actionLoadingId = null
                this.actionKind = null
            }
        }
    }
}
</script>
