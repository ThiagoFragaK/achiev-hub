<template>
    <div class="card border-primary h-100">
        <div class="card-header bg-transparent border-primary">
            <div class="fw-semibold">What-if calculator</div>
            <div class="small text-secondary">
                Stack game overrides to see how your average would change. Nothing is saved.
            </div>
        </div>
        <div class="card-body">
            <p v-if="loadError" class="text-danger small mb-3">{{ loadError }}</p>
            <p v-else-if="loading" class="text-secondary small mb-3">Loading games…</p>

            <div class="row g-4">
                <div class="col-12 col-lg-6">
                    <div class="row g-3 mb-4">
                        <div class="col-6">
                            <div class="fs-4 fw-semibold text-success lh-1">{{ formatPercentage(currentAverage) }}</div>
                            <div class="small text-secondary mt-1">Current average</div>
                        </div>
                        <div class="col-6">
                            <div class="fs-4 fw-semibold lh-1">{{ gamesInAverage }}</div>
                            <div class="small text-secondary mt-1">Games in average</div>
                        </div>
                    </div>

                    <div class="row g-2 mb-3">
                        <div class="col-12">
                            <div ref="gamePicker">
                                <label class="form-label small text-secondary mb-1" for="whatIfGameSearch">
                                    Game name
                                </label>
                                <div class="position-relative">
                                    <div class="input-group">
                                        <input
                                            id="whatIfGameSearch"
                                            type="text"
                                            class="form-control border-end-0"
                                            autocomplete="off"
                                            placeholder="Search games…"
                                            :value="searchText"
                                            :disabled="loading || !!loadError"
                                            @input="onSearchInput"
                                            @focus="openDropdown"
                                            @keydown.down.prevent="moveHighlight(1)"
                                            @keydown.up.prevent="moveHighlight(-1)"
                                            @keydown.enter.prevent="selectHighlighted"
                                            @keydown.esc.prevent="closeDropdown"
                                        />
                                        <button
                                            type="button"
                                            class="input-group-text bg-body"
                                            :disabled="loading || !!loadError || !searchText"
                                            aria-label="Clear search"
                                            @mousedown.prevent="clearSearch"
                                        >
                                            ×
                                        </button>
                                    </div>
                                    <div
                                        v-if="dropdownOpen"
                                        class="list-group position-absolute w-100 shadow-sm what-if-dropdown"
                                        role="listbox"
                                    >
                                        <button
                                            v-for="(game, index) in filteredGames"
                                            :key="game.appId"
                                            type="button"
                                            class="list-group-item list-group-item-action d-flex justify-content-between align-items-center"
                                            :class="{ active: index === highlightIndex }"
                                            role="option"
                                            @mousedown.prevent="selectGame(game)"
                                        >
                                            <span class="text-truncate me-2">{{ game.name }}</span>
                                            <span class="small text-nowrap">{{ formatPercentage(game.achievementsPercentage) }}</span>
                                        </button>
                                        <div
                                            v-if="filteredGames.length === 0"
                                            class="list-group-item text-secondary small"
                                        >
                                            No games match.
                                        </div>
                                    </div>
                                </div>
                            </div>
                            <div v-if="selectedGame" class="form-text">
                                Selected: {{ selectedGame.name }}
                                ({{ formatPercentage(selectedGame.achievementsPercentage) }})
                            </div>
                        </div>
                        <div class="col-6">
                            <label class="form-label small text-secondary mb-1" for="whatIfMinPercent">
                                Min %
                            </label>
                            <input
                                id="whatIfMinPercent"
                                type="number"
                                class="form-control"
                                min="0"
                                max="100"
                                step="1"
                                placeholder="0"
                                :value="minPercentage"
                                :disabled="loading || !!loadError"
                                @input="onMinPercentageInput"
                            />
                        </div>
                        <div class="col-6">
                            <label class="form-label small text-secondary mb-1" for="whatIfMaxPercent">
                                Max %
                            </label>
                            <input
                                id="whatIfMaxPercent"
                                type="number"
                                class="form-control"
                                min="0"
                                max="100"
                                step="1"
                                placeholder="100"
                                :value="maxPercentage"
                                :disabled="loading || !!loadError"
                                @input="onMaxPercentageInput"
                            />
                        </div>
                    </div>

                    <div class="d-flex flex-wrap align-items-end gap-2 mb-4">
                        <div style="max-width: 10rem;">
                            <label class="form-label small text-secondary mb-1" for="whatIfPercent">
                                Hypothetical %
                            </label>
                            <input
                                id="whatIfPercent"
                                type="number"
                                class="form-control"
                                min="0"
                                max="100"
                                step="0.01"
                                :disabled="!selectedGame"
                                :value="overridePercentage"
                                @input="onOverrideInput"
                            />
                        </div>
                        <button
                            type="button"
                            class="btn btn-success"
                            :disabled="!canAddChange"
                            @click="addChange"
                        >
                            Add
                        </button>
                    </div>

                    <div class="row g-3">
                        <div class="col-6">
                            <div class="fs-4 fw-semibold text-success lh-1">{{ formatPercentage(projectedAverage) }}</div>
                            <div class="small text-secondary mt-1">Projected average</div>
                        </div>
                        <div class="col-6">
                            <div
                                class="fs-4 fw-semibold lh-1"
                                :class="deltaClass"
                            >
                                {{ formatDelta(delta) }}
                            </div>
                            <div class="small text-secondary mt-1">Change vs current</div>
                        </div>
                    </div>
                </div>

                <div class="col-12 col-lg-6">
                    <div class="d-flex align-items-center justify-content-between gap-2 mb-3">
                        <div class="fw-semibold">Changes</div>
                        <div class="small text-secondary">{{ changes.length }} stacked</div>
                    </div>
                    <div v-if="changes.length === 0" class="text-secondary small">
                        No changes yet. Pick a game, set a hypothetical %, and click Add.
                    </div>
                    <div v-else class="list-group list-group-flush what-if-log">
                        <div
                            v-for="entry in changeLog"
                            :key="entry.appId"
                            class="list-group-item px-0"
                        >
                            <div class="d-flex justify-content-between align-items-start gap-2">
                                <div class="min-w-0">
                                    <div class="fw-medium text-truncate">{{ entry.name }}</div>
                                    <div class="small text-secondary">
                                        {{ formatPercentage(entry.originalPercentage) }}
                                        →
                                        {{ formatPercentage(entry.newPercentage) }}
                                    </div>
                                </div>
                                <div class="d-flex align-items-center gap-2 flex-shrink-0">
                                    <span
                                        class="small fw-semibold"
                                        :class="impactClass(entry.avgImpact)"
                                    >
                                        {{ formatDelta(entry.avgImpact) }}
                                    </span>
                                    <button
                                        type="button"
                                        class="btn btn-sm btn-outline-secondary"
                                        :aria-label="`Remove ${entry.name}`"
                                        @click="removeChange(entry.appId)"
                                    >
                                        Remove
                                    </button>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
        <div class="card-footer bg-transparent border-primary d-flex flex-wrap align-items-center gap-2">
            <button
                type="button"
                class="btn btn-sm btn-outline-primary"
                :disabled="!hasAnythingToReset"
                @click="reset"
            >
                Reset
            </button>
            <span class="small text-secondary">
                Only games above 0% count toward the average. Raising a 0% game adds it to the mean.
            </span>
        </div>
    </div>
