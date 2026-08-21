<script setup lang="ts">
import AppShell from '@/components/layout/AppShell.vue'
</script>

<template>
  <AppShell>
    <div class="space-y-6">
      <div class="flex items-center gap-4">
        <img
          src="/assets/steam-logo.png"
          alt="Steam"
          class="size-16 rounded-full sm:size-20"
        >
        <h1 class="text-3xl font-semibold tracking-tight sm:text-4xl">
          Steam
        </h1>
      </div>

      <div class="min-h-80 rounded-xl border border-purple p-6">
        <p class="text-muted-foreground text-sm">
          Steam integration content will appear here.
        </p>
      </div>
    </div>
  </AppShell>
</template>
