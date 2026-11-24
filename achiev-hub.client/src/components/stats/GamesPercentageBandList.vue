<template>
    <div>
        <p v-if="loadError" class="text-danger small mb-3">{{ loadError }}</p>
        <div
            v-if="bands.length > 0"
            class="d-flex flex-wrap align-items-center gap-2 mb-3"
        >
            <div class="d-flex flex-wrap gap-2 me-auto">
                <button
                    v-for="band in bands"
                    :key="band.key"
                    type="button"
                    class="btn btn-sm"
                    :class="selectedKey === band.key ? 'btn-primary' : 'btn-outline-secondary'"
                    @click="selectBand(band.key)"
                >
                    {{ band.label }}
                </button>
            </div>
            <div class="input-group input-group-sm stats-list-search">
                <label class="form-label visually-hidden" for="tier-game-search">Search by name</label>
                <input
                    id="tier-game-search"
                    v-model="nameDraft"
                    type="search"
                    class="form-control"
                    placeholder="Search by name"
                    :disabled="isLoading || bands.length === 0"
                    @keydown.enter.prevent="applySearch"
                />
                <button
                    type="button"
                    class="btn btn-outline-secondary"
                    :disabled="!nameDraft && !nameQuery"
                    aria-label="Clear search"
                    @click="clearSearch"
                >
                    ×
                </button>
            </div>
        </div>
        <p class="text-secondary small mb-2">
            {{ gameCountLabel }}
        </p>
        <TableComponent
            :data="pagedGames"
            :columns="columns"
            :isLoading="isLoading"
            :pagination="pagination"
            @change-page="onPageChange"
        >
            <template #cell-name="{ data }">
                <div class="d-flex align-items-center gap-2 text-start">
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
            <template #cell-achievementsPercentage="{ data }">
                <BadgeComponent
                    :label="formatPercentage(data.row.achievementsPercentage)"
                    :color="progressColor(data.row.achievementsPercentage)"
                />
            </template>
        </TableComponent>
    </div>
</template>

<script>
import { steamAppIconUrl } from '@/lib/steam'
import { getGamesPercentageBandGames } from '@/services/statsService'
import { formatPercentage } from '@/utils/NumberFormat'
import { createPagination, setPage } from '@/utils/PaginationHelper'
import TableComponent from '@/components/global/TableComponent.vue'
import BadgeComponent from '@/components/global/BadgeComponent.vue'

const PAGE_SIZE = 10

export default {
    name: 'GamesPercentageBandList',
    components: {
        TableComponent,
        BadgeComponent
    },
    data() {
        return {
            loadError: '',
            isLoading: true,
            bands: [],
            selectedKey: '',
            nameDraft: '',
            nameQuery: '',
            pagination: createPagination({ currentPage: 1, perPage: PAGE_SIZE }),
            columns: [
                {
                    label: 'Game',
                    key: 'name'
                },
                {
                    label: 'Time played',
                    key: 'playtime'
                },
                {
                    label: 'Percentage',
                    key: 'achievementsPercentage'
                }
            ]
        }
    },
    computed: {
        selectedBand() {
            return this.bands.find((item) => item.key === this.selectedKey) ?? null
        },
        filteredGames() {
            const games = this.selectedBand?.games ?? []
            const query = this.nameQuery.trim().toLowerCase()
            if (!query) {
                return games
            }
            return games.filter((game) => (game.name || '').toLowerCase().includes(query))
        },
        gameCountLabel() {
            const count = this.filteredGames.length
            return `${count} ${count === 1 ? 'game' : 'games'}`
        },
        pagedGames() {
            const start = (this.pagination.currentPage - 1) * this.pagination.perPage
            return this.filteredGames.slice(start, start + this.pagination.perPage)
        }
    },
    watch: {
        filteredGames: {
            immediate: true,
            handler(games) {
                this.syncPagination(games.length)
            }
        }
    },
    created() {
        this.loadData()
    },
    methods: {
        formatPercentage,
        iconUrl(game) {
            return steamAppIconUrl(game.appId, game.icon)
        },
        progressColor(percentage) {
            if (Number(percentage) === 100) {
                return 'success'
            }
            return Number(percentage) > 50 ? 'info' : 'danger'
        },
        pickDefaultKey(bands) {
            const withGames = bands.find((band) => (band.games?.length ?? 0) > 0)
            return withGames?.key ?? bands[0]?.key ?? ''
        },
        selectBand(key) {
            if (this.selectedKey === key) {
                return
            }
            this.selectedKey = key
            this.pagination.currentPage = 1
        },
        applySearch() {
            const next = this.nameDraft.trim()
            if (next === this.nameQuery) {
                return
            }
            this.nameQuery = next
            this.nameDraft = next
            this.pagination.currentPage = 1
        },
        clearSearch() {
            this.nameDraft = ''
            if (!this.nameQuery) {
                return
            }
            this.nameQuery = ''
            this.pagination.currentPage = 1
        },
        syncPagination(totalCount) {
            this.pagination.totalCount = totalCount
            this.pagination.totalPages = Math.max(1, Math.ceil(totalCount / this.pagination.perPage) || 1)
            if (this.pagination.currentPage > this.pagination.totalPages) {
                this.pagination.currentPage = this.pagination.totalPages
            }
        },
        onPageChange(page) {
            setPage(this.pagination, page)
        },
        async loadData() {
            this.loadError = ''
            this.isLoading = true
            try {
                const data = await getGamesPercentageBandGames()
                this.bands = data?.bands ?? []
                this.selectedKey = this.pickDefaultKey(this.bands)
                this.pagination.currentPage = 1
            } catch (err) {
                this.loadError = err?.message || 'Failed to load games by completion tier.'
                this.bands = []
                this.selectedKey = ''
            } finally {
                this.isLoading = false
            }
        }
    }
}
</script>

<style scoped>
.stats-list-search {
    max-width: 15rem;
}
</style>
