<template>
    <div class="card h-100">
        <div class="card-body">
            <div class="d-flex align-items-baseline justify-content-between gap-2 mb-3">
                <p class="text-uppercase text-secondary small fw-medium mb-0">
                    {{ totalLast14Days.toLocaleString() }} achievements earned in last 2 weeks
                </p>
            </div>
            <LineAreaChart
                :labels="labels"
                :datasets="datasets"
                :max="max"
                :height="height"
            />
        </div>
    </div>
</template>

<script>
import LineAreaChart from '@/components/charts/LineAreaChart.vue'

export default {
    name: 'AchievementsLast14DaysGraph',
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
        },
        height: {
            type: [Number, String],
            required: false,
            default: 220
        }
    },
    computed: {
        totalLast14Days() {
            const data = this.datasets?.[0]?.data ?? []
            return data.reduce((sum, count) => sum + (Number(count) || 0), 0)
        }
    }
}
</script>
