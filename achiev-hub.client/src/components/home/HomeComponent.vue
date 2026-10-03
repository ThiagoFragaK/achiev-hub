<template>
    <AppShell>
        <p v-if="statsError" class="text-danger small mb-3">{{ statsError }}</p>

        <div class="row g-4 mb-4">
            <div class="col-12 col-lg">
                <AchievementsLast14DaysGraph
                    :labels="achievementsLabels"
                    :datasets="achievementsData"
                    :height="chartHeight"
                />
            </div>

            <div class="col-12 col-lg-3">
                <UsersAverageSemiGauge
                    :value="averagePercentage"
                    :total-achievements="totalAchievements"
                    :games-with-achievements="gamesWithAchievements"
                    :height="chartHeight"
                />
            </div>

            <div class="col-12 col-lg">
                <AchievementsPerYearGraph
                    :labels="perYearLabels"
                    :datasets="perYearData"
                    :height="chartHeight"
                />
            </div>
        </div>

        <div class="d-flex align-items-center justify-content-between gap-3 mb-3">
            <h2 class="h5 mb-0">Recent Games</h2>
        </div>

        <RecentGamesTable :key="recentGamesKey" :hide-title="true" />
    </AppShell>
</template>

<script>
import { getUser } from '@/lib/session'
import { getUserStats } from '@/services/statsService'
import AppShell from '@/components/layout/AppShell.vue'
import AchievementsLast14DaysGraph from '@/components/home/graphs/AchievementsLast14DaysGraph.vue'
import AchievementsPerYearGraph from '@/components/home/graphs/AchievementsPerYearGraph.vue'
import UsersAverageSemiGauge from '@/components/home/graphs/UsersAverageSemiGauge.vue'
import RecentGamesTable from '@/components/home/RecentGamesTable.vue'

const dayFormatter = new Intl.DateTimeFormat('en-GB', { day: 'numeric', month: 'short' })
const CHART_HEIGHT = 'clamp(180px, 18vw, 280px)'

export default {
    name: 'HomeComponent',
    components: {
        AppShell,
        AchievementsLast14DaysGraph,
        AchievementsPerYearGraph,
        RecentGamesTable,
        UsersAverageSemiGauge,
    },
    data() {
        return {
            chartHeight: CHART_HEIGHT,
            recentGamesKey: 0,
            statsError: '',
            achievementsLabels: [],
            achievementsCounts: [],
            perYearLabels: [],
            perYearCounts: [],
            averagePercentage: 0,
            totalAchievements: 0,
            gamesWithAchievements: 0
        }
    },
    computed: {
        achievementsData() {
            return [
                {
                    label: 'Achievements',
                    data: this.achievementsCounts,
                    color: '#0d6efd'
                }
            ]
        },
        perYearData() {
            return [
                {
                    label: 'Achievements',
                    data: this.perYearCounts,
                    color: '#0d6efd',
                    fill: true
                }
            ]
        }
    },
    methods: {
        formatDay(isoDate) {
            return dayFormatter.format(new Date(`${isoDate}T00:00:00`))
        },
        async getStats() {
            this.statsError = ''
            try {
                const stats = await getUserStats()
                const days = stats?.achievementsLast14Days ?? []
                const years = stats?.achievementsPerYear ?? []

                this.achievementsLabels = days.map((day) => this.formatDay(day.date))
                this.achievementsCounts = days.map((day) => day.count)
                this.perYearLabels = years.map((year) => String(year.year))
                this.perYearCounts = years.map((year) => year.count)
                this.averagePercentage = Math.round(stats?.averagePercentage ?? 0)
                this.totalAchievements = stats?.totalAchievements ?? 0
                this.gamesWithAchievements = stats?.gamesWithAchievements ?? 0
            } catch (err) {
                this.statsError = err?.message || 'Failed to load your statistics.'
            }
        },
    },
    async created() {
        await this.getStats()
    }
}
</script>
