<script setup lang="ts">
import { Funnel } from '@lucide/vue'
import { RouterLink } from 'vue-router'
import AppShell from '@/components/layout/AppShell.vue'
import { Button } from '@/components/ui/button'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { demoGames } from '@/data/demo'

const cellClass = 'rounded-md border border-purple px-2 py-3 text-center'
const headClass = `${cellClass} bg-purple text-purple-foreground`
const bodyClass = `${cellClass} bg-cream text-cream-foreground`
</script>

<template>
  <AppShell>
    <div class="space-y-6">
      <div class="flex items-center gap-3">
        <Button variant="ghost" size="icon" aria-label="Filter games">
          <Funnel />
        </Button>
        <h1 class="text-2xl font-semibold tracking-tight sm:text-3xl">
          My Games
        </h1>
      </div>

      <div class="rounded-xl border border-purple p-4">
        <Table class="border-separate border-spacing-2">
          <TableHeader class="[&_tr]:border-0">
            <TableRow class="border-0 hover:bg-transparent">
              <TableHead :class="headClass">
                Game
              </TableHead>
              <TableHead :class="headClass">
                Hours
              </TableHead>
              <TableHead :class="headClass">
                Percentage
              </TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            <TableRow
              v-for="game in demoGames"
              :key="game.id"
              class="border-0 hover:bg-transparent"
            >
              <TableCell :class="bodyClass">
                <RouterLink
                  :to="{ name: 'game-detail', params: { id: game.id } }"
                  class="font-medium underline-offset-4 hover:underline"
                >
                  {{ game.name }}
                </RouterLink>
              </TableCell>
              <TableCell :class="bodyClass">
                {{ game.hours }}h
              </TableCell>
              <TableCell :class="bodyClass">
                {{ game.percentage }}%
              </TableCell>
            </TableRow>
          </TableBody>
        </Table>
      </div>
    </div>
  </AppShell>
</template>
