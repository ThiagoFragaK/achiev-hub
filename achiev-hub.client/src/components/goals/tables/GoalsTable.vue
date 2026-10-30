<template>
    <div>
        <TabsComponent v-model="statusFilter" :tabs="statusTabs">
            <div v-if="error" class="alert alert-danger" role="alert">{{ error }}</div>
            <TableComponent
                :data="table.data"
                :columns="table.columns"
                :isLoading="table.isLoading"
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
                        aria-label="Edit goal achievements"
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
import GoalAchievementsModal from '@/components/goals/modals/GoalAchievementsModal.vue'
import { abandonGoal, getGoals, reopenGoal } from '@/services/goalsService'
import { formatPercentage } from '@/utils/NumberFormat'

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

export default {
    name: 'GoalsTable',
    components: {
        TableComponent,
        TabsComponent,
        ConfirmModal,
        GoalAchievementsModal
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
                    { key: 'endGoal', label: 'End goal' },
                    { key: 'actions', label: '' }
                ]
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
            this.load()
        }
    },
    async created() {
        await this.load()
    },
    methods: {
        formatPercentage,
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
        async load() {
            this.table.isLoading = true
            this.error = ''
            try {
                const status = STATUS_BY_FILTER[this.statusFilter]
                this.table.data = await getGoals(status) || []
            } catch (err) {
                this.table.data = []
                this.error = err?.message || 'Failed to load goals.'
            } finally {
                this.table.isLoading = false
            }
        },
        openAchievementsModal(row) {
            this.$refs.achievementsModal?.open(row)
        },
        async onAchievementsSaved() {
            await this.load()
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
                await this.load()
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
                await this.load()
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
