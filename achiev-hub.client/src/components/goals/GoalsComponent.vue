<template>
    <AppShell>
        <div class="d-flex flex-wrap align-items-center gap-2 mb-4">
            <h1 class="lastica-h3 mb-0">Your Goals</h1>
            <RouterLink
                :to="{ name: 'goals-form' }"
                class="btn btn-primary btn-sm ms-auto"
            >
                Create a Goal
            </RouterLink>
        </div>

        <div class="mb-4">
            <CompletedGraphComponent />
        </div>

        <div class="card">
            <div class="card-body">
                <GoalsTable />
            </div>
        </div>
    </AppShell>
</template>

<script>
import AppShell from '@/components/layout/AppShell.vue'
import CompletedGraphComponent from '@/components/goals/graphs/CompletedGraphComponent.vue'
import GoalsTable from '@/components/goals/tables/GoalsTable.vue'

export default {
    name: 'GoalsComponent',
    components: {
        AppShell,
        CompletedGraphComponent,
        GoalsTable
    }
}
</script>