</template>

<script>
import { getGamesForAverage } from '@/services/statsService'
import { formatPercentage } from '@/utils/NumberFormat'

function averageOf(percentages) {
    const values = percentages.filter((p) => p > 0)
    if (!values.length) {
        return 0
    }
    const sum = values.reduce((a, b) => a + b, 0)
    return Math.round((sum / values.length) * 100) / 100
}

function clampPercent(n) {
    if (!Number.isFinite(n)) {
        return null
    }
    if (n < 0) {
        return 0
    }
    if (n > 100) {
        return 100
    }
    return n
}

function percentagesWithOverrides(games, overrideMap) {
    return games.map((g) => {
        if (Object.prototype.hasOwnProperty.call(overrideMap, g.appId)) {
            return Number(overrideMap[g.appId]) || 0
        }
        return Number(g.achievementsPercentage) || 0
    })
}

export default {
    name: 'AverageWhatIfCard',
    data() {
        return {
            loading: false,
            loadError: '',
            games: [],
            searchText: '',
            minPercentage: '',
            maxPercentage: '',
            dropdownOpen: false,
            highlightIndex: 0,
            selectedAppId: null,
            overridePercentage: '',
            changes: []
        }
    },
    computed: {
        selectedGame() {
            if (this.selectedAppId == null) {
                return null
            }
            return this.games.find((g) => g.appId === this.selectedAppId) ?? null
        },
        filteredGames() {
            const q = this.searchText.trim().toLowerCase()
            const min = this.minPercentage === '' ? null : Number(this.minPercentage)
            const max = this.maxPercentage === '' ? null : Number(this.maxPercentage)
            const changedIds = new Set(this.changes.map((c) => c.appId))

            return this.games
                .filter((g) => {
                    if (changedIds.has(g.appId)) {
                        return false
                    }
                    const name = (g.name || '').toLowerCase()
                    if (q && !name.includes(q)) {
                        return false
                    }
                    const p = Number(g.achievementsPercentage) || 0
                    if (min != null && Number.isFinite(min) && p < min) {
                        return false
                    }
                    if (max != null && Number.isFinite(max) && p > max) {
                        return false
                    }
                    return true
                })
                .slice(0, 50)
        },
        currentAverage() {
            return averageOf(this.games.map((g) => Number(g.achievementsPercentage) || 0))
        },
        gamesInAverage() {
            return this.games.filter((g) => Number(g.achievementsPercentage) > 0).length
        },
        overrideMap() {
            const map = {}
            for (const change of this.changes) {
                map[change.appId] = change.newPercentage
            }
            return map
        },
        projectedAverage() {
            if (!this.changes.length) {
                return this.currentAverage
            }
            return averageOf(percentagesWithOverrides(this.games, this.overrideMap))
        },
        delta() {
            return Math.round((this.projectedAverage - this.currentAverage) * 100) / 100
        },
        deltaClass() {
            return this.impactClass(this.delta)
        },
        changeLog() {
            const log = []
            const applied = {}
            let avgBefore = this.currentAverage

            for (const change of this.changes) {
                applied[change.appId] = change.newPercentage
                const avgAfter = averageOf(percentagesWithOverrides(this.games, applied))
                const avgImpact = Math.round((avgAfter - avgBefore) * 100) / 100
                log.push({
                    ...change,
                    avgImpact
                })
                avgBefore = avgAfter
            }

            return log
        },
        canAddChange() {
            if (!this.selectedGame || this.overridePercentage === '') {
                return false
            }
            const next = Number(this.overridePercentage)
            if (!Number.isFinite(next)) {
                return false
            }
            const original = Number(this.selectedGame.achievementsPercentage) || 0
            return next !== original
        },
        hasAnythingToReset() {
            return (
                this.changes.length > 0
                || this.selectedAppId != null
                || this.searchText !== ''
                || this.minPercentage !== ''
                || this.maxPercentage !== ''
                || this.overridePercentage !== ''
            )
        }
    },
    methods: {
        formatPercentage,
        formatDelta(value) {
            if (!Number.isFinite(value) || value === 0) {
                return formatPercentage(0)
            }
            const sign = value > 0 ? '+' : ''
            return `${sign}${formatPercentage(value)}`
        },
        impactClass(value) {
            if (value > 0) {
                return 'text-success'
            }
            if (value < 0) {
                return 'text-danger'
            }
            return 'text-secondary'
        },
        onSearchInput(event) {
            this.searchText = event.target.value
            this.dropdownOpen = true
            this.highlightIndex = 0
        },
        clearSearch() {
            this.searchText = ''
            this.highlightIndex = 0
            this.dropdownOpen = true
            this.$nextTick(() => {
                document.getElementById('whatIfGameSearch')?.focus()
            })
        },
        onMinPercentageInput(event) {
            this.minPercentage = this.normalizeBoundInput(event.target.value)
            this.highlightIndex = 0
        },
        onMaxPercentageInput(event) {
            this.maxPercentage = this.normalizeBoundInput(event.target.value)
            this.highlightIndex = 0
        },
        normalizeBoundInput(raw) {
            if (raw === '') {
                return ''
            }
            const n = clampPercent(Number(raw))
            return n == null ? '' : String(n)
        },
        openDropdown() {
            this.dropdownOpen = true
        },
        closeDropdown() {
            this.dropdownOpen = false
        },
        moveHighlight(delta) {
            if (!this.filteredGames.length) {
                return
            }
            this.dropdownOpen = true
            const len = this.filteredGames.length
            this.highlightIndex = (this.highlightIndex + delta + len) % len
        },
        selectHighlighted() {
            const game = this.filteredGames[this.highlightIndex]
            if (game) {
                this.selectGame(game)
            }
        },
        selectGame(game) {
            this.selectedAppId = game.appId
            this.searchText = game.name || ''
            const existing = this.changes.find((c) => c.appId === game.appId)
            this.overridePercentage = existing
                ? String(existing.newPercentage)
                : String(Number(game.achievementsPercentage) || 0)
            this.dropdownOpen = false
        },
        onOverrideInput(event) {
            const raw = event.target.value
            if (raw === '') {
                this.overridePercentage = ''
                return
            }
            const n = clampPercent(Number(raw))
            if (n == null) {
                return
            }
            this.overridePercentage = String(n)
        },
        addChange() {
            if (!this.canAddChange || !this.selectedGame) {
                return
            }
            const entry = {
                appId: this.selectedGame.appId,
                name: this.selectedGame.name || `App ${this.selectedGame.appId}`,
                originalPercentage: Number(this.selectedGame.achievementsPercentage) || 0,
                newPercentage: Number(this.overridePercentage)
            }
            const index = this.changes.findIndex((c) => c.appId === entry.appId)
            if (index >= 0) {
                this.changes.splice(index, 1, entry)
            } else {
                this.changes.push(entry)
            }
            this.selectedAppId = null
            this.searchText = ''
            this.overridePercentage = ''
            this.dropdownOpen = false
            this.highlightIndex = 0
        },
        removeChange(appId) {
            this.changes = this.changes.filter((c) => c.appId !== appId)
        },
        reset() {
            this.selectedAppId = null
            this.searchText = ''
            this.minPercentage = ''
            this.maxPercentage = ''
            this.overridePercentage = ''
            this.changes = []
            this.dropdownOpen = false
            this.highlightIndex = 0
        },
        onOutsidePointerDown(event) {
            if (!this.dropdownOpen) {
                return
            }
            const picker = this.$refs.gamePicker
            const target = event.target
            if (!(picker instanceof Element) || !(target instanceof Node)) {
                return
            }
            if (!picker.contains(target)) {
                this.closeDropdown()
            }
        },
        async loadGames() {
            this.loading = true
            this.loadError = ''
            try {
                const data = await getGamesForAverage()
                this.games = data?.games ?? []
            } catch (err) {
                this.loadError = err?.message || 'Failed to load games for the calculator.'
                this.games = []
            } finally {
                this.loading = false
            }
        }
    },
    async created() {
        await this.loadGames()
    },
    mounted() {
        // Capture phase so clicks inside the card (but outside the picker) still close it,
        // even if a parent stops bubble-phase propagation.
        document.addEventListener('pointerdown', this.onOutsidePointerDown, true)
    },
    beforeUnmount() {
        document.removeEventListener('pointerdown', this.onOutsidePointerDown, true)
    }
}
</script>

<style scoped>
.card.border-primary {
    --bs-card-border-width: 1px;
}

.what-if-dropdown {
    z-index: 20;
    max-height: 16rem;
    overflow-y: auto;
}

.what-if-log {
    max-height: 22rem;
    overflow-y: auto;
}

.min-w-0 {
    min-width: 0;
}
</style>
