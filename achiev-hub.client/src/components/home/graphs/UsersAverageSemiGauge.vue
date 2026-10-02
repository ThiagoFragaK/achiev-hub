<template>
    <div class="card h-100">
        <div class="card-body d-flex flex-column align-items-center justify-content-center gap-2">
            <p class="text-secondary small text-center mb-0">
                {{ totalAchievements.toLocaleString() }} achievements in
                {{ gamesWithAchievements.toLocaleString() }} games
            </p>
            <SemiGauge :value="value" :label="label" :height="height" />
        </div>
    </div>
</template>

<script>
import SemiGauge from '@/components/charts/SemiGauge.vue'

export default {
    name: 'UsersAverageSemiGauge',
    components: {
        SemiGauge
    },
    props: {
        value: {
            type: Number,
            required: false,
            default: 67
        },
        label: {
            type: String,
            required: false,
            default: 'Users Average'
        },
        height: {
            type: [Number, String],
            required: false,
            default: 220
        },
        totalAchievements: {
            type: Number,
            required: false,
            default: 0
        },
        gamesWithAchievements: {
            type: Number,
            required: false,
            default: 0
        }
    }
}
</script>
