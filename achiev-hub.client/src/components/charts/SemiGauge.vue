<template>
    <div class="d-flex flex-column align-items-center gap-2">
        <p class="text-uppercase text-secondary small fw-medium mb-0">
            {{ label }}
        </p>
        <div class="w-100" style="max-width: 220px">
            <apexchart type="radialBar" height="200" :options="chartOptions" :series="series" />
        </div>
    </div>
</template>

<script>
export default {
    name: 'SemiGauge',
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
                plotOptions: {
                    radialBar: {
                        startAngle: -90,
                        endAngle: 90,
                        hollow: {
                            size: '60%'
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
