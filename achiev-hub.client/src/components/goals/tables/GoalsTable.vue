<template>
    <div>
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
                {{ data.row.achievementsMissing }}
                <span v-if="data.row.achievementsTotal > 0" class="text-secondary">
                    / {{ data.row.achievementsTotal }}
                </span>
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
                        class="btn btn-sm btn-outline-primary"
                        aria-label="Edit goal achievements"
                        @click="openAchievementsModal(data.row)"
                    >
                        <LucideIcon icon="List" :size="16" />
                    </button>
                    <button
                        type="button"
                        class="btn btn-sm btn-outline-warning"
                        :disabled="abandoningId === data.row.id"
                        @click="onAbandon(data.row)"
                    >
                        Abandon
                    </button>
                </div>
                <span v-else class="text-secondary">—</span>
            </template>
        </TableComponent>

        <GoalAchievementsModal
            ref="achievementsModal"
            @saved="onAchievementsSaved"
        />
    </div>
</template>

<script>
import TableComponent from '@/components/global/TableComponent.vue'
import LucideIcon from '@/components/global/LucideIcon.vue'
import GoalAchievementsModal from '@/components/goals/modals/GoalAchievementsModal.vue'
import { abandonGoal, getGoals } from '@/services/goalsService'
import { formatPercentage } from '@/utils/NumberFormat'

const STATUS = {
    Active: 0,
    Completed: 1,
    Abandoned: 2
}

export default {
    name: 'GoalsTable',
    components: {
        TableComponent,
        LucideIcon,
        GoalAchievementsModal
    },
    data() {
        return {
            error: '',
            abandoningId: null,
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
    async created() {
        await this.load()
    },
    methods: {
        formatPercentage,
        isActive(status) {
            return status === STATUS.Active || status === 'Active'
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
                this.table.data = await getGoals() || []
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
        async onAbandon(row) {
            if (!row?.id || !confirm(`Abandon the goal for ${row.gameName}?`)) {
                return
            }
            this.abandoningId = row.id
            this.error = ''
            try {
                await abandonGoal(row.id)
                await this.load()
            } catch (err) {
                this.error = err?.message || 'Failed to abandon goal.'
            } finally {
                this.abandoningId = null
            }
        }
    }
}
</script>
