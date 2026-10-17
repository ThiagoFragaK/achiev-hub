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
                hasAchievements: '',
                sort: 'playtime'
            },
            appliedFilters: 0,
            sortOptions: [
                { value: 'playtime', label: 'Playtime' },
                { value: 'name', label: 'A–Z' },
                { value: 'achievements', label: 'Achievements' }
            ]
        }
    },
    methods: {
        toggleFilters() {
            this.$refs.MyGamesFilters.toggleFilters()
        },
        applyFilters(filters) {
            this.filters = {
                ...filters,
                sort: this.filters.sort || 'playtime'
            };
            this.appliedFilters++;
        },
        setSort(sort) {
            if (this.filters.sort === sort) {
                return
            }
            this.filters = {
                ...this.filters,
                sort
            }
            this.appliedFilters++
        }
    }
}
</script>
