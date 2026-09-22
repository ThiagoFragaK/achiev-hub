<template>
    <div class="card h-100">
        <div class="card-body">
            <p class="text-uppercase text-secondary small fw-medium mb-3">
                Achievements per year
            </p>
            <LineAreaChart :labels="labels" :datasets="datasets" :max="max" />
        </div>
    </div>
</template>

<script>
import LineAreaChart from '@/components/charts/LineAreaChart.vue'

export default {
    name: 'AchievementsPerYearGraph',
    components: {
        LineAreaChart
    },
    props: {
        labels: {
            type: Array,
            required: true
        },
        datasets: {
            type: Array,
            required: true
        },
        max: {
            type: Number,
            required: false,
            default: undefined
        }
    }
}
</script>
