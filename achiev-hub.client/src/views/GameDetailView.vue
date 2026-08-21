<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { Funnel, RefreshCw } from '@lucide/vue'
import AppShell from '@/components/layout/AppShell.vue'
import LineAreaChart from '@/components/charts/LineAreaChart.vue'
import { Button } from '@/components/ui/button'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { demoAchievements, demoGames } from '@/data/demo'

const route = useRoute()

const game = computed(() => {
  const id = String(route.params.id)
  return demoGames.find((g) => g.id === id) ?? demoGames[0]
})

const percentageLabels = ['ITEM 1', 'ITEM 2', 'ITEM 3', 'ITEM 4', 'ITEM 5']
const percentageData = [
  {
    label: 'Percentage',
    data: [8, 14, 20, 28, 36],
    color: '#1B4564',
  },
]

const cellClass = 'rounded-md border border-purple px-2 py-3 text-center'
const headClass = `${cellClass} bg-purple text-purple-foreground`
const bodyClass = `${cellClass} bg-cream text-cream-foreground`
</script>

<template>
  <AppShell>
    <div class="space-y-8">
      <section class="grid gap-6 lg:grid-cols-[200px_1fr_240px_auto] lg:items-start">
        <div
          class="aspect-video overflow-hidden rounded-lg border border-purple bg-navy lg:aspect-square"
          aria-hidden="true"
        >
          <div class="flex h-full flex-col">
            <div class="h-1/2 bg-navy" />
            <div class="h-1/2 bg-green/60" />
          </div>
        </div>

        <div class="space-y-4">
          <h1 class="text-2xl font-semibold tracking-tight uppercase sm:text-3xl">
            {{ game.name }}
          </h1>
          <div class="grid gap-3 sm:grid-cols-2">
            <div>
              <p class="text-xs uppercase tracking-wide text-muted-foreground">
                Total game hours
              </p>
              <p class="text-xl font-semibold">
                {{ game.hours }}h
              </p>
            </div>
            <div>
              <p class="text-xs uppercase tracking-wide text-muted-foreground">
                Percentage
              </p>
              <p class="text-xl font-semibold">
                {{ game.percentage }}%
              </p>
            </div>
          </div>
        </div>

        <div class="rounded-xl border border-purple p-3">
          <LineAreaChart
            title="Percentage per year."
            :labels="percentageLabels"
            :datasets="percentageData"
            :max="40"
          />
        </div>

        <Button variant="ghost" size="icon" aria-label="Sync game data" class="justify-self-end">
          <RefreshCw />
        </Button>
      </section>

      <section class="space-y-4 rounded-xl border border-purple p-4">
        <div class="flex items-center justify-between gap-3">
          <h2 class="text-lg font-semibold tracking-tight">
            Games Achievements
          </h2>
          <Button variant="ghost" size="icon" aria-label="Filter achievements">
            <Funnel />
          </Button>
        </div>

        <Table class="border-separate border-spacing-2">
          <TableHeader class="[&_tr]:border-0">
            <TableRow class="border-0 hover:bg-transparent">
              <TableHead :class="headClass">
                Achievement
              </TableHead>
              <TableHead :class="headClass">
                Status
              </TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            <TableRow
              v-for="achievement in demoAchievements"
              :key="achievement.id"
              class="border-0 hover:bg-transparent"
            >
              <TableCell :class="bodyClass">
                {{ achievement.name }}
              </TableCell>
              <TableCell :class="bodyClass">
                {{ achievement.unlocked ? 'Unlocked' : 'Locked' }}
              </TableCell>
            </TableRow>
          </TableBody>
        </Table>
      </section>
    </div>
  </AppShell>
</template>
