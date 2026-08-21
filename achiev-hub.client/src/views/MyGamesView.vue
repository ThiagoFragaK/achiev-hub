<template>
    <AppShell>
        <div class="d-flex align-items-center gap-2 mb-4">
            <button
                type="button"
                class="btn btn-outline-secondary btn-sm"
                aria-label="Filter games"
            >
                <Funnel :size="18" />
            </button>
            <h1 class="h2 mb-0">My Games</h1>
        </div>

        <div class="card">
            <div class="card-body">
                <div class="table-responsive">
                    <table class="table table-bordered table-hover text-center mb-0 align-middle">
                        <thead class="table-primary">
                            <tr>
                                <th scope="col">Game</th>
                                <th scope="col">Hours</th>
                                <th scope="col">Percentage</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr v-for="game in demoGames" :key="game.id">
                                <td>
                                    <RouterLink
                                        :to="{ name: 'game-detail', params: { id: game.id } }"
                                        class="fw-medium"
                                    >
                                        {{ game.name }}
                                    </RouterLink>
                                </td>
                                <td>{{ game.hours }}h</td>
                                <td>{{ game.percentage }}%</td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    </AppShell>
</template>

<script>
import { Funnel } from '@lucide/vue'
import AppShell from '@/components/layout/AppShell.vue'
import { demoGames } from '@/data/demo'

export default {
    name: 'MyGamesView',
    components: {
        AppShell,
        Funnel
    },
    data() {
        return {
            demoGames
        }
    }
}
</script>
