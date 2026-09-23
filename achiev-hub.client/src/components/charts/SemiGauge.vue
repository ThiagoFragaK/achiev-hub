<template>
    <div class="d-flex flex-column align-items-center gap-2">
        <p class="text-uppercase text-secondary small fw-medium mb-0">
            {{ label }}
        </p>
        <div
            ref="chartWrapper"
            class="w-100"
            :style="{ height: wrapperHeight, maxWidth: '320px' }"
        >
            <apexchart type="radialBar" :height="chartHeight" :options="chartOptions" :series="series" />
        </div>
    </div>
</template>

<script>
import { chartHeightMixin } from '@/utils/ChartHeightMixin'

export default {
    name: 'SemiGauge',
    mixins: [chartHeightMixin],
    props: {
        value: {
            type: Number,
            default: 67
        },
        label: {
            type: String,
            default: 'Users Average'
        }
    },
    computed: {
        series() {
            return [this.value]
        },
        chartOptions() {
            return {
                chart: {
                    type: 'radialBar',
                    sparkline: { enabled: true }
                },
                stroke: {
                    lineCap: 'round'
                },
                plotOptions: {
                    radialBar: {
                        startAngle: -90,
                        endAngle: 90,
                        // Drops the arc so it lines up with the plot area of the
                        // neighbouring line charts instead of riding above them.
                        offsetY: 24,
                        hollow: {
                            size: '62%'
                        },
                        track: {
                            background: '#e9ecef',
                            strokeWidth: '97%'
                        },
                        dataLabels: {
                            name: { show: false },
                            value: {
                                offsetY: -10,
                                fontSize: '1.75rem',
                                fontWeight: 600,
                                fontFamily:
                                    'Montserrat, system-ui, -apple-system, "Segoe UI", Roboto, sans-serif',
                                formatter(val) {
                                    return `${val}%`
                                }
                            }
                        }
                    }
                },
                colors: ['#0d6efd'],
                labels: [this.label]
            }
        }
    }
}
</script>
