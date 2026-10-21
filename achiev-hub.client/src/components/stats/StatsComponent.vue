<template>
    <AppShell>
        <h1 class="lastica-h3 mb-4">Stats</h1>
        <div class="row g-4">
            <div class="col-12 col-lg-8">
                <AchievementsYearlyGraph :height="chartHeight" />
            </div>
            <div class="col-12 col-lg-4">
                <GamesPercentageBandGraph :height="chartHeight" />
            </div>
        </div>
    </AppShell>
</template>

<script>
import AppShell from '@/components/layout/AppShell.vue'
import AchievementsYearlyGraph from '@/components/stats/graphs/AchievementsYearlyGraph.vue'
import GamesPercentageBandGraph from '@/components/stats/graphs/GamesPercentageBandGraph.vue'

const CHART_HEIGHT = 'clamp(220px, 22vw, 320px)'

export default {
    name: 'StatsComponent',
    components: {
        AppShell,
        AchievementsYearlyGraph,
        GamesPercentageBandGraph
    },
    data() {
        return {
            chartHeight: CHART_HEIGHT
        }
    }
}
</script>
