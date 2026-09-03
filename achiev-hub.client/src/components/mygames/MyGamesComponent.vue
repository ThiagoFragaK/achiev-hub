<template>
    <AppShell>
        <div class="d-flex align-items-center gap-2 mb-4">
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
        </div>

        <!-- Move to MyGamesFilters -->
        <CollapseComponent ref="filters" collapse-id="my-games-filters">
            <div class="row g-3 mb-4">
                <div class="col-md-4">
                    <label class="form-label" for="filter-game-name">Game</label>
                    <input
                        id="filter-game-name"
                        v-model="filters.name"
                        type="search"
                        class="form-control"
                        placeholder="Search by name"
                    />
                </div>
                <div class="col-md-4">
                    <label class="form-label" for="filter-min-hours">Min hours</label>
                    <input
                        id="filter-min-hours"
                        v-model="filters.minHours"
                        type="number"
                        min="0"
                        class="form-control"
                    />
                </div>
                <div class="col-md-4">
                    <label class="form-label" for="filter-min-percentage">Min percentage</label>
                    <input
                        id="filter-min-percentage"
                        v-model="filters.minPercentage"
                        type="number"
                        min="0"
                        max="100"
                        class="form-control"
                    />
                </div>
            </div>
        </CollapseComponent>

        <!-- Move to MyGamesTable -->
        <div class="card">
            <div class="card-body">
                <TableComponent :data="pagedGames" :columns="gameColumns">
                    <template #cell-name="{ data }">
                        <RouterLink
                            :to="{ name: 'game-detail', params: { id: data.row.id } }"
                            class="fw-medium"
                        >
                            {{ data.row.name }}
                        </RouterLink>
                    </template>
                    <template #cell-hours="{ data }"> {{ data.row.hours }}h </template>
                    <template #cell-percentage="{ data }"> {{ data.row.percentage }}% </template>
                </TableComponent>
                <PaginationComponent
                    :current-page="currentPage"
                    :total-pages="totalPages"
                    :per-page="perPage"
                    :total-items="demoGames.length"
                    @change-page="currentPage = $event"
                />
            </div>
        </div>
    </AppShell>
</template>

<script>
import AppShell from '@/components/layout/AppShell.vue'
import CollapseComponent from '@/components/global/CollapseComponent.vue'
import LucideIcon from '@/components/global/LucideIcon.vue'
import PaginationComponent from '@/components/global/PaginationComponent.vue'
import TableComponent from '@/components/global/TableComponent.vue'
import { demoGames } from '@/data/demo'

export default {
    name: 'MyGamesComponent',
    components: {
        AppShell,
        CollapseComponent,
        LucideIcon,
        PaginationComponent,
        TableComponent
    },
    data() {
        return {
            demoGames,
            currentPage: 1,
            perPage: 10,
            filters: {
                name: '',
                minHours: '',
                minPercentage: ''
            },
            gameColumns: [
                { key: 'name', label: 'Game' },
                { key: 'hours', label: 'Hours' },
                { key: 'percentage', label: 'Percentage' }
            ]
        }
    },
    computed: {
        totalPages() {
            return Math.max(1, Math.ceil(this.demoGames.length / this.perPage))
        },
        pagedGames() {
            const start = (this.currentPage - 1) * this.perPage
            return this.demoGames.slice(start, start + this.perPage)
        }
    },
    methods: {
        toggleFilters() {
            this.$refs.filters.toggle()
        }
    }
}
</script>
