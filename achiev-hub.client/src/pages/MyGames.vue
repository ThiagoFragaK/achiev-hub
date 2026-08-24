<template>
    <AppShell>
        <div class="d-flex align-items-center gap-2 mb-4">
            <button type="button" class="btn btn-outline-light btn-sm" aria-label="Filter games">
                <LucideIcon icon="Funnel" :size="18" />
            </button>
            <h1 class="lastica-h3 mb-0">My Games</h1>
        </div>

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
import LucideIcon from '@/components/global/LucideIcon.vue'
import PaginationComponent from '@/components/global/PaginationComponent.vue'
import TableComponent from '@/components/global/TableComponent.vue'
import { demoGames } from '@/data/demo'

export default {
    name: 'MyGames',
    components: {
        AppShell,
        LucideIcon,
        PaginationComponent,
        TableComponent
    },
    data() {
        return {
            demoGames,
            currentPage: 1,
            perPage: 10,
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
    }
}
</script>
