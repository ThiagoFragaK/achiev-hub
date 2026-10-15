<template>
    <p v-if="isLoading" class="text-secondary">Loading game details…</p>
    <p v-else-if="error" class="text-danger">{{ error }}</p>

    <div v-else class="row g-4 align-items-start">
        <div class="col-7">
            <div class="row">
                <div class="col">
                    <ImageComponent
                        :src="game.gameImage || ''"
                        :alt="game.gameName || 'Game'"
                        ratio="21x9"
                    />
                </div>
    
                <div class="col">
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
            </div>
            <div class="row justify-content-center mt-3">
                <div class="col-1">
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
                <div class="col-10 mt-3">
                    <ProgressComponent
                        :value="achievementsPercentage"
                        :label="formatPercentage(achievementsPercentage)"
                        :color="getColor(achievementsPercentage)"
                    />
                </div>
                <div
                    v-if="isCompleted"
                    class="col-1"
                >
                    <button
                        type="button"
                        class="btn btn-outline-warning btn-sm"
                        aria-label="Sync game data"
                        disabled
                    >
                        <LucideIcon icon="Crown" :size="18" />
                    </button>
                </div>
            </div>
        </div>
        <div class="col-5">
            <GameAchievementsGraph
                title="Achievements unlocked (%)"
                :labels="progressLabels"
                :datasets="progressData"
                :max="100"
                :height="160"
                empty-message="No achievements unlocked yet."
            />
        </div>
    </div>
</template>
<script>
import { getGameDetails } from '@/services/gamesService';
import { getGameProgress } from '@/services/statsService';
import { formatPercentage } from '@/utils/NumberFormat'
import ImageComponent from '@/components/global/ImageComponent.vue'
import LucideIcon from '@/components/global/LucideIcon.vue'
import ProgressComponent from '@/components/global/ProgressComponent.vue'
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
        ProgressComponent,
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
        formatPercentage,
        getColor(percentage) {
            if(percentage == 100) {
                return 'success';
            }
            return percentage > 50 ? 'info' : 'danger';
        },
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
        achievementsPercentage() {
            const total = Number(this.game.totalAchievements) || 0;
            if (total === 0) {
                return 0;
            }
            return (Number(this.game.userAchievementsCount) || 0) / total * 100;
        },
        progressData() {
            return [
                {
                    label: 'Unlocked',
                    data: this.progressPercentages,
                    color: '#0d6efd',
                    fill: true
                }
            ]
        },
        isCompleted() {
            return this.game.userAchievementsCount == this.game.totalAchievements;
        }
    },
}
</script>