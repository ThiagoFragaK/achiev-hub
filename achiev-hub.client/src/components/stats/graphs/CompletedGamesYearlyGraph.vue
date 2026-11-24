<template>
    <div class="card h-100">
        <div class="card-body">
            <p v-if="loadError" class="text-danger small mb-3">{{ loadError }}</p>
            <LineAreaChart
                :labels="labels"
                :datasets="datasets"
                :max="chartMax"
                :height="height"
                fill-style="gradient"
            />
        </div>
    </div>
</template>

<script>
import LineAreaChart from '@/components/charts/LineAreaChart.vue'
import { getCompletedGamesByYear } from '@/services/statsService'

export default {
    name: 'CompletedGamesYearlyGraph',
    components: {
        LineAreaChart
    },
    props: {
        height: {
            type: [Number, String],
            default: 280
        }
    },
    data() {
        return {
            loadError: '',
            availableYears: [],
            series: []
        }
    },
    computed: {
        countByYear() {
            const map = new Map()
            for (const item of this.series) {
                map.set(item.year, item.count ?? 0)
            }
            return map
        },
        displayYears() {
            return this.availableYears.slice().sort((a, b) => a - b)
        },
        labels() {
            return this.displayYears.map((year) => String(year))
        },
        counts() {
            return this.displayYears.map((year) => this.countByYear.get(year) ?? 0)
        },
        datasets() {
            return [
                {
                    label: '100% games',
                    data: this.counts,
                    color: '#73C2C1',
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
    created() {
        this.loadData()
    },
    methods: {
        async loadData() {
            this.loadError = ''
            try {
                const data = await getCompletedGamesByYear()
                this.availableYears = data?.availableYears ?? []
                this.series = data?.series ?? []
            } catch (err) {
                this.loadError = err?.message || 'Failed to load 100% games by year.'
                this.availableYears = []
                this.series = []
            }
        }
    }
}
</script>
