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
            <button
                v-if="canSyncLibrary"
                type="button"
                class="btn btn-outline-secondary btn-sm"
                aria-label="Sync library from Steam"
                :disabled="isSyncing"
                @click="onSyncLibrary"
            >
                <LucideIcon icon="RefreshCw" :size="18" />
            </button>
        </div>
        <p v-if="syncError" class="text-danger small mb-3">{{ syncError }}</p>

        <RecentGamesTable :key="recentGamesKey" :hide-title="true" />
    </AppShell>
</template>

<script>
import { getUser } from '@/lib/session'
import { syncLibrary } from '@/services/gamesService'
import { getUserStats } from '@/services/statsService'
import AppShell from '@/components/layout/AppShell.vue'
import AchievementsLast14DaysGraph from '@/components/home/graphs/AchievementsLast14DaysGraph.vue'
import AchievementsPerYearGraph from '@/components/home/graphs/AchievementsPerYearGraph.vue'
import UsersAverageSemiGauge from '@/components/home/graphs/UsersAverageSemiGauge.vue'
import RecentGamesTable from '@/components/home/RecentGamesTable.vue'
import LucideIcon from '@/components/global/LucideIcon.vue'

const dayFormatter = new Intl.DateTimeFormat('en-GB', { day: 'numeric', month: 'short' })

// The charts follow the viewport width, within a readable range.
const CHART_HEIGHT = 'clamp(180px, 18vw, 280px)'

export default {
    name: 'HomeComponent',
    components: {
        AppShell,
        AchievementsLast14DaysGraph,
        AchievementsPerYearGraph,
        RecentGamesTable,
        UsersAverageSemiGauge,
        LucideIcon
    },
    data() {
        return {
            chartHeight: CHART_HEIGHT,
            recentGamesKey: 0,
            isSyncing: false,
            syncError: '',
            statsError: '',
            achievementsLabels: [],
            achievementsCounts: [],
            perYearLabels: [],
            perYearCounts: [],
            averagePercentage: 0
        }
    },
    computed: {
        canSyncLibrary() {
            const user = getUser()
            return !!user && user.role !== 'guest'
        },
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
            // The API sends a plain calendar day, so it is parsed as local time.
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
            } catch (err) {
                this.statsError = err?.message || 'Failed to load your statistics.'
            }
        },
        async onSyncLibrary() {
            this.isSyncing = true
            this.syncError = ''
            try {
                await syncLibrary()
                this.recentGamesKey++
                await this.getStats()
            } catch (err) {
                this.syncError = err?.message || 'Failed to sync library.'
            } finally {
                this.isSyncing = false
            }
        }
    },
    async created() {
        await this.getStats()
    }
}
</script>
