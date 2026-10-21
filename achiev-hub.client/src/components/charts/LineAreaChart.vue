<template>
    <div ref="chartWrapper" class="w-100" :style="{ height: wrapperHeight }">
        <apexchart type="area" :height="chartHeight" :options="chartOptions" :series="series" />
    </div>
</template>

<script>
import { chartHeightMixin } from '@/utils/ChartHeightMixin'

export default {
    name: 'LineAreaChart',
    mixins: [chartHeightMixin],
    props: {
        labels: {
            type: Array,
            required: true
        },
        datasets: {
            type: Array,
            required: true
        },
        title: {
            type: String,
            default: ''
        },
        max: {
            type: Number,
            default: undefined
        },
        xLabelRotate: {
            type: Number,
            default: 0
        },
        xLabelEvery: {
            type: Number,
            default: 1
        },
        showLegend: {
            type: Boolean,
            default: false
        },
        fillStyle: {
            type: String,
            default: 'solid',
            validator: (value) => ['solid', 'gradient'].includes(value)
        }
    },
    computed: {
        labelHeightExtra() {
            return this.xLabelRotate !== 0 ? 48 : 0
        },
        wrapperHeight() {
            if (typeof this.height === 'number') {
                return `${this.height + this.labelHeightExtra}px`
            }
            if (this.labelHeightExtra === 0) {
                return this.height
            }
            return `calc(${this.height} + ${this.labelHeightExtra}px)`
        },
        chartHeight() {
            const extra = this.labelHeightExtra
            if (this.measuredHeight > 0) {
                return this.measuredHeight
            }
            return typeof this.height === 'number' ? this.height + extra : '100%'
        },
        series() {
            return this.datasets.map((ds) => ({
                name: ds.label,
                data: ds.data
            }))
        },
        fillOptions() {
            if (this.fillStyle === 'gradient') {
                return {
                    type: this.datasets.map((ds) => (ds.fill ? 'gradient' : 'solid')),
                    gradient: {
                        shadeIntensity: 1,
                        inverseColors: false,
                        opacityFrom: 0.9,
                        opacityTo: 0.1,
                        stops: [0, 95, 100]
                    },
                    opacity: this.datasets.map((ds) => (ds.fill ? 1 : 0))
                }
            }

            return {
                type: 'solid',
                opacity: this.datasets.map((ds) => (ds.fill ? 0.35 : 0))
            }
        },
        chartOptions() {
            const rotate = this.xLabelRotate
            const every = Math.max(1, this.xLabelEvery)
            const categories = this.labels

            return {
                chart: {
                    type: 'area',
                    toolbar: { show: false },
                    zoom: { enabled: false }
                },
                dataLabels: { enabled: false },
                stroke: {
                    curve: 'smooth',
                    width: 2
                },
                fill: this.fillOptions,
                colors: this.datasets.map((ds) => ds.color),
                title: {
                    text: this.title || undefined,
                    style: { fontSize: '13px', fontWeight: 400 }
                },
                legend: {
                    show: this.showLegend,
                    position: 'bottom',
                    horizontalAlign: 'center'
                },
                xaxis: {
                    categories: this.labels,
                    labels: {
                        rotate,
                        rotateAlways: rotate !== 0,
                        hideOverlappingLabels: true,
                        formatter: (value, _timestamp, opts) => {
                            const index = opts?.i ?? categories.indexOf(value)
                            if (index % every !== 0) {
                                return ''
                            }
                            return value
                        }
                    }
                },
                yaxis: {
                    min: 0,
                    max: this.max
                },
                tooltip: {
                    theme: 'dark',
                    shared: true,
                    intersect: false
                }
            }
        }
    }
}
</script>
