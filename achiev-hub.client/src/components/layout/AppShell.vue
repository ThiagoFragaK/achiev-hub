<script setup lang="ts">
import AppHeader from './AppHeader.vue'
</script>

<template>
  <div class="min-h-svh bg-background text-foreground">
    <AppHeader />
    <main class="mx-auto w-full max-w-6xl px-6 py-8">
      <slot />
    </main>
  </div>
</template>
