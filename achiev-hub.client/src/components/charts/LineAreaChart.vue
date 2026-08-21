<template>
    <div class="w-100" style="min-height: 14rem">
        <apexchart type="area" height="220" :options="chartOptions" :series="series" />
    </div>
</template>

<script>
export default {
    name: 'LineAreaChart',
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
                    categories: this.labels
                },
                yaxis: {
                    min: 0,
                    max: this.max
                },
                tooltip: {
                    shared: true,
                    intersect: false
                }
            }
        }
    }
}
</script>
