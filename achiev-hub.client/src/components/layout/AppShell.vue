<template>
    <div class="min-vh-100">
        <AppHeader />
        <main class="container py-4">
            <slot />
        </main>
    </div>
</template>

<script>
import AppHeader from './AppHeader.vue'

export default {
    name: 'AppShell',
    components: {
        AppHeader
    }
}
</script>
