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
            <div class="col-md-2 mt-4">
                <button class="btn btn-primary mt-4" @click="applyFilters">Apply</button>
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
    data() {
        return {
            filters: {
                name: '',
                minHours: 0,
                minPercentage: 0
            }
        }
    },
    methods: {
        toggleFilters() {
            this.$refs.filters.toggle();
        },
        applyFilters() {
            console.log(this.filters);
            this.$emit('apply-filters', this.filters);
        }
    }
}
</script>