<template>
    <div ref="chartWrapper" class="w-100" :style="{ height: wrapperHeight }">
        <apexchart type="donut" :height="chartHeight" :options="chartOptions" :series="series" />
    </div>
</template>

<script>
import { chartHeightMixin } from '@/utils/ChartHeightMixin'

export default {
    name: 'DonutChart',
    mixins: [chartHeightMixin],
    props: {
        labels: {
            type: Array,
            required: true
        },
        series: {
            type: Array,
            required: true
        },
        colors: {
            type: Array,
            default: () => []
        },
        showLegend: {
            type: Boolean,
            default: true
        }
    },
    computed: {
        chartOptions() {
            return {
                chart: {
                    type: 'donut',
                    toolbar: { show: false }
                },
                labels: this.labels,
                colors: this.colors.length > 0 ? this.colors : undefined,
                dataLabels: { enabled: false },
                legend: {
                    show: this.showLegend,
                    position: 'bottom',
                    horizontalAlign: 'center',
                    fontSize: '12px'
                },
                stroke: {
                    width: 0
                },
                plotOptions: {
                    pie: {
                        donut: {
                            size: '65%'
                        }
                    }
                },
                tooltip: {
                    theme: 'dark'
                }
            }
        }
    }
}
</script>
