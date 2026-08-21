<template>
    <AppShell>
        <div class="d-flex align-items-center gap-3 mb-4">
            <img
                src="/assets/steam-logo.png"
                alt="Steam"
                class="rounded-circle"
                width="64"
                height="64"
            />
            <h1 class="h2 mb-0">Steam</h1>
        </div>

        <div class="card">
            <div class="card-body" style="min-height: 20rem">
                <p class="text-secondary mb-0">Steam integration content will appear here.</p>
            </div>
        </div>
    </AppShell>
</template>

<script>
import AppShell from '@/components/layout/AppShell.vue'

export default {
    name: 'SteamView',
    components: {
        AppShell
    }
}
</script>
