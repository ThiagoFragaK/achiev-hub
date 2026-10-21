<template>
    <div class="card h-100">
        <div class="card-body">
            <div class="d-flex flex-wrap align-items-center gap-2 mb-3">
                <p class="text-uppercase text-secondary small fw-medium mb-0 me-auto">
                    Games by completion
                </p>
                <div class="btn-group" role="group" aria-label="Band filter">
                    <button
                        type="button"
                        class="btn btn-sm"
                        :class="bandFilter === 'all' ? 'btn-primary' : 'btn-outline-secondary'"
                        @click="setBandFilter('all')"
                    >
                        All
                    </button>
                    <button
                        type="button"
                        class="btn btn-sm"
                        :class="bandFilter === 'custom' ? 'btn-primary' : 'btn-outline-secondary'"
                        @click="toggleBandPicker"
                    >
                        Filter
                    </button>
                </div>
            </div>
            <p v-if="loadError" class="text-danger small mb-3">{{ loadError }}</p>
            <div v-if="showBandPicker" class="mb-3">
                <div class="d-flex flex-wrap gap-2">
                    <button
                        v-for="band in bands"
                        :key="band.key"
                        type="button"
                        class="btn btn-sm"
                        :class="selectedKeys.includes(band.key) ? 'btn-primary' : 'btn-outline-secondary'"
                        @click="toggleBand(band.key)"
                    >
                        {{ band.label }}
                    </button>
                </div>
            </div>
            <DonutChart
                :labels="chartLabels"
                :series="chartSeries"
                :colors="chartColors"
                :height="height"
                :show-legend="true"
            />
        </div>
    </div>
</template>

<script>
import DonutChart from '@/components/charts/DonutChart.vue'
import { getGamesPercentageBands } from '@/services/statsService'

const BAND_COLORS = {
    '100': '#73C2C1',
    gt80: '#32329E',
    gt60: '#419F3E',
    gt50: '#6f42c1',
    gt30: '#CC8636',
    gt10: '#D37473',
    gt0: '#C92121',
    '0': '#6c757d'
}

export default {
    name: 'GamesPercentageBandGraph',
    components: {
        DonutChart
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
            bands: [],
            bandFilter: 'all',
            selectedKeys: [],
            showBandPicker: false
        }
    },
    computed: {
        visibleBands() {
            if (this.bandFilter === 'all' || this.selectedKeys.length === 0) {
                return this.bands
            }
            return this.bands.filter((band) => this.selectedKeys.includes(band.key))
        },
        chartLabels() {
            return this.visibleBands.map((band) => `${band.label}`)
        },
        chartSeries() {
            return this.visibleBands.map((band) => band.count)
        },
        chartColors() {
            return this.visibleBands.map((band) => BAND_COLORS[band.key] ?? '#0d6efd')
        }
    },
    created() {
        this.loadData()
    },
    methods: {
        async loadData() {
            this.loadError = ''
            try {
                const data = await getGamesPercentageBands()
                this.bands = data?.bands ?? []
                this.selectedKeys = this.bands.map((b) => b.key)
                this.bandFilter = 'all'
            } catch (err) {
                this.loadError = err?.message || 'Failed to load games by completion.'
                this.bands = []
                this.selectedKeys = []
            }
        },
        setBandFilter(filter) {
            this.bandFilter = filter
            if (filter === 'all') {
                this.selectedKeys = this.bands.map((b) => b.key)
                this.showBandPicker = false
            }
        },
        toggleBandPicker() {
            this.showBandPicker = !this.showBandPicker
            if (this.showBandPicker) {
                this.bandFilter = 'custom'
            }
        },
        toggleBand(key) {
            if (this.selectedKeys.includes(key)) {
                if (this.selectedKeys.length === 1) {
                    return
                }
                this.selectedKeys = this.selectedKeys.filter((k) => k !== key)
            } else {
                this.selectedKeys = [...this.selectedKeys, key]
            }
            const allSelected = this.selectedKeys.length === this.bands.length
            this.bandFilter = allSelected ? 'all' : 'custom'
        }
    }
}
</script>
