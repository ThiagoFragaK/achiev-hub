<script setup lang="ts">
import { RouterLink } from 'vue-router'
import { Info } from '@lucide/vue'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'

const palette = [
  { name: 'Purple', token: 'purple', hex: '#372649', swatch: 'bg-purple' },
  { name: 'Slate', token: 'slate', hex: '#6C738C', swatch: 'bg-slate' },
  { name: 'Green', token: 'green', hex: '#6AC36C', swatch: 'bg-green' },
  { name: 'Navy', token: 'navy', hex: '#1B4564', swatch: 'bg-navy' },
  { name: 'Gray', token: 'gray', hex: '#D7D6D0', swatch: 'bg-gray' },
  { name: 'Ink', token: 'ink', hex: '#0E0500', swatch: 'bg-ink' },
  { name: 'Cream', token: 'cream', hex: '#FFFECB', swatch: 'bg-cream' },
  { name: 'Coral', token: 'coral', hex: '#FF6462', swatch: 'bg-coral' },
] as const

const colorVariants = [
  'purple',
  'slate',
  'green',
  'navy',
  'gray',
  'ink',
  'cream',
  'coral',
] as const

const sampleGames = [
  { name: 'Hades', progress: '92%', status: 'green' as const, label: 'Nearly done' },
  { name: 'Celeste', progress: '100%', status: 'navy' as const, label: 'Complete' },
  { name: 'Hollow Knight', progress: '64%', status: 'slate' as const, label: 'In progress' },
  { name: 'Dead Cells', progress: '18%', status: 'coral' as const, label: 'Started' },
]
</script>

<template>
  <div class="min-h-svh bg-background text-foreground">
    <div class="mx-auto flex w-full max-w-4xl flex-col gap-12 px-6 py-12">
      <header class="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div class="space-y-2">
          <h1 class="text-3xl font-semibold tracking-tight sm:text-4xl">
            Style Guide
          </h1>
          <p class="text-muted-foreground max-w-xl text-balance">
            AchievHub design tokens and UI components used across the app.
          </p>
        </div>
        <Button as-child variant="outline">
          <RouterLink to="/">
            Back to Home
          </RouterLink>
        </Button>
      </header>

      <section class="space-y-4">
        <h2 class="text-xl font-semibold tracking-tight">
          Colors
        </h2>
        <div class="grid grid-cols-2 gap-3 sm:grid-cols-4">
          <div
            v-for="color in palette"
            :key="color.token"
            class="overflow-hidden rounded-xl border border-border"
          >
            <div
              class="h-20 border-b border-border"
              :class="[color.swatch, color.token === 'ink' ? 'border-purple' : '']"
            />
            <div class="space-y-0.5 p-3">
              <p class="font-medium">
                {{ color.name }}
              </p>
              <p class="text-muted-foreground text-xs uppercase tracking-wide">
                {{ color.hex }}
              </p>
            </div>
          </div>
        </div>
      </section>

      <section class="space-y-4">
        <h2 class="text-xl font-semibold tracking-tight">
          Typography
        </h2>
        <div class="space-y-3 rounded-xl border border-border p-5">
          <h1 class="text-4xl font-semibold tracking-tight">
            Heading 1
          </h1>
          <h2 class="text-2xl font-semibold tracking-tight">
            Heading 2
          </h2>
          <h3 class="text-xl font-semibold tracking-tight">
            Heading 3
          </h3>
          <p class="text-base">
            Body text for primary content and descriptions.
          </p>
          <p class="text-muted-foreground text-sm">
            Muted text for secondary details and helper copy.
          </p>
        </div>
      </section>

      <section class="space-y-4">
        <h2 class="text-xl font-semibold tracking-tight">
          Buttons
        </h2>
        <div class="flex flex-wrap gap-2">
          <Button
            v-for="variant in colorVariants"
            :key="variant"
            :variant="variant"
          >
            {{ variant }}
          </Button>
        </div>
        <div class="flex flex-wrap gap-2">
          <Button variant="default">
            Default
          </Button>
          <Button variant="outline">
            Outline
          </Button>
          <Button variant="secondary">
            Secondary
          </Button>
          <Button variant="ghost">
            Ghost
          </Button>
          <Button variant="destructive">
            Destructive
          </Button>
          <Button variant="link">
            Link
          </Button>
        </div>
      </section>

      <section class="space-y-4">
        <h2 class="text-xl font-semibold tracking-tight">
          Badges
        </h2>
        <div class="flex flex-wrap gap-2">
          <Badge
            v-for="variant in colorVariants"
            :key="variant"
            :variant="variant"
          >
            {{ variant }}
          </Badge>
        </div>
      </section>

      <section class="space-y-4">
        <h2 class="text-xl font-semibold tracking-tight">
          Notifications
        </h2>
        <div class="grid gap-3">
          <Alert
            v-for="variant in colorVariants"
            :key="variant"
            :variant="variant"
          >
            <Info />
            <AlertTitle class="capitalize">
              {{ variant }} notification
            </AlertTitle>
            <AlertDescription>
              Sample alert using the {{ variant }} palette color.
            </AlertDescription>
          </Alert>
        </div>
      </section>

      <section class="space-y-4">
        <h2 class="text-xl font-semibold tracking-tight">
          Form
        </h2>
        <Card>
          <CardHeader>
            <CardTitle>Sample input</CardTitle>
            <CardDescription>
              Input and button pairing used in forms.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <form class="flex flex-col gap-3 sm:flex-row" @submit.prevent>
              <Input
                type="text"
                name="sample"
                placeholder="Enter a value"
                class="sm:flex-1"
              />
              <Button type="submit">
                Submit
              </Button>
            </form>
          </CardContent>
        </Card>
      </section>

      <section class="space-y-4">
        <h2 class="text-xl font-semibold tracking-tight">
          Card
        </h2>
        <Card>
          <CardHeader>
            <CardTitle>Achievement card</CardTitle>
            <CardDescription>
              Cards group related content and actions.
            </CardDescription>
          </CardHeader>
          <CardContent class="space-y-3">
            <p>
              Use cards for lookup results, game summaries, and settings panels.
            </p>
            <div class="flex flex-wrap gap-2">
              <Badge variant="green">
                Unlocked
              </Badge>
              <Badge variant="slate">
                Rare
              </Badge>
            </div>
          </CardContent>
        </Card>
      </section>

      <section class="space-y-4">
        <h2 class="text-xl font-semibold tracking-tight">
          Table
        </h2>
        <div class="overflow-hidden rounded-xl border border-border">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Game</TableHead>
                <TableHead>Progress</TableHead>
                <TableHead>Status</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              <TableRow
                v-for="game in sampleGames"
                :key="game.name"
              >
                <TableCell class="font-medium">
                  {{ game.name }}
                </TableCell>
                <TableCell>
                  {{ game.progress }}
                </TableCell>
                <TableCell>
                  <Badge :variant="game.status">
                    {{ game.label }}
                  </Badge>
                </TableCell>
              </TableRow>
            </TableBody>
          </Table>
        </div>
      </section>
    </div>
  </div>
</template>
