<template>
    <CollapseComponent ref="collapse" :collapse-id="collapseId">
        <div class="row g-3 mb-3">
            <div class="col-md-6">
                <label class="form-label" for="filter-achievement-name">Achievement</label>
                <input
                    id="filter-achievement-name"
                    v-model="name"
                    type="search"
                    class="form-control"
                    placeholder="Search by name"
                />
            </div>
            <div class="col-md-6">
                <label class="form-label" for="filter-achievement-status">Status</label>
                <select id="filter-achievement-status" v-model="status" class="form-select">
                    <option value="">All</option>
                    <option value="unlocked">Unlocked</option>
                    <option value="locked">Locked</option>
                </select>
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
    emits: ['update:modelValue'],
    computed: {
        name: {
            get() {
                return this.modelValue.name
            },
            set(value) {
                this.$emit('update:modelValue', { ...this.modelValue, name: value })
            }
        },
        status: {
            get() {
                return this.modelValue.status
            },
            set(value) {
                this.$emit('update:modelValue', { ...this.modelValue, status: value })
            }
        }
    },
    methods: {
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
