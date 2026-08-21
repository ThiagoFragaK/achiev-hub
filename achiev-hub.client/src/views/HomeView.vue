<script setup lang="ts">
import AppShell from '@/components/layout/AppShell.vue'
import LineAreaChart from '@/components/charts/LineAreaChart.vue'
import SemiGauge from '@/components/charts/SemiGauge.vue'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { demoRecentHours } from '@/data/demo'

const achievementsLabels = ['2021', '2023', '2025']
const achievementsData = [
  {
    label: 'Achievements',
    data: [10, 24, 38],
    color: '#1B4564',
  },
]

const perYearLabels = ['JAN', 'FEV', 'MAR', 'APR', 'MAY', 'JUN', 'JUL', 'AUG', 'SEP', 'OCT', 'NOV', 'DEC']
const perYearData = [
  {
    label: 'Series A',
    data: [12, 18, 15, 22, 28, 25, 30, 27, 32, 35, 30, 40],
    color: '#372649',
    fill: true,
  },
  {
    label: 'Series B',
    data: [8, 14, 20, 18, 24, 30, 26, 34, 28, 36, 42, 38],
    color: '#1B4564',
    fill: true,
  },
]

const cellClass = 'rounded-md border border-purple px-2 py-3 text-center'
const headClass = `${cellClass} bg-purple text-purple-foreground`
const bodyClass = `${cellClass} bg-cream text-cream-foreground`
</script>

<template>
  <AppShell>
    <div class="space-y-8">
      <section class="grid gap-6 lg:grid-cols-3">
        <div class="rounded-xl border border-purple p-4">
          <p class="mb-3 text-sm font-medium uppercase tracking-wide text-muted-foreground">
            Achievements in 14 days
          </p>
          <LineAreaChart
            :labels="achievementsLabels"
            :datasets="achievementsData"
            :max="40"
          />
        </div>

        <div class="flex items-center justify-center rounded-xl border border-purple p-4">
          <SemiGauge :value="67" label="Users Average" />
        </div>

        <div class="rounded-xl border border-purple p-4">
          <p class="mb-3 text-sm font-medium uppercase tracking-wide text-muted-foreground">
            Per Years
          </p>
          <LineAreaChart
            :labels="perYearLabels"
            :datasets="perYearData"
            :max="50"
          />
        </div>
      </section>

      <section class="space-y-3 rounded-xl border border-purple p-4">
        <h2 class="text-lg font-semibold tracking-tight">
          76hrs in last 14 days
        </h2>
        <Table class="border-separate border-spacing-2">
          <TableHeader class="[&_tr]:border-0">
            <TableRow class="border-0 hover:bg-transparent">
              <TableHead
                v-for="game in demoRecentHours"
                :key="game.name"
                :class="headClass"
              >
                {{ game.name }}
              </TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            <TableRow class="border-0 hover:bg-transparent">
              <TableCell
                v-for="game in demoRecentHours"
                :key="`${game.name}-hours`"
                :class="bodyClass"
              >
                {{ game.hours }}
              </TableCell>
            </TableRow>
            <TableRow class="border-0 hover:bg-transparent">
              <TableCell
                v-for="game in demoRecentHours"
                :key="`${game.name}-percentage`"
                :class="bodyClass"
              >
                {{ game.percentage }}
              </TableCell>
            </TableRow>
            <TableRow class="border-0 hover:bg-transparent">
              <TableCell
                v-for="game in demoRecentHours"
                :key="`${game.name}-status`"
                :class="bodyClass"
              >
                {{ game.status }}
              </TableCell>
            </TableRow>
          </TableBody>
        </Table>
      </section>
    </div>
  </AppShell>
</template>
