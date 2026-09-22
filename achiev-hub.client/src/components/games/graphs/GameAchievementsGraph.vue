<template>
    <div class="card h-100">
        <div class="card-body p-2">
            <p
                v-if="isEmpty"
                class="text-secondary small text-center mb-0 py-4"
            >
                {{ emptyMessage }}
            </p>
            <LineAreaChart
                v-else
                :title="title"
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
    name: 'GameAchievementsGraph',
    components: {
        LineAreaChart
    },
    props: {
        title: {
            type: String,
            required: false,
            default: ''
        },
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
            type: Number,
            required: false,
            default: 220
        },
        emptyMessage: {
            type: String,
            required: false,
            default: ''
        }
    },
    computed: {
        isEmpty() {
            return !!this.emptyMessage && this.labels.length === 0
        }
    }
}
</script>
