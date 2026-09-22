<template>
    <TableComponent
        :data="table.data"
        :columns="table.columns"
        :isLoading="table.isLoading"
        :pagination="table.pagination"
        @change-page="onPageChange"
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
        <template #cell-playtime="{ data }">
            {{ data.row.playtime }} hours
        </template>
        <template #cell-notPlayedSince="{ data }">
            {{ data.row.notPlayedSince == 0 ? '-' : data.row.notPlayedSince }}
        </template>
    </TableComponent>
</template>
<script>
import { getSessionSteamId, steamAppIconUrl } from '@/lib/steam'
import { getLibrary } from '@/services/gamesService';
import TableComponent from '@/components/global/TableComponent.vue';

export default {
    name: 'MyGamesTable',
    components: {
        TableComponent
    },
    props: {
        filters: {
            type: Object,
            required: true
        }
    },
    data() {
        return {
            steamId: null,
            table: {
                data: [],
                columns: [
                    {
                        label: 'Game',
                        key: 'name'
                    },
                    {
                        label: 'Hours',
                        key: 'playtime'
                    },
                    {
                        label: 'Last played',
                        key: 'notPlayedSince'
                    }
                ],
                pagination:{
                    currentPage: 1,
                    perPage: 15,
                    totalPages: 1,
                    totalCount: 0,
                },
                isLoading: true
            }
        }
    },
    methods: {
        iconUrl(game) {
            return steamAppIconUrl(game.appId, game.icon)
        },
        async setSteamId() {
            this.steamId = await getSessionSteamId();
        },
        async onPageChange(page) {
            this.table.pagination.currentPage = page
            await this.getUsersLibrary()
        },
        async getUsersLibrary() {
            if (!this.steamId) {
                this.error = 'Steam ID is missing from your session.'
                this.table.data = []
                return
            }

            this.table.isLoading = true;
            try {
                const result = await getLibrary(
                    this.steamId,
                    this.table.pagination.currentPage,
                    this.table.pagination.perPage,
                    this.filters
                )
                this.table.data = result?.data ?? []
                this.table.pagination.currentPage = result?.currentPage ?? 1
                this.table.pagination.totalPages = Math.max(1, result?.lastPage ?? 1)
                this.table.pagination.totalCount = result?.totalCount ?? 0
            } catch (err) {
                this.table.data = []
            } finally {
                this.table.isLoading = false;
            }
        }
    },
    async created() {
        await this.setSteamId();
        await this.getUsersLibrary()
    }
}
</script>