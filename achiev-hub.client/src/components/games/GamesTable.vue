<template>
    <div>
        <TableComponent :data="achievements" :columns="columns">
            <template #cell-status="{ data }">
                {{ data.row.unlocked ? 'Unlocked' : 'Locked' }}
            </template>
        </TableComponent>
        <PaginationComponent
            :current-page="currentPage"
            :total-pages="totalPages"
            :per-page="perPage"
            :total-items="totalItems"
            @change-page="$emit('change-page', $event)"
        />
    </div>
</template>

<script>
import PaginationComponent from '@/components/global/PaginationComponent.vue'
import TableComponent from '@/components/global/TableComponent.vue'

export default {
    name: 'GamesTable',
    components: {
        PaginationComponent,
        TableComponent
    },
    props: {
        achievements: {
            type: Array,
            required: true
        },
        columns: {
            type: Array,
            required: true
        },
        currentPage: {
            type: Number,
            required: true
        },
        totalPages: {
            type: Number,
            required: true
        },
        perPage: {
            type: Number,
            required: true
        },
        totalItems: {
            type: Number,
            required: true
        }
    },
    emits: ['change-page']
}
</script>
