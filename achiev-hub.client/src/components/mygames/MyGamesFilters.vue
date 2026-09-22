<template>
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
            <div class="col-md-3">
                <label class="form-label" for="filter-min-hours">Min hours</label>
                <input
                    id="filter-min-hours"
                    v-model="filters.minHours"
                    type="number"
                    min="0"
                    class="form-control"
                />
            </div>
            <div class="col-md-3">
                <label class="form-label" for="filter-has-achievements">Has achievements</label>
                <select
                    id="filter-has-achievements"
                    v-model="filters.hasAchievements"
                    class="form-select"
                >
                    <option value="">All</option>
                    <option value="true">Yes</option>
                    <option value="false">No</option>
                </select>
            </div>
            <div class="col-md-2 d-flex align-items-end">
                <button type="button" class="btn btn-primary w-100" @click="applyFilters">Apply</button>
            </div>
        </div>
    </CollapseComponent>
</template>
<script>
import CollapseComponent from '@/components/global/CollapseComponent.vue';

export default {
    name: 'MyGamesFilters',
    components: {
        CollapseComponent
    },
    emits: ['apply-filters'],
    data() {
        return {
            filters: {
                name: '',
                minHours: '',
                hasAchievements: ''
            }
        }
    },
    methods: {
        toggleFilters() {
            this.$refs.filters.toggle();
        },
        applyFilters() {
            const payload = {
                name: this.filters.name?.trim() || '',
                minHours: this.filters.minHours === '' || this.filters.minHours == null
                    ? ''
                    : Number(this.filters.minHours),
                hasAchievements: this.filters.hasAchievements === ''
                    ? ''
                    : this.filters.hasAchievements === 'true'
            }
            this.$emit('apply-filters', payload);
        }
    }
}
</script>
