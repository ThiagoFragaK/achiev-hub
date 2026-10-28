<template>
    <div v-if="error" class="alert alert-danger" role="alert">{{ error }}</div>
    <TableComponent
        :data="table.data"
        :columns="table.columns"
        :isLoading="table.isLoading"
        :pagination="table.pagination"
        @change-page="onPageChange"
    >
        <template #cell-name="{ data }">
            <div class="d-flex align-items-center gap-2">
                <img
                    v-if="data.row.icon"
                    :src="data.row.icon"
                    alt="Achievement Icon"
                    width="35"
                    height="35"
                    class="rounded"
                >
                {{ data.row.name }}
            </div>
        </template>
        <template #cell-gameName="{ data }">
            <RouterLink
                v-if="data.row.appId"
                :to="{ name: 'game-detail', params: { id: data.row.appId } }"
                class="fw-medium"
            >
                {{ data.row.gameName }}
            </RouterLink>
            <span v-else>{{ data.row.gameName }}</span>
        </template>
        <template #cell-globalPercentage="{ data }">
            <span :class="globalPercentageColorClass(data.row.globalPercentage)">
                {{ formatGlobalPercentage(data.row.globalPercentage) }}
            </span>
        </template>
        <template #cell-unlocked="{ data }">
            {{ data.row.unlocked || '-' }}
        </template>
    </TableComponent>
</template>

<script>
import { formatGlobalPercentage, globalPercentageColorClass } from '@/lib/globalPercentage'
import { getRecentAchievements } from '@/services/gamesService'
import TableComponent from '@/components/global/TableComponent.vue'

export default {
    name: 'RecentAchievementsTable',
    components: {
        TableComponent,
    },
    data() {
        return {
            error: '',
            table: {
                isLoading: true,
                data: [],
                columns: [
                    { key: 'name', label: 'Achievement' },
                    { key: 'gameName', label: 'Game' },
                    { key: 'globalPercentage', label: 'Global %' },
                    { key: 'unlocked', label: 'Unlocked' },
                ],
                pagination: {
                    currentPage: 1,
                    perPage: 10,
                    totalPages: 1,
                    totalCount: 0,
                },
            },
        }
    },
    methods: {
        formatGlobalPercentage,
        globalPercentageColorClass,
        async onPageChange(page) {
            this.table.pagination.currentPage = page
            await this.loadRecentAchievements()
        },
        async loadRecentAchievements() {
            this.table.isLoading = true
            this.error = ''
            try {
                const result = await getRecentAchievements(
                    this.table.pagination.currentPage,
                    this.table.pagination.perPage
                )
                this.table.data = result?.data ?? []
                this.table.pagination.currentPage = result?.currentPage ?? this.table.pagination.currentPage
                this.table.pagination.totalPages = Math.max(1, result?.lastPage ?? 1)
                this.table.pagination.totalCount = result?.totalCount ?? this.table.data.length
            } catch (err) {
                this.table.data = []
                this.table.pagination.totalCount = 0
                this.table.pagination.totalPages = 1
                this.error = err?.message || 'Failed to load recent achievements.'
            } finally {
                this.table.isLoading = false
            }
        },
    },
    async created() {
        await this.loadRecentAchievements()
    },
}
</script>
