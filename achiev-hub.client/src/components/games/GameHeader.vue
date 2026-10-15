<template>
    <p v-if="isLoading" class="text-secondary">Loading game details…</p>
    <p v-else-if="error" class="text-danger">{{ error }}</p>

    <div v-else class="row g-4 align-items-start">
        <div class="col-md-3 col-lg-3">
            <ImageComponent
                :src="game.gameImage || ''"
                :alt="game.gameName || 'Game'"
                ratio="21x9"
            />
        </div>

        <div class="col-md-5 col-lg-4">
            <h1 class="lastica-h3 mb-3">
                {{ game.gameName || 'Unknown game' }}
            </h1>
            <div class="row g-3">
                <div class="col-sm-6">
                    <p class="text-uppercase text-secondary small mb-1">Developers</p>
                    <p class="fs-6 fw-semibold mb-0">{{ game.developers || '—' }}</p>
                </div>
                <div class="col-sm-6">
                    <p class="text-uppercase text-secondary small mb-1">Publishers</p>
                    <p class="fs-6 fw-semibold mb-0">{{ game.publishers || '—' }}</p>
                </div>
            </div>
            <div class="row g-3">
                <div class="col-sm-6">
                    <p class="text-uppercase text-secondary small mb-1">Total achievements</p>
                    <p class="fs-6 fw-semibold mb-0">{{ game.totalAchievements || '—' }}</p>
                </div>
                <div class="col-sm-6">
                    <p class="text-uppercase text-secondary small mb-1">Unlocked achievements</p>
                    <p class="fs-6 fw-semibold mb-0">{{ game.userAchievementsCount || '—' }}</p>
                </div>
            </div>
        </div>

        <div class="col-md-4 col-lg-5">
            <GameAchievementsGraph
                title="Achievements unlocked (%)"
                :labels="progressLabels"
                :datasets="progressData"
                :max="100"
                :height="160"
                empty-message="No achievements unlocked yet."
            />
        </div>

        <div class="col-lg-3">
            <button
                type="button"
                class="btn btn-outline-secondary btn-sm"
                aria-label="Sync game data"
                :disabled="isLoading"
                @click="reload"
            >
                <LucideIcon icon="RefreshCw" :size="18" />
            </button>
        </div>
    </div>
</template>
<script>
import { getGameDetails } from '@/services/gamesService';
import { getGameProgress } from '@/services/statsService';
import ImageComponent from '@/components/global/ImageComponent.vue'
import LucideIcon from '@/components/global/LucideIcon.vue'
import GameAchievementsGraph from '@/components/games/graphs/GameAchievementsGraph.vue';

const labelFormatters = {
    day: new Intl.DateTimeFormat('en-GB', { day: 'numeric', month: 'short' }),
    week: new Intl.DateTimeFormat('en-GB', { day: 'numeric', month: 'short', year: '2-digit' }),
    month: new Intl.DateTimeFormat('en-GB', { month: 'short', year: 'numeric' }),
    year: new Intl.DateTimeFormat('en-GB', { year: 'numeric' })
}

export default {
    name: 'GameHeader',
    components: {
        ImageComponent,
        LucideIcon,
        GameAchievementsGraph
    },
    props: {
        gameId: {
            type: String,
            required: true
        }
    },
    data() {
        return {
            game: {
                gameName: '',
                gameImage: '',
                developers: '',
                publishers: '',
                totalAchievements: '',
                userAchievementsCount: ''
            },
            isLoading: false,
            error: null,
            progressLabels: [],
            progressPercentages: []
        }
    },
    methods: {
        async reload() {
            this.$emit('reload');
            await this.getProgress();
        },
        async getGameDetails() {
            if (!this.gameId) {
                this.error = 'Game id is missing.'
                return;
            }

            this.isLoading = true;
            try {
                const details = await getGameDetails(this.gameId)
                this.game = {
                    gameName: details?.gameName || '',
                    gameImage: details?.gameImage || '',
                    developers: details?.developers || '',
                    publishers: details?.publishers || ''
                }
                await this.getProgress();
            } catch (err) {
                this.game = {
                    gameName: '',
                    gameImage: '',
                    developers: '',
                    publishers: ''
                }
                this.error = err?.message || 'Failed to load game details.'
                this.isLoading = false;
            }
        },
        formatProgressLabel(isoDate, granularity) {
            const formatter = labelFormatters[granularity] || labelFormatters.day
            return formatter.format(new Date(`${isoDate}T00:00:00`))
        },
        async getProgress() {
            try {
                const progress = await getGameProgress(this.gameId);
                this.game.totalAchievements = progress?.totalCount ?? 0;
                this.game.userAchievementsCount = progress?.unlockedCount ?? 0;

                const points = progress?.points ?? []
                this.progressLabels = points.map(
                    (point) => this.formatProgressLabel(point.date, progress?.granularity)
                )
                this.progressPercentages = points.map((point) => point.percentage)
            } catch (err) {
                this.progressLabels = []
                this.progressPercentages = []
            } finally {
                this.isLoading = false;
            }
        },
    },
    async created() {
        await this.getGameDetails();
    },
    computed: {
        progressData() {
            return [
                {
                    label: 'Unlocked',
                    data: this.progressPercentages,
                    color: '#0d6efd',
                    fill: true
                }
            ]
        }
    },
}
</script>