<template>
    <div class="card h-100">
        <div class="card-body">
            <p class="text-uppercase text-secondary small fw-medium mb-3">
                Goals completed
            </p>
            <p v-if="loadError" class="text-danger small mb-3">{{ loadError }}</p>
            <p v-else-if="loading" class="text-secondary small mb-3">Loading…</p>
            <LineAreaChart
                v-else
                :labels="labels"
                :datasets="datasets"
                :height="height"
                :max="chartMax"
                fill-style="gradient"
            />
        </div>
    </div>
</template>

<script>
import LineAreaChart from '@/components/charts/LineAreaChart.vue'
import { getCompletedSeries } from '@/services/goalsService'

export default {
    name: 'CompletedGraphComponent',
    components: {
        LineAreaChart
    },
    props: {
        height: {
            type: [Number, String],
            default: 160
        }
    },
    data() {
        return {
            loading: false,
            loadError: '',
            labels: [],
            counts: []
        }
    },
    computed: {
        datasets() {
            return [
                {
                    label: 'Completed',
                    data: this.counts,
                    color: '#6ea8fe',
                    fill: true
                }
            ]
        },
        chartMax() {
            const peak = Math.max(0, ...this.counts)
            if (peak <= 0) {
                return 5
            }
            return Math.ceil(peak / 5) * 5
        }
    },
    async created() {
        await this.load()
    },
    methods: {
        async load() {
            this.loading = true
            this.loadError = ''
            try {
                const series = await getCompletedSeries()
                this.labels = series?.labels ?? []
                this.counts = series?.counts ?? []
            } catch (err) {
                this.labels = []
                this.counts = []
                this.loadError = err?.message || 'Failed to load completed goals.'
            } finally {
                this.loading = false
            }
        }
    }
}
</script>
