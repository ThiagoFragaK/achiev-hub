<template>
    <ModalComponent
        ref="modal"
        id="goals-celebration-modal"
        title="Goal reached!"
        cancel-text="Dismiss"
        save-text="Go to Goals"
        :isLoading="acknowledging"
        @save="onGoToGoals"
        @cancel="onDismiss"
    >
        <template v-if="goals.length === 1">
            <p class="mb-2">
                You've reached your goal for game
                <strong>{{ goals[0].gameName }}</strong>. Congratulations!
            </p>
            <ul class="list-unstyled mb-0 small text-secondary">
                <li>Achievements earned: {{ goals[0].achievementsEarned }}</li>
                <li>Final percentage: {{ formatPercentage(goals[0].finalPercentage) }}</li>
            </ul>
        </template>
        <template v-else-if="goals.length > 1">
            <p class="mb-3">You've reached your goals. Congratulations!</p>
            <ul class="list-group list-group-flush">
                <li
                    v-for="goal in goals"
                    :key="goal.id"
                    class="list-group-item px-0"
                >
                    <div class="fw-semibold">{{ goal.gameName }}</div>
                    <div class="small text-secondary">
                        Achievements earned: {{ goal.achievementsEarned }} · Final percentage:
                        {{ formatPercentage(goal.finalPercentage) }}
                    </div>
                </li>
            </ul>
        </template>
    </ModalComponent>
</template>

<script>
import ModalComponent from '@/components/global/ModalComponent.vue'
import { acknowledgeGoals, getUnacknowledgedCompletedGoals } from '@/services/goalsService'
import { getUser, mergeUser } from '@/lib/session'

export default {
    name: 'GoalsCelebrationModal',
    components: {
        ModalComponent
    },
    data() {
        return {
            goals: [],
            acknowledging: false
        }
    },
    methods: {
        formatPercentage(value) {
            const n = Number(value)
            if (Number.isNaN(n)) return '0%'
            return `${n.toFixed(n % 1 === 0 ? 0 : 1)}%`
        },
        async tryOpen() {
            const user = getUser()
            if (!user?.showGoals) {
                return
            }

            try {
                const goals = await getUnacknowledgedCompletedGoals()
                this.goals = Array.isArray(goals) ? goals : []
                if (this.goals.length === 0) {
                    await this.acknowledge()
                    return
                }
                this.$nextTick(() => this.$refs.modal?.open())
            } catch {
                // Keep login flow unblocked if celebration fetch fails.
            }
        },
        async acknowledge() {
            this.acknowledging = true
            try {
                await acknowledgeGoals()
                mergeUser({ showGoals: false })
            } finally {
                this.acknowledging = false
            }
        },
        async onDismiss() {
            await this.acknowledge()
            this.$refs.modal?.close()
        },
        async onGoToGoals() {
            await this.acknowledge()
            this.$refs.modal?.close()
            if (this.$route.name !== 'goals') {
                await this.$router.push({ name: 'goals' })
            }
        }
    }
}
</script>
