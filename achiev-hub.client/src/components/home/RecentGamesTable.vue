<template>
    <h2 v-if="!hideTitle" class="h5 mb-3">Recent Games</h2>
    <TableComponent 
        :data="table.data" 
        :columns="table.columns" 
        :isLoading="table.isLoading"
    >
        <template #cell-name="{ data }">
            <div class="d-flex align-items-center gap-2">
                <img
                    v-if="iconUrl(data.row)"
                    :src="iconUrl(data.row)"
                    :alt="data.row.name"
                    width="32"
                    height="32"
                    class="rounded"
                />
                <RouterLink
                    :to="{ name: 'game-detail', params: { id: data.row.appId } }"
                    class="fw-medium"
                >
                    {{ data.row.name }}
                </RouterLink>
            </div>
        </template>
        <template #cell-playTimeWeeks="{ data }">
            {{ data.row.playTimeWeeks }} hours
        </template>
        <template #cell-playTimeTotal="{ data }">
            {{ data.row.playTimeTotal }} hours
        </template>
        <template #cell-achievements="{ data }">
            <ProgressComponent 
                :value="data.row.achievements.percentage" 
                :label="data.row.achievements.percentage + '%'" 
                :color="getColor(data.row.achievements.percentage)"
            />
            <span class="text-muted">
                {{ data.row.achievements.unlocked }}/{{ data.row.achievements.total }}
            </span>
        </template>
    </TableComponent>
</template>

<script>
import { getSessionSteamId, steamAppIconUrl } from '@/lib/steam';
import { getRecentGames } from '@/services/gamesService';
import TableComponent from '@/components/global/TableComponent.vue';
import ProgressComponent from '@/components/global/ProgressComponent.vue';

export default {
    name: 'RecentGamesTable',
    components: {
        TableComponent,
        ProgressComponent
    },
    props: {
        hideTitle: {
            type: Boolean,
            default: false
        }
    },
    data() {
        return {
            steamId: null,
            table: {
                isLoading: true,
                data: [],
                columns: [
                    {
                        label: 'Game',
                        key: 'name',
                        type: 'link',
                        to: { name: 'game-detail', params: { id: 'appId' } }
                    },
                    {
                        label: '2 weeks (h)',
                        key: 'playTimeWeeks'
                    },
                    {
                        label: 'Total (h)',
                        key: 'playTimeTotal'
                    },
                    {
                        label: 'Achievements',
                        key: 'achievements'
                    }
                ]
            }
        }
    },
    methods: {
        iconUrl(game) {
            return steamAppIconUrl(game.appId, game.image)
        },
        getColor(percentage) {
            if(percentage == 100) {
                return 'success';
            }
            return percentage > 50 ? 'info' : 'danger';
        },
        async setSteamId() {
            this.steamId = await getSessionSteamId();
        },
        async getUsersRecentGames() {
            if (!this.steamId) {
                this.error = 'Steam ID is missing from your session.'
                this.table.data = []
                return
            }

            this.table.isLoading = true;
            try {
                const result = await getRecentGames(this.steamId);
                this.table.data = result.data ?? [];
            } catch (err) {
                this.table.data = [];
                this.error = err?.message || 'Failed to load recent games.';
            } finally {
                this.table.isLoading = false;
            }
        }
    },
    async created() {
        await this.setSteamId();
        await this.getUsersRecentGames();
    }
}
</script>
