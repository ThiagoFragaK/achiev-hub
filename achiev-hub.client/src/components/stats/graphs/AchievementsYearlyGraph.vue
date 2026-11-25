<template>
    <div class="card h-100">
        <div class="card-body">
            <p v-if="loadError" class="text-danger small mb-3">{{ loadError }}</p>
            <div v-if="availableYears.length > 0" class="mb-3">
                <div class="d-flex flex-wrap gap-2">
                    <button
                        v-for="year in availableYears"
                        :key="year"
                        type="button"
                        class="btn btn-sm"
                        :class="selectedYears.includes(year) ? 'btn-primary' : 'btn-outline-secondary'"
                        @click="toggleYear(year)"
                    >
                        {{ year }}
                    </button>
                </div>
            </div>
            <LineAreaChart
                :labels="monthLabels"
                :datasets="datasets"
                :height="height"
                :show-legend="true"
                fill-style="gradient"
            />
        </div>
    </div>
</template>

<script>
import LineAreaChart from '@/components/charts/LineAreaChart.vue'
import { getAchievementsByMonth } from '@/services/statsService'

const MONTH_LABELS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']

function colorForYear(year) {
    const hue = Math.abs(year * 47) % 360
    return `hsl(${hue}, 65%, 48%)`
}

export default {
    name: 'AchievementsYearlyGraph',
    components: {
        LineAreaChart
    },
    props: {
        height: {
            type: [Number, String],
            default: 280
        },
        yearRange: {
            type: String,
            default: 'last5'
        }
    },
    emits: ['update:yearRange'],
    data() {
        return {
            loadError: '',
            availableYears: [],
            series: [],
            selectedYears: [],
            monthLabels: MONTH_LABELS
        }
    },
    computed: {
        seriesByYear() {
            const map = new Map()
            for (const item of this.series) {
                map.set(item.year, item.counts ?? [])
            }
            return map
        },
        datasets() {
            return this.selectedYears
                .slice()
                .sort((a, b) => a - b)
                .map((year) => ({
                    label: String(year),
                    data: this.seriesByYear.get(year) ?? Array(12).fill(0),
                    color: colorForYear(year),
                    fill: true
                }))
        }
    },
    watch: {
        yearRange(range) {
            if (range === 'custom') {
                return
            }
            this.applyRange(range)
        }
    },
    created() {
        this.loadData()
    },
    methods: {
        async loadData() {
            this.loadError = ''
            try {
                const data = await getAchievementsByMonth()
                this.availableYears = data?.availableYears ?? []
                this.series = data?.series ?? []
                const range = this.yearRange === 'custom' ? 'last5' : this.yearRange
                this.applyRange(range)
            } catch (err) {
                this.loadError = err?.message || 'Failed to load achievements by month.'
                this.availableYears = []
                this.series = []
                this.selectedYears = []
            }
        },
        applyRange(range) {
            const years = this.availableYears.slice()
            if (range === 'all') {
                this.selectedYears = years
                return
            }
            this.selectedYears = years.slice(0, 5)
        },
        toggleYear(year) {
            if (this.selectedYears.includes(year)) {
                if (this.selectedYears.length === 1) {
                    return
                }
                this.selectedYears = this.selectedYears.filter((y) => y !== year)
            } else {
                this.selectedYears = [...this.selectedYears, year]
            }
            this.syncYearRangeFromSelection()
        },
        syncYearRangeFromSelection() {
            const selected = this.selectedYears
            const allSelected =
                selected.length === this.availableYears.length &&
                this.availableYears.every((y) => selected.includes(y))
            const last5 = this.availableYears.slice(0, 5)
            const isLast5 =
                selected.length === last5.length &&
                last5.every((y) => selected.includes(y))
            if (allSelected) {
                this.$emit('update:yearRange', 'all')
            } else if (isLast5) {
                this.$emit('update:yearRange', 'last5')
            } else {
                this.$emit('update:yearRange', 'custom')
            }
        }
    }
}
</script>
