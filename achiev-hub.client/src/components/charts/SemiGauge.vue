<script setup lang="ts">
withDefaults(defineProps<{
  value?: number
  label?: string
}>(), {
  value: 67,
  label: 'Users Average',
})
</script>

<template>
  <div class="flex flex-col items-center gap-3">
    <p class="text-sm font-medium uppercase tracking-wide text-muted-foreground">
      {{ label }}
    </p>
    <div class="relative w-full max-w-[220px]">
      <svg viewBox="0 0 200 120" class="h-auto w-full" aria-hidden="true">
        <path
          d="M 20 100 A 80 80 0 0 1 180 100"
          fill="none"
          stroke="var(--purple)"
          stroke-width="18"
          stroke-linecap="round"
        />
        <path
          d="M 20 100 A 80 80 0 0 1 180 100"
          fill="none"
          stroke="var(--navy)"
          stroke-width="18"
          stroke-linecap="round"
          :stroke-dasharray="`${(value / 100) * 251} 251`"
        />
      </svg>
      <div class="absolute inset-0 flex items-end justify-center pb-2">
        <span class="text-3xl font-semibold tracking-tight">
          {{ value }}%
        </span>
      </div>
    </div>
  </div>
</template>
