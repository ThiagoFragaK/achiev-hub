<script setup lang="ts">
import { RouterLink, useRoute, useRouter } from 'vue-router'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { setAuthenticated } from '@/lib/session'

const route = useRoute()
const router = useRouter()

const links = [
  { to: '/', label: 'Home', name: 'home' },
  { to: '/games', label: 'My games', name: 'games' },
  { to: '/stats', label: 'Stats', name: 'stats' },
  { to: '/steam', label: 'Steam', name: 'steam' },
] as const

function isActive(name: string) {
  if (name === 'games') {
    return route.name === 'games' || route.name === 'game-detail'
  }
  return route.name === name
}

function logout() {
  setAuthenticated(false)
  void router.push({ name: 'login' })
}
</script>

<template>
  <header class="border-b border-purple">
    <div class="mx-auto flex w-full max-w-6xl items-center gap-6 px-6 py-4">
      <RouterLink to="/" class="shrink-0">
        <img
          src="/assets/achiev-hub-complete-logo.png"
          alt="Achievements Hub"
          class="h-10 w-auto"
        >
      </RouterLink>

      <nav class="flex flex-1 flex-wrap items-center justify-center gap-6 text-sm font-medium uppercase tracking-wide">
        <RouterLink
          v-for="link in links"
          :key="link.to"
          :to="link.to"
          class="transition-colors hover:text-cream"
          :class="isActive(link.name) ? 'text-cream' : 'text-muted-foreground'"
        >
          {{ link.label }}
        </RouterLink>
      </nav>

      <DropdownMenu>
        <DropdownMenuTrigger
          class="flex shrink-0 items-center gap-3 rounded-md outline-none focus-visible:ring-3 focus-visible:ring-ring/50"
          aria-label="User menu"
        >
          <span class="text-sm font-medium tracking-wide">
            T.K.
          </span>
          <div
            class="size-9 overflow-hidden rounded-md border border-purple bg-navy"
            aria-hidden="true"
          >
            <div class="flex h-full flex-col">
              <div class="h-1/2 bg-navy" />
              <div class="h-1/2 bg-green/70" />
            </div>
          </div>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end" class="min-w-36">
          <DropdownMenuItem variant="destructive" @click="logout">
            Logout
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
    </div>
  </header>
</template>
