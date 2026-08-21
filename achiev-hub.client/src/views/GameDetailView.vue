<template>
    <AppShell>
        <div class="row g-4 mb-4 align-items-start">
            <div class="col-md-3 col-lg-2">
                <div
                    class="bg-secondary-subtle border rounded ratio ratio-1x1"
                    aria-hidden="true"
                />
            </div>

            <div class="col-md-5 col-lg-4">
                <h1 class="h2 text-uppercase mb-3">
                    {{ game.name }}
                </h1>
                <div class="row g-3">
                    <div class="col-sm-6">
                        <p class="text-uppercase text-secondary small mb-1">Total game hours</p>
                        <p class="fs-4 fw-semibold mb-0">{{ game.hours }}h</p>
                    </div>
                    <div class="col-sm-6">
                        <p class="text-uppercase text-secondary small mb-1">Percentage</p>
                        <p class="fs-4 fw-semibold mb-0">{{ game.percentage }}%</p>
                    </div>
                </div>
            </div>

            <div class="col-md-4 col-lg-4">
                <div class="card">
                    <div class="card-body">
                        <LineAreaChart
                            title="Percentage per year."
                            :labels="percentageLabels"
                            :datasets="percentageData"
                            :max="40"
                        />
                    </div>
                </div>
            </div>

            <div class="col-lg-2 text-lg-end">
                <button
                    type="button"
                    class="btn btn-outline-secondary btn-sm"
                    aria-label="Sync game data"
                >
                    <RefreshCw :size="18" />
                </button>
            </div>
        </div>

        <div class="card">
            <div class="card-body">
                <div class="d-flex align-items-center justify-content-between gap-3 mb-3">
                    <h2 class="h5 mb-0">Games Achievements</h2>
                    <button
                        type="button"
                        class="btn btn-outline-secondary btn-sm"
                        aria-label="Filter achievements"
                    >
                        <Funnel :size="18" />
                    </button>
                </div>

                <div class="table-responsive">
                    <table class="table table-bordered table-hover text-center mb-0 align-middle">
                        <thead class="table-primary">
                            <tr>
                                <th scope="col">Achievement</th>
                                <th scope="col">Status</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr v-for="achievement in demoAchievements" :key="achievement.id">
                                <td>
                                    {{ achievement.name }}
                                </td>
                                <td>
                                    {{ achievement.unlocked ? 'Unlocked' : 'Locked' }}
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
import { Funnel, RefreshCw } from '@lucide/vue'
import AppShell from '@/components/layout/AppShell.vue'
import LineAreaChart from '@/components/charts/LineAreaChart.vue'
import { demoAchievements, demoGames } from '@/data/demo'

export default {
    name: 'GameDetailView',
    components: {
        AppShell,
        LineAreaChart,
        Funnel,
        RefreshCw
    },
    data() {
        return {
            demoAchievements,
            percentageLabels: ['ITEM 1', 'ITEM 2', 'ITEM 3', 'ITEM 4', 'ITEM 5'],
            percentageData: [
                {
                    label: 'Percentage',
                    data: [8, 14, 20, 28, 36],
                    color: '#0d6efd'
                }
            ]
        }
    },
    computed: {
        game() {
            const id = String(this.$route.params.id)
            return demoGames.find((g) => g.id === id) ?? demoGames[0]
        }
    }
}
</script>
