<template>
    <div class="min-vh-100">
        <AppHeader />
        <GameSyncInfoComponent />
        <main class="container py-4">
            <slot />
        </main>
        <GoalsCelebrationModal ref="celebrationModal" />
    </div>
</template>

<script>
import GoalsCelebrationModal from '@/components/goals/modals/GoalsCelebrationModal.vue'
import AppHeader from './AppHeader.vue'
import GameSyncInfoComponent from './GameSyncInfoComponent.vue'

export default {
    name: 'AppShell',
    components: {
        AppHeader,
        GameSyncInfoComponent,
        GoalsCelebrationModal
    },
    mounted() {
        this.$refs.celebrationModal?.tryOpen()
    }
}
</script>
