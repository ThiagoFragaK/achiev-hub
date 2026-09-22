<template>
    <CollapseComponent ref="collapse" :collapse-id="collapseId">
        <div class="row g-3 mb-3 align-items-end">
            <div class="col-md-5">
                <label class="form-label" for="filter-achievement-name">Achievement</label>
                <input
                    id="filter-achievement-name"
                    v-model="draft.name"
                    type="search"
                    class="form-control"
                    placeholder="Search by name"
                />
            </div>
            <div class="col-md-5">
                <label class="form-label" for="filter-achievement-status">Status</label>
                <select id="filter-achievement-status" v-model="draft.status" class="form-select">
                    <option value="">All</option>
                    <option value="unlocked">Unlocked</option>
                    <option value="locked">Locked</option>
                </select>
            </div>
            <div class="col-md-2">
                <button type="button" class="btn btn-primary w-100" @click="applyFilters">Apply</button>
            </div>
        </div>
    </CollapseComponent>
</template>

<script>
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
            draft: {
                name: this.modelValue.name || '',
                status: this.modelValue.status || ''
            }
        }
    },
    watch: {
        modelValue: {
            deep: true,
            handler(value) {
                this.draft = {
                    name: value?.name || '',
                    status: value?.status || ''
                }
            }
        }
    },
    methods: {
        applyFilters() {
            const filters = {
                name: this.draft.name || '',
                status: this.draft.status || ''
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
