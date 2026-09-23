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
        }
    },
    computed: {
        series() {
            return this.datasets.map((ds) => ({
                name: ds.label,
                data: ds.data
            }))
        },
        chartOptions() {
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
                fill: {
                    type: 'solid',
                    opacity: this.datasets.map((ds) => (ds.fill ? 0.35 : 0))
                },
                colors: this.datasets.map((ds) => ds.color),
                title: {
                    text: this.title || undefined,
                    style: { fontSize: '13px', fontWeight: 400 }
                },
                legend: { show: false },
                xaxis: {
                    categories: this.labels,
                    // Rotation is disabled so every chart reserves the same label
                    // strip and therefore the same plot area height.
                    labels: {
                        rotate: 0,
                        rotateAlways: false,
                        hideOverlappingLabels: true
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
