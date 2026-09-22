<template>
    <AppShell>
        <div class="row g-4 mb-4">
            <div class="col-lg">
                <AchievementsLast14DaysGraph
                    :labels="achievementsLabels"
                    :datasets="achievementsData"
                    :max="8"
                />
            </div>

            <div class="col-lg-3">
                <UsersAverageSemiGauge :value="67" />
            </div>

            <div class="col-lg">
                <AchievementsPerYearGraph
                    :labels="perYearLabels"
                    :datasets="perYearData"
                    :max="100"
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
import { demoAchievementsLast14Days, demoAchievementsPerYear } from '@/data/demo'
import { getUser } from '@/lib/session'
import { syncLibrary } from '@/services/gamesService'
import AppShell from '@/components/layout/AppShell.vue'
import AchievementsLast14DaysGraph from '@/components/home/graphs/AchievementsLast14DaysGraph.vue'
import AchievementsPerYearGraph from '@/components/home/graphs/AchievementsPerYearGraph.vue'
import UsersAverageSemiGauge from '@/components/home/graphs/UsersAverageSemiGauge.vue'
import RecentGamesTable from '@/components/home/RecentGamesTable.vue'
import LucideIcon from '@/components/global/LucideIcon.vue'

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
            recentGamesKey: 0,
            isSyncing: false,
            syncError: '',
            achievementsLabels: demoAchievementsLast14Days.labels,
            achievementsData: [
                {
                    label: 'Achievements',
                    data: demoAchievementsLast14Days.data,
                    color: '#0d6efd'
                }
            ],
            perYearLabels: demoAchievementsPerYear.labels,
            perYearData: [
                {
                    label: 'Achievements',
                    data: demoAchievementsPerYear.data,
                    color: '#0d6efd',
                    fill: true
                }
            ]
        }
    },
    computed: {
        canSyncLibrary() {
            const user = getUser()
            return !!user && user.role !== 'guest'
        }
    },
    methods: {
        async onSyncLibrary() {
            this.isSyncing = true
            this.syncError = ''
            try {
                await syncLibrary()
                this.recentGamesKey++
            } catch (err) {
                this.syncError = err?.message || 'Failed to sync library.'
            } finally {
                this.isSyncing = false
            }
        }
    }
}
</script>
