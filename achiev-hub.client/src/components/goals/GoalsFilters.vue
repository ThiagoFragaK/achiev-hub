<template>
    <CollapseComponent ref="filters" collapse-id="goals-filters">
        <div class="row g-3 mb-3">
            <div class="col-md-4">
                <label class="form-label" for="filter-goal-game">Game</label>
                <select
                    id="filter-goal-game"
                    v-model="localFilters.appId"
                    class="form-select"
                >
                    <option value="">All</option>
                    <option
                        v-for="game in games"
                        :key="game.appId"
                        :value="String(game.appId)"
                    >
                        {{ game.gameName }}
                    </option>
                </select>
            </div>
            <div class="col-md-3">
                <label class="form-label" for="filter-min-completion">Min completion %</label>
                <input
                    id="filter-min-completion"
                    v-model="localFilters.minCompletion"
                    type="number"
                    min="0"
                    max="100"
                    step="0.1"
                    class="form-control"
                    @keydown.enter.prevent="applyFilters"
                />
            </div>
            <div class="col-md-3">
                <label class="form-label" for="filter-min-end-goal">Min end goal %</label>
                <input
                    id="filter-min-end-goal"
                    v-model="localFilters.minEndGoal"
                    type="number"
                    min="0"
                    max="100"
                    step="0.1"
                    class="form-control"
                    @keydown.enter.prevent="applyFilters"
                />
            </div>
            <div class="col-md-2 d-flex align-items-end">
                <button type="button" class="btn btn-primary w-100" @click="applyFilters">Apply</button>
            </div>
        </div>
    </CollapseComponent>
</template>

<script>
import CollapseComponent from '@/components/global/CollapseComponent.vue'

function toLocalFilters(filters = {}) {
    return {
        appId: filters.appId === '' || filters.appId == null
            ? ''
            : String(filters.appId),
        minCompletion: filters.minCompletion === '' || filters.minCompletion == null
            ? ''
            : filters.minCompletion,
        minEndGoal: filters.minEndGoal === '' || filters.minEndGoal == null
            ? ''
            : filters.minEndGoal
    }
}

function toOptionalNumber(value) {
    if (value === '' || value == null) {
        return ''
    }
    const n = Number(value)
    return Number.isFinite(n) ? n : ''
}

export default {
    name: 'GoalsFilters',
    components: {
        CollapseComponent
    },
    props: {
        filters: {
            type: Object,
            default: () => ({
                appId: '',
                minCompletion: '',
                minEndGoal: ''
            })
        },
        games: {
            type: Array,
            default: () => []
        }
    },
    emits: ['apply-filters'],
    data() {
        return {
            localFilters: toLocalFilters(this.filters)
        }
    },
    watch: {
        filters: {
            deep: true,
            handler(filters) {
                this.localFilters = toLocalFilters(filters)
            }
        }
    },
    methods: {
        toggleFilters() {
            this.$refs.filters.toggle()
        },
        applyFilters() {
            this.$emit('apply-filters', {
                appId: this.localFilters.appId === '' || this.localFilters.appId == null
                    ? ''
                    : String(this.localFilters.appId),
                minCompletion: toOptionalNumber(this.localFilters.minCompletion),
                minEndGoal: toOptionalNumber(this.localFilters.minEndGoal)
            })
        }
    }
}
</script>
