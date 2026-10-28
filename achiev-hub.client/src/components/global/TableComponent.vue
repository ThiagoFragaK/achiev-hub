<template>
    <div class="table-responsive">
        <table class="table table-bordered table-hover text-center mb-0 align-middle">
            <thead class="table-primary">
                <tr>
                    <th v-if="hasSelection">
                        <input
                            type="checkbox"
                            class="form-check-input"
                            :checked="allSelected"
                            :disabled="selectableRows.length === 0"
                            @change="selectAllRow"
                        />
                    </th>
                    <th v-for="(column, index) in columns" :key="index">
                        {{ column.label }}
                    </th>
                </tr>
            </thead>
            <tbody v-if="isLoading">
                <tr>
                    <td
                        :colspan="columns.length + (hasSelection ? 1 : 0)"
                        class="text-center py-4"
                    >
                        <LoadingComponent :color="light" />
                    </td>
                </tr>
            </tbody>
            <tbody v-else-if="data.length > 0">
                <tr v-for="(row, index) in data" :key="index">
                    <td v-if="hasSelection">
                        <input
                            type="checkbox"
                            class="form-check-input"
                            :value="row"
                            :checked="isSelected(row)"
                            :disabled="isRowDisabled(row)"
                            @change="selectRow(row)"
                        />
                    </td>
                    <td v-for="column in columns" :key="column.key">
                        <slot :name="`cell-${column.key}`" :data="{ row, column }">
                            {{ row[column.key] }}
                        </slot>
                    </td>
                </tr>
            </tbody>
            <tbody v-else>
                <tr>
                    <td
                        :colspan="columns.length + (hasSelection ? 1 : 0)"
                        class="text-center text-secondary py-4"
                    >
                        No data available.
                    </td>
                </tr>
            </tbody>
        </table>
        <div v-if="pagination" class="d-flex justify-content-center">
            <PaginationComponent
                :current-page="pagination.currentPage"
                :total-pages="pagination.totalPages"
                :per-page="pagination.perPage"
                :total-items="pagination.totalCount"
                @change-page="$emit('change-page', $event)"
            />
        </div>
    </div>
</template>

<script>
import LoadingComponent from '@/components/global/LoadingComponent.vue'
import PaginationComponent from '@/components/global/PaginationComponent.vue'

export default {
    name: 'TableComponent',
    components: {
        LoadingComponent,
        PaginationComponent
    },
    props: {
        data: {
            type: Array,
            required: true
        },
        columns: {
            type: Array,
            required: true
        },
        isLoading: {
            type: Boolean,
            default: false
        },
        hasSelection: {
            type: Boolean,
            default: false
        },
        disabledIds: {
            type: Array,
            default: () => []
        },
        pagination: {
            type: Object,
            required: false
        },
        light: {
            type: String,
            default: 'primary',
            required: false
        }
    },
    emits: ['selectedRows', 'change-page'],
    data() {
        return {
            selectedRows: []
        }
    },
    computed: {
        disabledIdSet() {
            return new Set((this.disabledIds || []).map(Number))
        },
        selectableRows() {
            return this.data.filter((row) => !this.isRowDisabled(row))
        },
        allSelected() {
            return this.selectableRows.length > 0
                && this.selectableRows.every((row) => this.selectedRows.includes(row))
        }
    },
    mounted() {
        this.cleanSelection()
    },
    methods: {
        isRowDisabled(row) {
            if (this.disabledIdSet.size === 0) {
                return false
            }
            return this.disabledIdSet.has(Number(row.id))
        },
        selectAllRow() {
            if (this.allSelected) {
                this.selectedRows = []
            } else {
                this.selectedRows = [...this.selectableRows]
            }
            this.$emit('selectedRows', this.selectedRows)
        },
        selectRow(row) {
            if (this.isRowDisabled(row)) {
                return
            }
            const index = this.selectedRows.indexOf(row)
            if (index === -1) {
                this.selectedRows.push(row)
            } else {
                this.selectedRows.splice(index, 1)
            }
            this.$emit('selectedRows', this.selectedRows)
        },
        isSelected(row) {
            return this.selectedRows.includes(row)
        },
        cleanSelection() {
            this.selectedRows = []
            this.$emit('selectedRows', [])
        }
    }
}
</script>
