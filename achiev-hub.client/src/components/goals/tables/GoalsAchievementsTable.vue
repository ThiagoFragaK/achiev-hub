<template>
    <div>
        <div class="d-flex flex-wrap justify-content-end align-items-center gap-2 mb-3">
            <div class="input-group input-group-sm" style="max-width: 22rem">
                <input
                    id="goal-achievement-search"
                    v-model="draftSearch"
                    type="search"
                    class="form-control"
                    placeholder="Search name or description"
                    aria-label="Search achievements by name or description"
                    :disabled="isLoading"
                    @keydown.enter.prevent="applySearch"
                />
                <button
                    type="button"
                    class="btn btn-outline-primary"
                    aria-label="Filter achievements"
                    :disabled="isLoading"
                    @click="applySearch"
                >
                    <LucideIcon icon="Search" :size="16" />
                </button>
            </div>
        </div>

        <div v-if="error" class="alert alert-danger" role="alert">{{ error }}</div>
        <TableComponent
            ref="table"
            :data="table.data"
            :columns="table.columns"
            :isLoading="isLoading"
            :pagination="table.pagination"
            :hasSelection="true"
            :disabledIds="excludeIds"
            @change-page="onPageChange"
            @selectedRows="onSelectedRows"
        >
            <template #cell-name="{ data }">
                <div class="d-flex align-items-center gap-2">
                    <img
                        :src="data.row.icon"
                        alt=""
                        width="20"
                        height="20"
                        class="rounded"
                    />
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
        </TableComponent>
    </div>
</template>

<script>
import { getSessionSteamId } from '@/lib/steam'
import { formatGlobalPercentage, globalPercentageColorClass } from '@/lib/globalPercentage'
import { getAchievements as fetchAchievements } from '@/services/gamesService'
import TableComponent from '@/components/global/TableComponent.vue'
import LucideIcon from '@/components/global/LucideIcon.vue'

export default {
    name: 'GoalsAchievementsTable',
    components: {
        TableComponent,
        LucideIcon
    },
    props: {
        gameId: {
            type: [Number, String],
            required: true
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
            draftSearch: '',
            appliedSearch: '',
            table: {
                data: [],
                columns: [
                    { key: 'name', label: 'Achievement' },
                    { key: 'description', label: 'Description' },
                    { key: 'globalPercentage', label: 'Global %' }
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
        gameId: {
            immediate: false,
            handler() {
                this.draftSearch = ''
                this.appliedSearch = ''
                this.table.pagination.currentPage = 1
                this.getAchievements()
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
        applySearch() {
            this.appliedSearch = (this.draftSearch || '').trim()
            this.table.pagination.currentPage = 1
            this.getAchievements()
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
                    {
                        name: this.appliedSearch,
                        status: 'locked',
                        sort: 'name',
                        global: 'all'
                    }
                )
                this.table.data = result?.data ?? []
                this.table.pagination.currentPage = result?.currentPage ?? this.table.pagination.currentPage
                this.table.pagination.totalPages = Math.max(1, result?.lastPage ?? 1)
                this.table.pagination.totalCount = result?.totalCount ?? this.table.data.length
            } catch {
                this.error = 'Failed to load achievements.'
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
