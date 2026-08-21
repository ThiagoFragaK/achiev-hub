<script setup lang="ts">
import { ref } from 'vue'
import { RouterLink } from 'vue-router'
import { Button } from '@/components/ui/button'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { HttpError } from '@/services/http'
import { getPlayer, type Player } from '@/services/playersService'

const steamId = ref('')
const loading = ref(false)
const error = ref<string | null>(null)
const player = ref<Player | null>(null)

async function lookUpPlayer() {
  const id = steamId.value.trim()
  error.value = null
  player.value = null

  if (!id) {
    error.value = 'Enter a Steam ID to look up a player.'
    return
  }

  loading.value = true
  try {
    player.value = await getPlayer(id)
    if (!player.value) {
      error.value = 'No player found for that Steam ID.'
    }
  } catch (err) {
    if (err instanceof HttpError) {
      error.value = err.status === 404
        ? 'No player found for that Steam ID.'
        : `Lookup failed (${err.status}).`
    } else {
      error.value = 'Lookup failed. Is the API running?'
    }
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="min-h-svh bg-background text-foreground">
    <div class="mx-auto flex min-h-svh w-full max-w-lg flex-col justify-center gap-8 px-6 py-12">
      <header class="space-y-3 text-center">
        <img
          src="/assets/AchievHub%20-%20LOGO.png"
          alt="AchievHub"
          class="mx-auto h-20 w-auto sm:h-24"
        >
        <h1 class="text-4xl font-semibold tracking-tight sm:text-5xl">
          AchievHub
        </h1>
        <p class="text-muted-foreground text-balance">
          Track Steam games, achievements, and completion goals.
        </p>
      </header>

      <Card>
        <CardHeader>
          <CardTitle>Player lookup</CardTitle>
          <CardDescription>
            Enter a Steam ID to verify the API connection.
          </CardDescription>
        </CardHeader>
        <CardContent class="space-y-4">
          <form class="flex flex-col gap-3 sm:flex-row" @submit.prevent="lookUpPlayer">
            <Input
              v-model="steamId"
              type="text"
              name="steamId"
              placeholder="Steam ID"
              autocomplete="off"
              :disabled="loading"
              class="sm:flex-1"
            />
            <Button type="submit" :disabled="loading">
              {{ loading ? 'Looking up…' : 'Look up' }}
            </Button>
          </form>

          <p v-if="error" class="text-destructive text-sm" role="alert">
            {{ error }}
          </p>

          <div
            v-if="player"
            class="flex items-center gap-3 rounded-lg border border-border p-3"
          >
            <img
              v-if="player.avatar"
              :src="player.avatar"
              :alt="player.personaName ?? 'Steam avatar'"
              class="size-10 rounded-md"
            >
            <div class="min-w-0">
              <p class="truncate font-medium">
                {{ player.personaName ?? 'Unknown player' }}
              </p>
              <p class="text-muted-foreground truncate text-sm">
                {{ player.steamId }}
              </p>
            </div>
          </div>
        </CardContent>
      </Card>

      <div class="flex justify-center">
        <Button as-child variant="outline">
          <RouterLink to="/style-guide">
            Style Guide
          </RouterLink>
        </Button>
      </div>
    </div>
  </div>
</template>
