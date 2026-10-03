<template>
    <div class="min-vh-100">
        <AppHeader />
        <GameSyncInfoComponent />
        <main class="container py-4">
            <slot />
        </main>
    </div>
</template>

<script>
import AppHeader from './AppHeader.vue'
import GameSyncInfoComponent from './GameSyncInfoComponent.vue'

export default {
    name: 'AppShell',
    components: {
        AppHeader,
        GameSyncInfoComponent
    }
}
</script>
