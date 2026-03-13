<script setup lang="ts">
import { RouterView } from 'vue-router'
import { onMounted } from 'vue'
import { useAuthStore } from '@/stores/authStore'

const authStore = useAuthStore()

onMounted(() => {
  authStore.init()
  if (authStore.isLoggedIn) {
    authStore.fetchProfile()
  }
})
</script>

<template>
  <RouterView />
</template>

<style>
#app {
  width: 100%;
  height: 100vh;
}
</style>
