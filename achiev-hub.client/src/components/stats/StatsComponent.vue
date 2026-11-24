<template>
    <AppShell>
        <h1 class="lastica-h3 mb-4">Stats</h1>
        <div class="row g-4">
            <div class="col-12 col-lg-8">
                <TabsComponent v-model="activeYearlyTab" :tabs="yearlyTabs">
                    <template v-if="activeYearlyTab === 'yearly-achievements'" #append>
                        <div class="btn-group" role="group" aria-label="Year range">
                            <button
                                v-for="option in yearRangeOptions"
                                :key="option.value"
                                type="button"
                                class="btn btn-sm"
                                :class="yearlyYearRange === option.value ? 'btn-primary' : 'btn-outline-secondary'"
                                @click="yearlyYearRange = option.value"
                            >
                                {{ option.label }}
                            </button>
                        </div>
                    </template>
                    <template #yearly-achievements>
                        <AchievementsYearlyGraph
                            v-model:year-range="yearlyYearRange"
                            :height="chartHeight"
                        />
                    </template>
                    <template #yearly-completion>
                        <CompletedGamesYearlyGraph :height="chartHeight" />
                    </template>
                </TabsComponent>
            </div>
            <div class="col-12 col-lg-4">
                <GamesPercentageBandGraph :height="chartHeight" />
            </div>
            <div class="col-12">
                <div class="stats-list-panel">
                    <TabsComponent v-model="activeListTab" :tabs="listTabs">
                        <template #achievements-by-month>
                            <AchievementsByMonthList />
                        </template>
                        <template #games-in-tier>
                            <GamesPercentageBandList />
                        </template>
                    </TabsComponent>
                </div>
            </div>
        </div>
    </AppShell>
</template>

<script>
import AppShell from '@/components/layout/AppShell.vue'
import TabsComponent from '@/components/global/TabsComponent.vue'
import AchievementsYearlyGraph from '@/components/stats/graphs/AchievementsYearlyGraph.vue'
import GamesPercentageBandGraph from '@/components/stats/graphs/GamesPercentageBandGraph.vue'
import CompletedGamesYearlyGraph from '@/components/stats/graphs/CompletedGamesYearlyGraph.vue'
import AchievementsByMonthList from '@/components/stats/AchievementsByMonthList.vue'
import GamesPercentageBandList from '@/components/stats/GamesPercentageBandList.vue'

const CHART_HEIGHT = 'clamp(220px, 22vw, 320px)'

export default {
    name: 'StatsComponent',
    components: {
        AppShell,
        TabsComponent,
        AchievementsYearlyGraph,
        GamesPercentageBandGraph,
        CompletedGamesYearlyGraph,
        AchievementsByMonthList,
        GamesPercentageBandList
    },
    data() {
        return {
            chartHeight: CHART_HEIGHT,
            activeYearlyTab: 'yearly-achievements',
            yearlyYearRange: 'last5',
            yearRangeOptions: [
                { value: 'last5', label: 'Last 5 years' },
                { value: 'all', label: 'All' }
            ],
            yearlyTabs: [
                {
                    name: 'yearly-achievements',
                    label: 'Yearly Achievements'
                },
                {
                    name: 'yearly-completion',
                    label: 'Yearly Completion'
                }
            ],
            activeListTab: 'achievements-by-month',
            listTabs: [
                {
                    name: 'achievements-by-month',
                    label: 'Achievements unlocked'
                },
                {
                    name: 'games-in-tier',
                    label: 'Games by tiers'
                }
            ]
        }
    }
}
</script>

<style scoped>
.stats-list-panel {
    max-height: calc(100dvh - 14rem);
    overflow-y: auto;
}
</style>
