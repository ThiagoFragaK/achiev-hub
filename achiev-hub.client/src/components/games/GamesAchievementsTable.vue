<template>
    <div v-if="error" class="alert alert-danger" role="alert">{{ error }}</div>
    <TableComponent
        ref="table"
        :data="table.data"
        :columns="table.columns"
        :isLoading="isLoading"
        :pagination="table.pagination"
        :hasSelection="hasSelection"
        :disabledIds="excludeIds"
        @change-page="onPageChange"
        @selectedRows="onSelectedRows"
    >
        <template #cell-name="{ data }">
            <div class="d-flex align-items-center gap-2">
                <img
                    :src="data.row.icon"
                    alt="Achievement Icon"
                    width="20"
                    height="20"
                    class="rounded"
                >
                {{ data.row.name }}
            </div>
        </template>
        <template #cell-description="{ data }">
            {{ data.row.description }}
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
import { getSessionSteamId } from '@/lib/steam'
import { formatGlobalPercentage, globalPercentageColorClass } from '@/lib/globalPercentage'
import { getAchievements as fetchAchievements } from '@/services/gamesService'
import TableComponent from '@/components/global/TableComponent.vue'

export default {
    name: 'GamesAchievementsTable',
    components: {
        TableComponent
    },
    props: {
        gameId: {
            type: [Number, String],
            required: true
        },
        filters: {
            type: Object,
            default: () => ({ name: '', status: 'locked', sort: 'unlockTime', global: 'all' })
        },
        hasSelection: {
            type: Boolean,
            default: false
        },
        excludeIds: {
            type: Array,
            default: () => []
        }
    },
    emits: ['selectedRows'],
    data() {
        return {
            isLoading: false,
            error: '',
            table: {
                data: [],
                columns: [
                    { key: 'name', label: 'Achievement' },
                    { key: 'description', label: 'Description' },
                    { key: 'globalPercentage', label: 'Global %' },
                    { key: 'unlocked', label: 'Unlocked' }
                ],
                pagination: {
                    currentPage: 1,
                    perPage: 15,
                    totalPages: 1,
                    totalCount: 0
                }
            }
        }
    },
    watch: {
        filters: {
            deep: true,
            handler() {
                this.table.pagination.currentPage = 1
                this.getAchievements()
            }
        },
        hasSelection(value) {
            if (!value) {
                this.cleanSelection()
            }
        }
    },
    methods: {
        formatGlobalPercentage,
        globalPercentageColorClass,
        cleanSelection() {
            this.$refs.table?.cleanSelection?.()
            this.$emit('selectedRows', [])
        },
        onSelectedRows(rows) {
            this.$emit('selectedRows', rows)
        },
        async onPageChange(page) {
            this.table.pagination.currentPage = page
            await this.getAchievements()
        },
        async getAchievements() {
            const steamId = getSessionSteamId()
            const gameId = this.gameId
            if (!steamId) {
                this.error = 'Steam ID is missing from your session.'
                this.table.data = []
                this.table.pagination.totalCount = 0
                this.table.pagination.totalPages = 1
                return
            }
            if (!gameId) {
                this.error = 'Game id is missing.'
                return
            }

            this.isLoading = true
            this.error = ''
            try {
                const result = await fetchAchievements(
                    steamId,
                    gameId,
                    this.table.pagination.currentPage,
                    this.table.pagination.perPage,
                    this.filters
                )
                this.table.data = result?.data ?? []
                this.table.pagination.currentPage = result?.currentPage ?? this.table.pagination.currentPage
                this.table.pagination.totalPages = Math.max(1, result?.lastPage ?? 1)
                this.table.pagination.totalCount = result?.totalCount ?? this.table.data.length
            } catch {
                this.error = 'Steam API is unreachable.'
                this.table.data = []
                this.table.pagination.totalCount = 0
                this.table.pagination.totalPages = 1
            } finally {
                this.isLoading = false
            }
        }
    },
    async created() {
        await this.getAchievements()
    }
}
</script>
