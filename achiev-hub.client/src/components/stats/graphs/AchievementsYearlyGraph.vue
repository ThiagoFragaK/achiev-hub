<template>
    <div class="card h-100">
        <div class="card-body">
            <div class="d-flex flex-wrap align-items-center gap-2 mb-3">
                <p class="text-uppercase text-secondary small fw-medium mb-0 me-auto">
                    Yearly achievements by month
                </p>
                <div class="btn-group" role="group" aria-label="Year range">
                    <button
                        v-for="option in rangeOptions"
                        :key="option.value"
                        type="button"
                        class="btn btn-sm"
                        :class="yearRange === option.value ? 'btn-primary' : 'btn-outline-secondary'"
                        @click="setYearRange(option.value)"
                    >
                        {{ option.label }}
                    </button>
                </div>
                <button
                    v-if="availableYears.length > 0"
                    type="button"
                    class="btn btn-sm"
                    :class="showYearPicker ? 'btn-primary' : 'btn-outline-secondary'"
                    @click="showYearPicker = !showYearPicker"
                >
                    Years
                </button>
            </div>
            <p v-if="loadError" class="text-danger small mb-3">{{ loadError }}</p>
            <div v-if="showYearPicker && availableYears.length > 0" class="mb-3">
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
        }
    },
    data() {
        return {
            loadError: '',
            availableYears: [],
            series: [],
            yearRange: 'last5',
            selectedYears: [],
            showYearPicker: false,
            monthLabels: MONTH_LABELS,
            rangeOptions: [
                { value: 'last5', label: 'Last 5 years' },
                { value: 'all', label: 'All' }
            ]
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
                this.applyRange(this.yearRange)
            } catch (err) {
                this.loadError = err?.message || 'Failed to load achievements by month.'
                this.availableYears = []
                this.series = []
                this.selectedYears = []
            }
        },
        setYearRange(range) {
            this.yearRange = range
            this.applyRange(range)
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
                this.yearRange = 'all'
            } else if (isLast5) {
                this.yearRange = 'last5'
            } else {
                this.yearRange = 'custom'
            }
        }
    }
}
</script>
