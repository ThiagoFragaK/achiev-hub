<template>
    <CollapseComponent ref="collapse" :collapse-id="collapseId">
        <div class="row g-3 mb-3 align-items-end">
            <div class="col-md-6">
                <label class="form-label" for="filter-achievement-name">Achievement</label>
                <input
                    id="filter-achievement-name"
                    v-model="draft.name"
                    type="search"
                    class="form-control"
                    placeholder="Search by name"
                    @keydown.enter.prevent="applyFilters"
                />
            </div>
            <div class="col-md-4">
                <label class="form-label" for="filter-achievement-global">Global %</label>
                <select
                    id="filter-achievement-global"
                    v-model="draft.global"
                    class="form-select"
                    aria-label="Filter by global percentage"
                >
                    <option
                        v-for="option in globalOptions"
                        :key="option.value"
                        :value="option.value"
                    >
                        {{ option.label }}
                    </option>
                </select>
            </div>
            <div class="col-md-2">
                <button type="button" class="btn btn-primary w-100" @click="applyFilters">Apply</button>
            </div>
        </div>
    </CollapseComponent>
</template>

<script>
import { GLOBAL_PERCENTAGE_FILTER_OPTIONS } from '@/lib/globalPercentage'
import CollapseComponent from '@/components/global/CollapseComponent.vue'

export default {
    name: 'GamesFilters',
    components: {
        CollapseComponent
    },
    props: {
        modelValue: {
            type: Object,
            required: true
        },
        collapseId: {
            type: String,
            required: true
        }
    },
    emits: ['update:modelValue', 'apply-filters'],
    data() {
        return {
            globalOptions: GLOBAL_PERCENTAGE_FILTER_OPTIONS,
            draft: {
                name: this.modelValue.name || '',
                status: this.modelValue.status || 'locked',
                sort: this.modelValue.sort || 'unlockTime',
                global: this.modelValue.global || 'all'
            }
        }
    },
    watch: {
        modelValue: {
            deep: true,
            handler(value) {
                this.draft = {
                    name: value?.name || '',
                    status: value?.status || 'locked',
                    sort: value?.sort || 'unlockTime',
                    global: value?.global || 'all'
                }
            }
        }
    },
    methods: {
        applyFilters() {
            const filters = {
                name: this.draft.name?.trim() || '',
                status: this.modelValue?.status || 'locked',
                sort: this.modelValue?.sort || 'unlockTime',
                global: this.draft.global || 'all'
            }
            this.$emit('update:modelValue', filters)
            this.$emit('apply-filters', filters)
        },
        open() {
            this.$refs.collapse.open()
        },
        close() {
            this.$refs.collapse.close()
        },
        toggle() {
            this.$refs.collapse.toggle()
        }
    }
}
</script>
