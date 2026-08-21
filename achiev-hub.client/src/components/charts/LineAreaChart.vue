<script setup lang="ts">
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Filler,
  Tooltip,
  Legend,
} from 'chart.js'
import { Line } from 'vue-chartjs'
import { computed } from 'vue'

ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Filler,
  Tooltip,
  Legend,
)

const props = withDefaults(defineProps<{
  labels: string[]
  datasets: Array<{
    label: string
    data: number[]
    color: string
    fill?: boolean
  }>
  title?: string
  max?: number
}>(), {
  title: '',
  max: undefined,
})

const chartData = computed(() => ({
  labels: props.labels,
  datasets: props.datasets.map((ds) => ({
    label: ds.label,
    data: ds.data,
    borderColor: ds.color,
    backgroundColor: ds.fill ? `${ds.color}55` : 'transparent',
    pointBackgroundColor: ds.color,
    pointBorderColor: ds.color,
    fill: ds.fill ?? false,
    tension: 0.35,
  })),
}))

const chartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: { display: false },
    title: {
      display: Boolean(props.title),
      text: props.title,
      color: '#FFFECB',
      font: { size: 13, weight: 'normal' as const },
    },
  },
  scales: {
    x: {
      ticks: { color: '#D7D6D0', maxRotation: 45, minRotation: 0 },
      grid: { color: '#37264966' },
      border: { color: '#372649' },
    },
    y: {
      min: 0,
      max: props.max,
      ticks: { color: '#D7D6D0' },
      grid: { color: '#37264966' },
      border: { color: '#372649' },
    },
  },
}))
</script>

<template>
  <div class="h-48 w-full sm:h-56">
    <Line :data="chartData" :options="chartOptions" />
  </div>
</template>
