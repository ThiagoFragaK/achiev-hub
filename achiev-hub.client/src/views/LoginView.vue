<script setup lang="ts">
import { ref } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { setAuthenticated } from '@/lib/session'

const router = useRouter()
const route = useRoute()
const steamId = ref('')
const password = ref('')
const steamIdError = ref(false)

function enterApp() {
  setAuthenticated(true)
  const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : '/'
  void router.push(redirect)
}

function onLogin() {
  steamIdError.value = !steamId.value.trim()
  if (steamIdError.value) return
  enterApp()
}

function continueAsGuest() {
  enterApp()
}
</script>

<template>
  <div class="flex min-h-svh items-center justify-center bg-background px-6 py-12 text-foreground">
    <div class="flex w-full max-w-md flex-col items-center gap-8">
      <img
        src="/assets/achiev-hub-complete-logo.png"
        alt="Achievements Hub"
        class="h-14 w-auto sm:h-16"
      >

      <div class="h-px w-full bg-purple" />

      <form class="flex w-full flex-col gap-5" @submit.prevent="onLogin">
        <div class="space-y-2">
          <label class="text-sm font-semibold" for="steamId">
            Steam ID
          </label>
          <Input
            id="steamId"
            v-model="steamId"
            type="text"
            name="steamId"
            autocomplete="username"
            class="bg-background"
            :aria-invalid="steamIdError"
          />
          <p
            v-if="steamIdError"
            class="text-center text-sm text-coral"
            role="alert"
          >
            Required
          </p>
        </div>

        <div class="space-y-2">
          <label class="text-sm font-semibold" for="password">
            Password
          </label>
          <Input
            id="password"
            v-model="password"
            type="password"
            name="password"
            autocomplete="current-password"
            class="bg-background"
          />
        </div>

        <div class="flex justify-center pt-2">
          <Button type="submit" variant="purple" size="lg" class="min-w-40">
            Login
          </Button>
        </div>
      </form>

      <button
        type="button"
        class="text-sm text-cream underline-offset-4 hover:underline"
        @click="continueAsGuest"
      >
        Continue without login.
      </button>

      <RouterLink
        to="/style-guide"
        class="text-muted-foreground text-xs underline-offset-4 hover:underline"
      >
        Style Guide
      </RouterLink>
    </div>
  </div>
</template>
