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

        <RecentGamesTable/>
    </AppShell>
</template>

<script>
import { demoAchievementsLast14Days, demoAchievementsPerYear } from '@/data/demo'
import AppShell from '@/components/layout/AppShell.vue'
import AchievementsLast14DaysGraph from '@/components/home/graphs/AchievementsLast14DaysGraph.vue'
import AchievementsPerYearGraph from '@/components/home/graphs/AchievementsPerYearGraph.vue'
import UsersAverageSemiGauge from '@/components/home/graphs/UsersAverageSemiGauge.vue'
import RecentGamesTable from '@/components/home/RecentGamesTable.vue'

export default {
    name: 'HomeComponent',
    components: {
        AppShell,
        AchievementsLast14DaysGraph,
        AchievementsPerYearGraph,
        RecentGamesTable,
        UsersAverageSemiGauge
    },
    data() {
        return {
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
}
</script>
