<template>
    <AppShell>
        <div class="d-flex flex-wrap align-items-center gap-2 mb-4">
            <button
                type="button"
                class="btn btn-outline-light btn-sm"
                aria-label="Filter games"
                aria-controls="my-games-filters"
                @click="toggleFilters"
            >
                <LucideIcon icon="Funnel" :size="18" />
            </button>
            <h1 class="lastica-h3 mb-0">My Games</h1>
            <div class="btn-group ms-auto" role="group" aria-label="Sort games">
                <button
                    v-for="option in sortOptions"
                    :key="option.value"
                    type="button"
                    class="btn btn-sm"
                    :class="filters.sort === option.value ? 'btn-primary' : 'btn-outline-secondary'"
                    @click="setSort(option.value)"
                >
                    {{ option.label }}
                </button>
            </div>
        </div>

        <MyGamesFilters
            :filters="filters"
            @apply-filters="applyFilters"
            ref="MyGamesFilters"
        />

        <MyGamesTable
            :key="appliedFilters"
            :filters="filters"
            :page="currentPage"
            @page-change="onPageChange"
        />
    </AppShell>
</template>

<script>
import AppShell from '@/components/layout/AppShell.vue'
import MyGamesFilters from '@/components/mygames/MyGamesFilters.vue';
import MyGamesTable from '@/components/mygames/MyGamesTable.vue';
import LucideIcon from '@/components/global/LucideIcon.vue'

const DEFAULT_SORT = 'playtime'
const SORT_OPTIONS = ['playtime', 'name', 'achievements']

function parseListQuery(query) {
    const page = Math.max(1, parseInt(query.page, 10) || 1)
    const name = typeof query.name === 'string' ? query.name : ''
    const minHoursRaw = query.minHours
    const minHours = minHoursRaw === undefined || minHoursRaw === ''
        ? ''
        : Number(minHoursRaw)
    let hasAchievements = ''
    if (query.hasAchievements === 'true') {
        hasAchievements = true
    } else if (query.hasAchievements === 'false') {
        hasAchievements = false
    }
    const sort = SORT_OPTIONS.includes(query.sort) ? query.sort : DEFAULT_SORT

    return {
        page,
        filters: {
            name,
            minHours: Number.isFinite(minHours) ? minHours : '',
            hasAchievements,
            sort
        }
    }
}

function filtersEqual(a, b) {
    return a.name === b.name
        && a.minHours === b.minHours
        && a.hasAchievements === b.hasAchievements
        && a.sort === b.sort
}

export default {
    name: 'MyGamesComponent',
    components: {
        AppShell,
        MyGamesFilters,
        MyGamesTable,
        LucideIcon,
    },
    data() {
        const { page, filters } = parseListQuery(this.$route.query)
        return {
            steamId: null,
            filters,
            currentPage: page,
            appliedFilters: 0,
            syncingQuery: false,
            sortOptions: [
                { value: 'playtime', label: 'Playtime' },
                { value: 'name', label: 'A–Z' },
                { value: 'achievements', label: 'Achievements' }
            ]
        }
    },
    watch: {
        '$route.query'(query) {
            if (this.syncingQuery) {
                return
            }
            this.hydrateFromQuery(query)
        }
    },
    methods: {
        toggleFilters() {
            this.$refs.MyGamesFilters.toggleFilters()
        },
        buildQuery() {
            const query = {}
            if (this.currentPage > 1) {
                query.page = String(this.currentPage)
            }
            if (this.filters.name) {
                query.name = this.filters.name
            }
            if (this.filters.minHours !== '' && this.filters.minHours != null) {
                query.minHours = String(this.filters.minHours)
            }
            if (this.filters.hasAchievements === true) {
                query.hasAchievements = 'true'
            } else if (this.filters.hasAchievements === false) {
                query.hasAchievements = 'false'
            }
            if (this.filters.sort && this.filters.sort !== DEFAULT_SORT) {
                query.sort = this.filters.sort
            }
            return query
        },
        queriesEqual(a, b) {
            const keys = ['page', 'name', 'minHours', 'hasAchievements', 'sort']
            return keys.every((key) => (a[key] ?? '') === (b[key] ?? ''))
        },
        async syncQuery() {
            const query = this.buildQuery()
            if (this.queriesEqual(query, this.$route.query)) {
                return
            }
            this.syncingQuery = true
            try {
                await this.$router.replace({ query })
            } finally {
                this.syncingQuery = false
            }
        },
        hydrateFromQuery(query) {
            const { page, filters } = parseListQuery(query)
            const filtersChanged = !filtersEqual(filters, this.filters)
            this.filters = filters
            this.currentPage = page
            if (filtersChanged) {
                this.appliedFilters++
            }
        },
        applyFilters(filters) {
            this.filters = {
                ...filters,
                sort: this.filters.sort || DEFAULT_SORT
            }
            this.currentPage = 1
            this.appliedFilters++
            this.syncQuery()
        },
        setSort(sort) {
            if (this.filters.sort === sort) {
                return
            }
            this.filters = {
                ...this.filters,
                sort
            }
            this.currentPage = 1
            this.appliedFilters++
            this.syncQuery()
        },
        onPageChange(page) {
            if (this.currentPage === page) {
                return
            }
            this.currentPage = page
            this.syncQuery()
        }
    }
}
</script>
