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

        <MyGamesFilters
            @apply-filters="applyFilters"
            ref="MyGamesFilters"
        />

        <MyGamesTable
            :key="appliedFilters"
            :filters="filters" 
        />
    </AppShell>
</template>

<script>
import AppShell from '@/components/layout/AppShell.vue'
import MyGamesFilters from '@/components/mygames/MyGamesFilters.vue';
import MyGamesTable from '@/components/mygames/MyGamesTable.vue';
import LucideIcon from '@/components/global/LucideIcon.vue'

export default {
    name: 'MyGamesComponent',
    components: {
        AppShell,
        MyGamesFilters,
        MyGamesTable,
        LucideIcon,
    },
    data() {
        return {
            steamId: null,
            filters: {
                name: '',
                minHours: '',
                hasAchievements: ''
            },
            appliedFilters: 0,
        }
    },
    methods: {
        toggleFilters() {
            this.$refs.MyGamesFilters.toggleFilters()
        },
        applyFilters(filters) {
            this.filters = filters;
            this.appliedFilters++;
        }
    }
}
</script>
