<template>
    <div>
        <p v-if="metaError" class="text-danger small mb-3">{{ metaError }}</p>
        <div
            v-if="availableYears.length > 0"
            class="d-flex flex-wrap align-items-center gap-2 mb-3"
        >
            <div class="d-flex flex-wrap gap-2 me-auto">
                <button
                    v-for="year in availableYears"
                    :key="year"
                    type="button"
                    class="btn btn-sm"
                    :class="selectedYear === year ? 'btn-primary' : 'btn-outline-secondary'"
                    @click="selectYear(year)"
                >
                    {{ year }}
                </button>
            </div>
            <div class="input-group input-group-sm stats-list-search">
                <label class="form-label visually-hidden" for="month-achievement-search">Search by name</label>
                <input
                    id="month-achievement-search"
                    v-model="nameDraft"
                    type="search"
                    class="form-control"
                    placeholder="Search by name"
                    :disabled="table.isLoading || availableYears.length === 0"
                    @keydown.enter.prevent="applySearch"
                />
                <button
                    type="button"
                    class="btn btn-outline-secondary"
                    :disabled="!nameDraft && !nameQuery"
                    aria-label="Clear search"
                    @click="clearSearch"
                >
                    ×
                </button>
            </div>
        </div>
        <div v-if="availableYears.length > 0" class="mb-3">
            <div class="d-flex flex-wrap gap-2">
                <button
                    v-for="month in monthOptions"
                    :key="month.value"
                    type="button"
                    class="btn btn-sm"
                    :class="selectedMonth === month.value ? 'btn-primary' : 'btn-outline-secondary'"
                    :disabled="month.count === 0"
                    @click="selectMonth(month.value)"
                >
                    {{ month.label }}
                </button>
            </div>
        </div>
        <p class="text-secondary small mb-2">
            {{ achievementCountLabel }}
        </p>
        <p v-if="tableError" class="text-danger small mb-3">{{ tableError }}</p>
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
    </div>
</template>

<script>
import { formatGlobalPercentage, globalPercentageColorClass } from '@/lib/globalPercentage'
import { getAchievementsByMonth, getAchievementsForMonth } from '@/services/statsService'
import TableComponent from '@/components/global/TableComponent.vue'
import {
    applyPagedResult,
    createPagination,
    resetPagination
} from '@/utils/PaginationHelper'

const MONTH_LABELS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']
const PAGE_SIZE = 10

export default {
    name: 'AchievementsByMonthList',
    components: {
        TableComponent
    },
    data() {
        return {
            metaError: '',
            tableError: '',
            availableYears: [],
            series: [],
            selectedYear: null,
            selectedMonth: null,
            nameDraft: '',
            nameQuery: '',
            table: {
                isLoading: true,
                data: [],
                columns: [
                    { key: 'name', label: 'Achievement' },
                    { key: 'gameName', label: 'Game' },
                    { key: 'globalPercentage', label: 'Global %' },
                    { key: 'unlocked', label: 'Unlocked' }
                ],
                pagination: createPagination({ perPage: PAGE_SIZE })
            }
        }
    },
    computed: {
        seriesByYear() {
            const map = new Map()
            for (const item of this.series) {
                map.set(item.year, item.counts ?? [])
            }
            return map
        },
        monthOptions() {
            const counts = this.seriesByYear.get(this.selectedYear) ?? Array(12).fill(0)
            return MONTH_LABELS.map((label, index) => ({
                value: index + 1,
                label,
                count: counts[index] ?? 0
            }))
        },
        achievementCountLabel() {
            const count = this.table.pagination.totalCount ?? 0
            return `${count} ${count === 1 ? 'achievement' : 'achievements'}`
        }
    },
    async created() {
        await this.loadMeta()
    },
    methods: {
        formatGlobalPercentage,
        globalPercentageColorClass,
        pickDefaultMonth(year) {
            const counts = this.seriesByYear.get(year) ?? []
            for (let month = 12; month >= 1; month--) {
                if ((counts[month - 1] ?? 0) > 0) {
                    return month
                }
            }
            return 1
        },
        async selectYear(year) {
            if (this.selectedYear === year) {
                return
            }
            this.selectedYear = year
            this.selectedMonth = this.pickDefaultMonth(year)
            this.table.pagination.currentPage = 1
            await this.loadAchievements()
        },
        async selectMonth(month) {
            if (this.selectedMonth === month) {
                return
            }
            this.selectedMonth = month
            this.table.pagination.currentPage = 1
            await this.loadAchievements()
        },
        async onPageChange(page) {
            const panel = this.$el?.closest?.('.stats-list-panel')
            const panelScroll = panel?.scrollTop ?? 0
            const windowScroll = window.scrollY
            this.table.pagination.currentPage = page
            await this.loadAchievements()
            await this.$nextTick()
            if (panel) {
                panel.scrollTop = panelScroll
            }
            window.scrollTo(0, windowScroll)
        },
        async applySearch() {
            const next = this.nameDraft.trim()
            if (next === this.nameQuery) {
                return
            }
            this.nameQuery = next
            this.nameDraft = next
            this.table.pagination.currentPage = 1
            await this.loadAchievements()
        },
        async clearSearch() {
            this.nameDraft = ''
            if (!this.nameQuery) {
                return
            }
            this.nameQuery = ''
            this.table.pagination.currentPage = 1
            await this.loadAchievements()
        },
        async loadMeta() {
            this.metaError = ''
            this.table.isLoading = true
            try {
                const data = await getAchievementsByMonth()
                this.availableYears = data?.availableYears ?? []
                this.series = data?.series ?? []
                if (this.availableYears.length === 0) {
                    this.selectedYear = null
                    this.selectedMonth = null
                    this.table.data = []
                    resetPagination(this.table.pagination)
                    this.table.isLoading = false
                    return
                }
                this.selectedYear = this.availableYears[0]
                this.selectedMonth = this.pickDefaultMonth(this.selectedYear)
                this.table.pagination.currentPage = 1
                await this.loadAchievements()
            } catch (err) {
                this.metaError = err?.message || 'Failed to load achievements by month.'
                this.availableYears = []
                this.series = []
                this.selectedYear = null
                this.selectedMonth = null
                this.table.data = []
                resetPagination(this.table.pagination)
                this.table.isLoading = false
            }
        },
        async loadAchievements() {
            if (this.selectedYear == null || this.selectedMonth == null) {
                this.table.data = []
                resetPagination(this.table.pagination)
                this.table.isLoading = false
                return
            }

            this.table.isLoading = true
            this.tableError = ''
            try {
                const result = await getAchievementsForMonth(
                    this.selectedYear,
                    this.selectedMonth,
                    this.table.pagination.currentPage,
                    this.table.pagination.perPage,
                    this.nameQuery
                )
                this.table.data = applyPagedResult(this.table.pagination, result)
            } catch (err) {
                this.table.data = []
                resetPagination(this.table.pagination)
                this.tableError = err?.message || 'Failed to load achievements for month.'
            } finally {
                this.table.isLoading = false
            }
        }
    }
}
</script>

<style scoped>
.stats-list-search {
    max-width: 15rem;
}
</style>
