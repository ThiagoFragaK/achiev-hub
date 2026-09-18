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
                            @change="selectAllRow"
                        />
                    </th>
                    <th v-for="(column, index) in columns" :key="index">
                        {{ column.label || column }}
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
import LoadingComponent from '@/components/global/LoadingComponent.vue';
import PaginationComponent from '@/components/global/PaginationComponent.vue';
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
        pagination: {
            type: Object,
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
        allSelected() {
            return this.data.length > 0 && this.selectedRows.length === this.data.length
        }
    },
    mounted() {
        this.cleanSelection()
    },
    methods: {
        selectAllRow() {
            if (this.allSelected) {
                this.selectedRows = []
            } else {
                this.selectedRows = [...this.data]
            }
            this.$emit('selectedRows', this.selectedRows)
        },
        selectRow(row) {
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
        }
    }
}
</script>
