<template>
    <AppShell>
        <div class="row g-4 mb-4">
            <div class="col-lg-4">
                <div class="card h-100">
                    <div class="card-body">
                        <p class="text-uppercase text-secondary small fw-medium mb-3">
                            Achievements in 14 days
                        </p>
                        <LineAreaChart
                            :labels="achievementsLabels"
                            :datasets="achievementsData"
                            :max="40"
                        />
                    </div>
                </div>
            </div>

            <div class="col-lg-4">
                <div class="card h-100">
                    <div class="card-body d-flex align-items-center justify-content-center">
                        <SemiGauge :value="67" label="Users Average" />
                    </div>
                </div>
            </div>

            <div class="col-lg-4">
                <div class="card h-100">
                    <div class="card-body">
                        <p class="text-uppercase text-secondary small fw-medium mb-3">Per Years</p>
                        <LineAreaChart :labels="perYearLabels" :datasets="perYearData" :max="50" />
                    </div>
                </div>
            </div>
        </div>

        <div class="card">
            <div class="card-body">
                <h2 class="h5 mb-3">76hrs in last 14 days</h2>
                <div class="table-responsive">
                    <table class="table table-bordered table-sm text-center mb-0">
                        <thead class="table-primary">
                            <tr>
                                <th v-for="game in demoRecentHours" :key="game.name" scope="col">
                                    {{ game.name }}
                                </th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr>
                                <td v-for="game in demoRecentHours" :key="`${game.name}-hours`">
                                    {{ game.hours }}
                                </td>
                            </tr>
                            <tr>
                                <td
                                    v-for="game in demoRecentHours"
                                    :key="`${game.name}-percentage`"
                                >
                                    {{ game.percentage }}
                                </td>
                            </tr>
                            <tr>
                                <td v-for="game in demoRecentHours" :key="`${game.name}-status`">
                                    {{ game.status }}
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    </AppShell>
</template>

<script>
import AppShell from '@/components/layout/AppShell.vue'
import LineAreaChart from '@/components/charts/LineAreaChart.vue'
import SemiGauge from '@/components/charts/SemiGauge.vue'
import { demoRecentHours } from '@/data/demo'

export default {
    name: 'HomeView',
    components: {
        AppShell,
        LineAreaChart,
        SemiGauge
    },
    data() {
        return {
            demoRecentHours,
            achievementsLabels: ['2021', '2023', '2025'],
            achievementsData: [
                {
                    label: 'Achievements',
                    data: [10, 24, 38],
                    color: '#0d6efd'
                }
            ],
            perYearLabels: [
                'JAN',
                'FEV',
                'MAR',
                'APR',
                'MAY',
                'JUN',
                'JUL',
                'AUG',
                'SEP',
                'OCT',
                'NOV',
                'DEC'
            ],
            perYearData: [
                {
                    label: 'Series A',
                    data: [12, 18, 15, 22, 28, 25, 30, 27, 32, 35, 30, 40],
                    color: '#0d6efd',
                    fill: true
                },
                {
                    label: 'Series B',
                    data: [8, 14, 20, 18, 24, 30, 26, 34, 28, 36, 42, 38],
                    color: '#6610f2',
                    fill: true
                }
            ]
        }
    }
}
</script>
